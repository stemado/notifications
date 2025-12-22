using System.Text.Json;
using NotificationService.Routing.Domain.Enums;

namespace NotificationService.Routing.DTOs;

/// <summary>
/// Request to send a test email to all members of a recipient group
/// </summary>
public record SendTestEmailToGroupRequest
{
    /// <summary>
    /// Name of the email template to use
    /// </summary>
    public required string TemplateName { get; init; }

    /// <summary>
    /// ID of the recipient group to send to
    /// </summary>
    public required Guid RecipientGroupId { get; init; }

    /// <summary>
    /// Template variable data as JSON string
    /// </summary>
    public string? TemplateData { get; init; }

    /// <summary>
    /// Reason for sending this test email (for audit purposes)
    /// </summary>
    public string? TestReason { get; init; }
}

/// <summary>
/// Request to send a test email to specific contacts (ad-hoc)
/// </summary>
public record SendTestEmailToContactsRequest
{
    /// <summary>
    /// Name of the email template to use
    /// </summary>
    public required string TemplateName { get; init; }

    /// <summary>
    /// List of contact IDs to send to
    /// </summary>
    public required List<Guid> ContactIds { get; init; }

    /// <summary>
    /// Template variable data as JSON string
    /// </summary>
    public string? TemplateData { get; init; }

    /// <summary>
    /// Reason for sending this test email (for audit purposes)
    /// </summary>
    public string? TestReason { get; init; }
}

/// <summary>
/// Request to send a test email to specific email addresses (bypass contacts)
/// </summary>
public record SendTestEmailToAddressesRequest
{
    /// <summary>
    /// Name of the email template to use
    /// </summary>
    public required string TemplateName { get; init; }

    /// <summary>
    /// List of email addresses to send to
    /// </summary>
    public required List<string> EmailAddresses { get; init; }

    /// <summary>
    /// Template variable data as JSON string
    /// </summary>
    public string? TemplateData { get; init; }

    /// <summary>
    /// Reason for sending this test email (for audit purposes)
    /// </summary>
    public string? TestReason { get; init; }
}

/// <summary>
/// Response from a test email send operation
/// </summary>
public record TestEmailResponse
{
    public bool Success { get; init; }
    public Guid? DeliveryId { get; init; }
    public string? MessageId { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Message { get; init; }
    public DateTime? SentAt { get; init; }
    public List<string> Recipients { get; init; } = new();
    public string? RenderedSubject { get; init; }
}

/// <summary>
/// Preview of who would receive an email based on routing policy
/// </summary>
public record RecipientPreview
{
    public int TotalRecipients { get; init; }
    public List<RecipientPreviewItem> Recipients { get; init; } = new();
}

/// <summary>
/// Individual recipient in a preview
/// </summary>
public record RecipientPreviewItem
{
    public Guid ContactId { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public string? Organization { get; init; }
    public required string GroupName { get; init; }
}

/// <summary>
/// Test email delivery history item
/// </summary>
public record TestEmailDeliveryDto
{
    public Guid Id { get; init; }
    public Guid? RecipientGroupId { get; init; }
    public string? RecipientGroupName { get; init; }
    public required string TemplateName { get; init; }
    public required string Subject { get; init; }
    public List<string> Recipients { get; init; } = new();
    public string? TestReason { get; init; }
    public required string InitiatedBy { get; init; }
    public DateTime SentAt { get; init; }
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? MessageId { get; init; }
    public string? Provider { get; init; }
}

/// <summary>
/// Parameters for querying test email history
/// </summary>
public record TestEmailHistoryParams
{
    public Guid? RecipientGroupId { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? InitiatedBy { get; init; }
    public bool? SuccessOnly { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Paginated response wrapper
/// </summary>
public record PaginatedResponse<T>
{
    public List<T> Data { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages { get; init; }
    public bool HasNext { get; init; }
    public bool HasPrevious { get; init; }
}

/// <summary>
/// Request to send a test email with explicit TO, CC, and BCC recipient groups.
/// Enables testing of emails with proper role-based addressing.
/// </summary>
public record SendTestEmailWithRolesRequest
{
    /// <summary>
    /// Name of the email template to use
    /// </summary>
    public required string TemplateName { get; init; }

    /// <summary>
    /// Group ID for TO recipients (required - at least one primary recipient needed)
    /// </summary>
    public required Guid ToGroupId { get; init; }

    /// <summary>
    /// Group ID for CC recipients (optional)
    /// </summary>
    public Guid? CcGroupId { get; init; }

    /// <summary>
    /// Group ID for BCC recipients (optional)
    /// </summary>
    public Guid? BccGroupId { get; init; }

    /// <summary>
    /// Template variable data as JSON string
    /// </summary>
    public string? TemplateData { get; init; }

    /// <summary>
    /// Reason for sending this test email (for audit purposes)
    /// </summary>
    public string? TestReason { get; init; }
}

/// <summary>
/// Response showing which groups/recipients are matched by policy criteria
/// </summary>
public record PolicyMatchPreviewResponse
{
    public List<PolicyGroupMatch> ToGroups { get; init; } = new();
    public List<PolicyGroupMatch> CcGroups { get; init; } = new();
    public List<PolicyGroupMatch> BccGroups { get; init; } = new();
    public List<MatchedPolicySummary> MatchedPolicies { get; init; } = new();
    public int TotalToRecipients { get; init; }
    public int TotalCcRecipients { get; init; }
    public int TotalBccRecipients { get; init; }
}

/// <summary>
/// Group matched by a policy with member count
/// </summary>
public record PolicyGroupMatch
{
    public Guid GroupId { get; init; }
    public required string GroupName { get; init; }
    public int ActiveMemberCount { get; init; }
    public bool IsTestEligible { get; init; }
    public DeliveryRole Role { get; init; }
}

/// <summary>
/// Summary of a matched routing policy
/// </summary>
public record MatchedPolicySummary
{
    public Guid PolicyId { get; init; }
    public required string Service { get; init; }
    public required string Topic { get; init; }
    public string? ClientId { get; init; }
    public DeliveryRole Role { get; init; }
    public Guid RecipientGroupId { get; init; }
    public required string RecipientGroupName { get; init; }
    public bool IsTestEligible { get; init; }
    public int Priority { get; init; }
}

/// <summary>
/// Extended test email delivery DTO with role information
/// </summary>
public record TestEmailDeliveryWithRolesDto : TestEmailDeliveryDto
{
    public Guid? ToGroupId { get; init; }
    public string? ToGroupName { get; init; }
    public List<string> ToRecipients { get; init; } = new();

    public Guid? CcGroupId { get; init; }
    public string? CcGroupName { get; init; }
    public List<string> CcRecipients { get; init; } = new();

    public Guid? BccGroupId { get; init; }
    public string? BccGroupName { get; init; }
    public List<string> BccRecipients { get; init; } = new();

    public bool UsedRoleBasedSending { get; init; }
}
