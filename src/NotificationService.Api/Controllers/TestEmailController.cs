using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Repositories;
using NotificationService.Infrastructure.Services.Email;
using NotificationService.Infrastructure.Services.Templates;
using NotificationService.Routing.Domain.Enums;
using NotificationService.Routing.Domain.Models;
using NotificationService.Routing.DTOs;
using NotificationService.Routing.Repositories;
using NotificationService.Routing.Services;

namespace NotificationService.Api.Controllers;

/// <summary>
/// API controller for sending test emails to specific recipient groups or contacts.
/// Provides targeted test email functionality with full audit trail.
/// </summary>
[ApiController]
[Route("api/test-emails")]
[Produces("application/json")]
public class TestEmailController : ControllerBase
{
    private readonly IRecipientGroupService _groupService;
    private readonly IContactService _contactService;
    private readonly ITestEmailDeliveryRepository _testEmailRepository;
    private readonly IEmailTemplateRepository _templateRepository;
    private readonly ITemplateRenderingService _renderingService;
    private readonly IEmailService _emailService;
    private readonly IRoutingPolicyService _policyService;
    private readonly ILogger<TestEmailController> _logger;

    public TestEmailController(
        IRecipientGroupService groupService,
        IContactService contactService,
        ITestEmailDeliveryRepository testEmailRepository,
        IEmailTemplateRepository templateRepository,
        ITemplateRenderingService renderingService,
        IEmailService emailService,
        IRoutingPolicyService policyService,
        ILogger<TestEmailController> logger)
    {
        _groupService = groupService;
        _contactService = contactService;
        _testEmailRepository = testEmailRepository;
        _templateRepository = templateRepository;
        _renderingService = renderingService;
        _emailService = emailService;
        _policyService = policyService;
        _logger = logger;
    }

    /// <summary>
    /// Send a test email to all members of a recipient group
    /// </summary>
    [HttpPost("send-to-group")]
    [ProducesResponseType(typeof(TestEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendToGroup([FromBody] SendTestEmailToGroupRequest request, CancellationToken ct)
    {
        // Validate group exists and is eligible for test emails
        var group = await _groupService.GetByIdAsync(request.RecipientGroupId);
        if (group == null)
        {
            return NotFound(new { message = $"Recipient group {request.RecipientGroupId} not found" });
        }

        if (group.Purpose == GroupPurpose.Production)
        {
            return BadRequest(new { message = $"Group '{group.Name}' is marked as Production-only and cannot receive test emails. Change the group purpose to 'TestOnly' or 'Both' to enable test emails." });
        }

        // Get group members
        var members = await _groupService.GetMembersAsync(request.RecipientGroupId);
        if (!members.Any())
        {
            return BadRequest(new { message = $"Group '{group.Name}' has no members to send to" });
        }

        var activeMembers = members.Where(m => m.IsActive).ToList();
        if (!activeMembers.Any())
        {
            return BadRequest(new { message = $"Group '{group.Name}' has no active members to send to" });
        }

        var recipientEmails = activeMembers.Select(m => m.Email).ToList();

        return await SendTestEmailInternal(
            request.TemplateName,
            recipientEmails,
            request.TemplateData,
            request.TestReason,
            request.RecipientGroupId,
            ct);
    }

    /// <summary>
    /// Send a test email to specific contacts by their IDs
    /// </summary>
    [HttpPost("send-to-contacts")]
    [ProducesResponseType(typeof(TestEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendToContacts([FromBody] SendTestEmailToContactsRequest request, CancellationToken ct)
    {
        if (!request.ContactIds.Any())
        {
            return BadRequest(new { message = "At least one contact ID is required" });
        }

        var recipientEmails = new List<string>();
        var missingContacts = new List<Guid>();

        foreach (var contactId in request.ContactIds)
        {
            var contact = await _contactService.GetByIdAsync(contactId);
            if (contact == null)
            {
                missingContacts.Add(contactId);
            }
            else if (contact.IsActive)
            {
                recipientEmails.Add(contact.Email);
            }
        }

        if (missingContacts.Any())
        {
            return NotFound(new { message = $"Contacts not found: {string.Join(", ", missingContacts)}" });
        }

        if (!recipientEmails.Any())
        {
            return BadRequest(new { message = "No active contacts found in the provided list" });
        }

        return await SendTestEmailInternal(
            request.TemplateName,
            recipientEmails,
            request.TemplateData,
            request.TestReason,
            null,
            ct);
    }

    /// <summary>
    /// Send a test email directly to specific email addresses (bypass contact system)
    /// </summary>
    [HttpPost("send-to-addresses")]
    [ProducesResponseType(typeof(TestEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendToAddresses([FromBody] SendTestEmailToAddressesRequest request, CancellationToken ct)
    {
        if (!request.EmailAddresses.Any())
        {
            return BadRequest(new { message = "At least one email address is required" });
        }

        // Basic email validation
        var invalidEmails = request.EmailAddresses.Where(e => !IsValidEmail(e)).ToList();
        if (invalidEmails.Any())
        {
            return BadRequest(new { message = $"Invalid email addresses: {string.Join(", ", invalidEmails)}" });
        }

        return await SendTestEmailInternal(
            request.TemplateName,
            request.EmailAddresses,
            request.TemplateData,
            request.TestReason,
            null,
            ct);
    }

    /// <summary>
    /// Get recipient groups eligible for test emails (Purpose = TestOnly or Both)
    /// </summary>
    [HttpGet("eligible-groups")]
    [ProducesResponseType(typeof(List<RecipientGroupSummary>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEligibleGroups([FromQuery] string? clientId = null)
    {
        var allGroups = clientId != null
            ? await _groupService.GetByClientAsync(clientId)
            : await _groupService.GetAllAsync(includeInactive: false);

        var eligibleGroups = allGroups
            .Where(g => g.Purpose != GroupPurpose.Production && g.IsActive)
            .Select(g => new RecipientGroupSummary
            {
                Id = g.Id,
                Name = g.Name,
                ClientId = g.ClientId,
                Description = g.Description,
                Purpose = g.Purpose,
                Tags = g.Tags,
                IsActive = g.IsActive,
                MemberCount = g.Memberships?.Count ?? 0,
                PolicyCount = 0
            })
            .ToList();

        return Ok(eligibleGroups);
    }

    /// <summary>
    /// Preview who would receive an email if sent to a group
    /// </summary>
    [HttpGet("preview-recipients/{groupId:guid}")]
    [ProducesResponseType(typeof(RecipientPreview), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PreviewRecipients(Guid groupId)
    {
        var group = await _groupService.GetByIdAsync(groupId);
        if (group == null)
        {
            return NotFound(new { message = $"Recipient group {groupId} not found" });
        }

        var members = await _groupService.GetMembersAsync(groupId);
        var activeMembers = members.Where(m => m.IsActive).ToList();

        var preview = new RecipientPreview
        {
            TotalRecipients = activeMembers.Count,
            Recipients = activeMembers.Select(m => new RecipientPreviewItem
            {
                ContactId = m.Id,
                Name = m.Name,
                Email = m.Email,
                Organization = m.Organization,
                GroupName = group.Name
            }).ToList()
        };

        return Ok(preview);
    }

    /// <summary>
    /// Get test email delivery history
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(PaginatedResponse<TestEmailDeliveryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] Guid? groupId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? initiatedBy = null,
        [FromQuery] bool? successOnly = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var (items, totalCount) = await _testEmailRepository.GetPagedAsync(
            groupId,
            startDate,
            endDate,
            initiatedBy,
            successOnly,
            page,
            pageSize);

        var dtos = items.Select(d => new TestEmailDeliveryDto
        {
            Id = d.Id,
            RecipientGroupId = d.RecipientGroupId,
            RecipientGroupName = d.RecipientGroup?.Name,
            TemplateName = d.TemplateName,
            Subject = d.Subject,
            Recipients = d.Recipients,
            TestReason = d.TestReason,
            InitiatedBy = d.InitiatedBy,
            SentAt = d.SentAt,
            Success = d.Success,
            ErrorMessage = d.ErrorMessage,
            MessageId = d.MessageId,
            Provider = d.Provider
        }).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return Ok(new PaginatedResponse<TestEmailDeliveryDto>
        {
            Data = dtos,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalCount,
            TotalPages = totalPages,
            HasNext = page < totalPages,
            HasPrevious = page > 1
        });
    }

    /// <summary>
    /// Get a specific test email delivery record
    /// </summary>
    [HttpGet("history/{id:guid}")]
    [ProducesResponseType(typeof(TestEmailDeliveryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDelivery(Guid id)
    {
        var delivery = await _testEmailRepository.GetByIdAsync(id);
        if (delivery == null)
        {
            return NotFound(new { message = $"Test email delivery {id} not found" });
        }

        var dto = new TestEmailDeliveryDto
        {
            Id = delivery.Id,
            RecipientGroupId = delivery.RecipientGroupId,
            RecipientGroupName = delivery.RecipientGroup?.Name,
            TemplateName = delivery.TemplateName,
            Subject = delivery.Subject,
            Recipients = delivery.Recipients,
            TestReason = delivery.TestReason,
            InitiatedBy = delivery.InitiatedBy,
            SentAt = delivery.SentAt,
            Success = delivery.Success,
            ErrorMessage = delivery.ErrorMessage,
            MessageId = delivery.MessageId,
            Provider = delivery.Provider
        };

        return Ok(dto);
    }

    /// <summary>
    /// Send a test email with explicit TO, CC, and BCC recipient groups.
    /// Sends a single email with proper role-based addressing.
    /// </summary>
    [HttpPost("send-with-roles")]
    [ProducesResponseType(typeof(TestEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendWithRoles([FromBody] SendTestEmailWithRolesRequest request, CancellationToken ct)
    {
        // Validate TO group exists and is eligible
        var toGroup = await _groupService.GetByIdAsync(request.ToGroupId);
        if (toGroup == null)
        {
            return NotFound(new { message = $"TO recipient group {request.ToGroupId} not found" });
        }
        if (toGroup.Purpose == GroupPurpose.Production)
        {
            return BadRequest(new { message = $"TO group '{toGroup.Name}' is marked as Production-only and cannot receive test emails" });
        }

        // Get TO members
        var toMembers = await _groupService.GetMembersAsync(request.ToGroupId);
        var activeToMembers = toMembers.Where(m => m.IsActive).ToList();
        if (!activeToMembers.Any())
        {
            return BadRequest(new { message = $"TO group '{toGroup.Name}' has no active members" });
        }
        var toEmails = activeToMembers.Select(m => m.Email).ToList();

        // Validate CC group if provided
        List<string>? ccEmails = null;
        RecipientGroup? ccGroup = null;
        if (request.CcGroupId.HasValue)
        {
            ccGroup = await _groupService.GetByIdAsync(request.CcGroupId.Value);
            if (ccGroup == null)
            {
                return NotFound(new { message = $"CC recipient group {request.CcGroupId} not found" });
            }
            if (ccGroup.Purpose == GroupPurpose.Production)
            {
                return BadRequest(new { message = $"CC group '{ccGroup.Name}' is marked as Production-only and cannot receive test emails" });
            }
            var ccMembers = await _groupService.GetMembersAsync(request.CcGroupId.Value);
            ccEmails = ccMembers.Where(m => m.IsActive).Select(m => m.Email).ToList();
        }

        // Validate BCC group if provided
        List<string>? bccEmails = null;
        RecipientGroup? bccGroup = null;
        if (request.BccGroupId.HasValue)
        {
            bccGroup = await _groupService.GetByIdAsync(request.BccGroupId.Value);
            if (bccGroup == null)
            {
                return NotFound(new { message = $"BCC recipient group {request.BccGroupId} not found" });
            }
            if (bccGroup.Purpose == GroupPurpose.Production)
            {
                return BadRequest(new { message = $"BCC group '{bccGroup.Name}' is marked as Production-only and cannot receive test emails" });
            }
            var bccMembers = await _groupService.GetMembersAsync(request.BccGroupId.Value);
            bccEmails = bccMembers.Where(m => m.IsActive).Select(m => m.Email).ToList();
        }

        // Validate template exists
        var template = await _templateRepository.GetByNameAsync(request.TemplateName, ct);
        if (template == null)
        {
            return NotFound(new { message = $"Template '{request.TemplateName}' not found" });
        }

        // Parse template data
        var data = string.IsNullOrEmpty(request.TemplateData)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(request.TemplateData)
              ?? new Dictionary<string, object>();

        // Render template
        var renderedSubject = _renderingService.RenderTemplate(template.Subject, data);
        var renderedBody = _renderingService.RenderTemplate(template.HtmlContent ?? string.Empty, data);

        // Get initiator
        var initiatedBy = User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? "api";

        var totalRecipients = toEmails.Count + (ccEmails?.Count ?? 0) + (bccEmails?.Count ?? 0);

        _logger.LogInformation(
            "Sending role-based test email: Template={Template}, TO={ToCount}, CC={CcCount}, BCC={BccCount}, InitiatedBy={InitiatedBy}",
            request.TemplateName,
            toEmails.Count,
            ccEmails?.Count ?? 0,
            bccEmails?.Count ?? 0,
            initiatedBy);

        // Create delivery record with role information
        var delivery = new TestEmailDelivery
        {
            ToGroupId = request.ToGroupId,
            CcGroupId = request.CcGroupId,
            BccGroupId = request.BccGroupId,
            TemplateName = request.TemplateName,
            Subject = renderedSubject,
            Recipients = toEmails, // Legacy field - store TO for backward compat
            ToRecipients = toEmails,
            CcRecipients = ccEmails ?? new List<string>(),
            BccRecipients = bccEmails ?? new List<string>(),
            UsedRoleBasedSending = true,
            TestReason = request.TestReason,
            InitiatedBy = initiatedBy,
            Metadata = data.ToDictionary(
                kvp => kvp.Key,
                kvp => JsonSerializer.SerializeToElement(kvp.Value))
        };

        try
        {
            // Send email using TO/CC/BCC overload
            var result = await _emailService.SendEmailAsync(
                toEmails,
                ccEmails,
                bccEmails,
                renderedSubject,
                renderedBody,
                ct);

            delivery.Success = result.Success;
            delivery.MessageId = result.MessageId;
            delivery.Provider = result.Provider.ToString();
            delivery.ErrorMessage = result.ErrorMessage;

            await _testEmailRepository.CreateAsync(delivery);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Role-based test email sent: DeliveryId={DeliveryId}, MessageId={MessageId}, TO={ToCount}, CC={CcCount}, BCC={BccCount}",
                    delivery.Id,
                    result.MessageId,
                    toEmails.Count,
                    ccEmails?.Count ?? 0,
                    bccEmails?.Count ?? 0);

                return Ok(new TestEmailResponse
                {
                    Success = true,
                    DeliveryId = delivery.Id,
                    MessageId = result.MessageId,
                    Message = $"Test email sent successfully to {totalRecipients} recipient(s) (TO: {toEmails.Count}, CC: {ccEmails?.Count ?? 0}, BCC: {bccEmails?.Count ?? 0})",
                    SentAt = result.SentAt,
                    Recipients = toEmails,
                    RenderedSubject = renderedSubject
                });
            }

            _logger.LogWarning(
                "Role-based test email failed: DeliveryId={DeliveryId}, Error={Error}",
                delivery.Id,
                result.ErrorMessage);

            return BadRequest(new TestEmailResponse
            {
                Success = false,
                DeliveryId = delivery.Id,
                ErrorMessage = result.ErrorMessage,
                Message = $"Test email failed: {result.ErrorMessage}",
                Recipients = toEmails,
                RenderedSubject = renderedSubject
            });
        }
        catch (Exception ex)
        {
            delivery.Success = false;
            delivery.ErrorMessage = ex.Message;
            await _testEmailRepository.CreateAsync(delivery);

            _logger.LogError(ex,
                "Role-based test email exception: DeliveryId={DeliveryId}, Template={Template}",
                delivery.Id,
                request.TemplateName);

            throw;
        }
    }

    /// <summary>
    /// Preview which recipient groups would be used based on routing policies for a service/topic combination.
    /// Useful for "Load from Policy" functionality in the test email modal.
    /// </summary>
    [HttpGet("preview-by-policy")]
    [ProducesResponseType(typeof(PolicyMatchPreviewResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewByPolicy(
        [FromQuery] SourceService service,
        [FromQuery] NotificationTopic topic,
        [FromQuery] string? clientId = null)
    {
        // Get all policies matching service/topic
        var allPolicies = await _policyService.GetByServiceAndTopicAsync(service, topic);

        // Filter by client if specified, otherwise include default (null clientId) policies
        var matchedPolicies = clientId != null
            ? allPolicies.Where(p => p.ClientId == clientId || p.ClientId == null).ToList()
            : allPolicies.Where(p => p.ClientId == null).ToList();

        // Group by role
        var toGroups = new List<PolicyGroupMatch>();
        var ccGroups = new List<PolicyGroupMatch>();
        var bccGroups = new List<PolicyGroupMatch>();
        var policySummaries = new List<MatchedPolicySummary>();

        foreach (var policy in matchedPolicies.Where(p => p.IsEnabled))
        {
            var group = await _groupService.GetByIdAsync(policy.RecipientGroupId);
            if (group == null) continue;

            var members = await _groupService.GetMembersAsync(policy.RecipientGroupId);
            var activeMemberCount = members.Count(m => m.IsActive);
            var isTestEligible = group.Purpose != GroupPurpose.Production;

            var groupMatch = new PolicyGroupMatch
            {
                GroupId = group.Id,
                GroupName = group.Name,
                ActiveMemberCount = activeMemberCount,
                IsTestEligible = isTestEligible,
                Role = policy.Role
            };

            // Add to appropriate role list (avoid duplicates)
            var targetList = policy.Role switch
            {
                DeliveryRole.To => toGroups,
                DeliveryRole.Cc => ccGroups,
                DeliveryRole.Bcc => bccGroups,
                _ => toGroups
            };

            if (!targetList.Any(g => g.GroupId == group.Id))
            {
                targetList.Add(groupMatch);
            }

            policySummaries.Add(new MatchedPolicySummary
            {
                PolicyId = policy.Id,
                Service = service.ToString(),
                Topic = topic.ToString(),
                ClientId = policy.ClientId,
                Role = policy.Role,
                RecipientGroupId = policy.RecipientGroupId,
                RecipientGroupName = group.Name,
                IsTestEligible = isTestEligible,
                Priority = policy.Priority
            });
        }

        return Ok(new PolicyMatchPreviewResponse
        {
            ToGroups = toGroups,
            CcGroups = ccGroups,
            BccGroups = bccGroups,
            MatchedPolicies = policySummaries.OrderByDescending(p => p.Priority).ToList(),
            TotalToRecipients = toGroups.Sum(g => g.ActiveMemberCount),
            TotalCcRecipients = ccGroups.Sum(g => g.ActiveMemberCount),
            TotalBccRecipients = bccGroups.Sum(g => g.ActiveMemberCount)
        });
    }

    private async Task<IActionResult> SendTestEmailInternal(
        string templateName,
        List<string> recipientEmails,
        string? templateData,
        string? testReason,
        Guid? recipientGroupId,
        CancellationToken ct)
    {
        // Validate template exists
        var template = await _templateRepository.GetByNameAsync(templateName, ct);
        if (template == null)
        {
            return NotFound(new { message = $"Template '{templateName}' not found" });
        }

        // Parse template data
        var data = string.IsNullOrEmpty(templateData)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(templateData)
              ?? new Dictionary<string, object>();

        // Render template
        var renderedSubject = _renderingService.RenderTemplate(template.Subject, data);
        var renderedBody = _renderingService.RenderTemplate(template.HtmlContent ?? string.Empty, data);

        // Get initiator from user claims or default
        var initiatedBy = User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.FindFirst(ClaimTypes.Email)?.Value
            ?? "api";

        _logger.LogInformation(
            "Sending test email: Template={Template}, Recipients={RecipientCount}, GroupId={GroupId}, InitiatedBy={InitiatedBy}",
            templateName,
            recipientEmails.Count,
            recipientGroupId,
            initiatedBy);

        // Create delivery record (will be updated with result)
        var delivery = new TestEmailDelivery
        {
            RecipientGroupId = recipientGroupId,
            TemplateName = templateName,
            Subject = renderedSubject,
            Recipients = recipientEmails,
            TestReason = testReason,
            InitiatedBy = initiatedBy,
            Metadata = data.ToDictionary(
                kvp => kvp.Key,
                kvp => JsonSerializer.SerializeToElement(kvp.Value))
        };

        try
        {
            // Send email
            var result = await _emailService.SendEmailAsync(
                recipientEmails,
                renderedSubject,
                renderedBody,
                true,
                ct);

            // Update delivery record with result
            delivery.Success = result.Success;
            delivery.MessageId = result.MessageId;
            delivery.Provider = result.Provider.ToString();
            delivery.ErrorMessage = result.ErrorMessage;

            await _testEmailRepository.CreateAsync(delivery);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Test email sent successfully: DeliveryId={DeliveryId}, MessageId={MessageId}, Recipients={Recipients}",
                    delivery.Id,
                    result.MessageId,
                    string.Join(", ", recipientEmails));

                return Ok(new TestEmailResponse
                {
                    Success = true,
                    DeliveryId = delivery.Id,
                    MessageId = result.MessageId,
                    Message = $"Test email sent successfully to {recipientEmails.Count} recipient(s)",
                    SentAt = result.SentAt,
                    Recipients = recipientEmails,
                    RenderedSubject = renderedSubject
                });
            }

            _logger.LogWarning(
                "Test email failed: DeliveryId={DeliveryId}, Error={Error}",
                delivery.Id,
                result.ErrorMessage);

            return BadRequest(new TestEmailResponse
            {
                Success = false,
                DeliveryId = delivery.Id,
                ErrorMessage = result.ErrorMessage,
                Message = $"Test email failed: {result.ErrorMessage}",
                Recipients = recipientEmails,
                RenderedSubject = renderedSubject
            });
        }
        catch (Exception ex)
        {
            delivery.Success = false;
            delivery.ErrorMessage = ex.Message;
            await _testEmailRepository.CreateAsync(delivery);

            _logger.LogError(ex,
                "Test email exception: DeliveryId={DeliveryId}, Template={Template}",
                delivery.Id,
                templateName);

            throw;
        }
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
