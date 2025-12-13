using Microsoft.AspNetCore.Mvc;
using NotificationService.Api.Events;
using NotificationService.Client.Events;

// Aliases to avoid ambiguity between Api and Client event types
using ApiSagaStuckEvent = NotificationService.Api.Events.SagaStuckEvent;
using ClientSagaStuckEvent = NotificationService.Client.Events.SagaStuckEvent;
using ApiSLABreachEvent = NotificationService.Api.Events.SLABreachEvent;
using ClientSLABreachEvent = NotificationService.Client.Events.SLABreachEvent;
using ApiPlanSourceOperationFailedEvent = NotificationService.Api.Events.PlanSourceOperationFailedEvent;
using ClientPlanSourceOperationFailedEvent = NotificationService.Client.Events.PlanSourceOperationFailedEvent;
using ApiAggregateGenerationStalledEvent = NotificationService.Api.Events.AggregateGenerationStalledEvent;
using ClientAggregateGenerationStalledEvent = NotificationService.Client.Events.AggregateGenerationStalledEvent;

namespace NotificationService.Api.Controllers;

/// <summary>
/// Controller for receiving and processing notification events directly.
/// This provides an alternative to using the NotificationService.Client library
/// for services that want to post raw events.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventHandler<ApiSagaStuckEvent> _sagaStuckHandler;
    private readonly IEventHandler<ImportCompletedEvent> _importCompletedHandler;
    private readonly IEventHandler<ImportFailedEvent> _importFailedHandler;
    private readonly IEventHandler<EscalationCreatedEvent> _escalationCreatedHandler;
    private readonly IEventHandler<FileProcessingErrorEvent> _fileProcessingErrorHandler;
    private readonly IEventHandler<FilePickedUpEvent> _filePickedUpHandler;
    private readonly IEventHandler<ApiSLABreachEvent> _slaBreachHandler;
    private readonly IEventHandler<ApiPlanSourceOperationFailedEvent> _planSourceFailedHandler;
    private readonly IEventHandler<ApiAggregateGenerationStalledEvent> _aggregateStalledHandler;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IEventHandler<ApiSagaStuckEvent> sagaStuckHandler,
        IEventHandler<ImportCompletedEvent> importCompletedHandler,
        IEventHandler<ImportFailedEvent> importFailedHandler,
        IEventHandler<EscalationCreatedEvent> escalationCreatedHandler,
        IEventHandler<FileProcessingErrorEvent> fileProcessingErrorHandler,
        IEventHandler<FilePickedUpEvent> filePickedUpHandler,
        IEventHandler<ApiSLABreachEvent> slaBreachHandler,
        IEventHandler<ApiPlanSourceOperationFailedEvent> planSourceFailedHandler,
        IEventHandler<ApiAggregateGenerationStalledEvent> aggregateStalledHandler,
        ILogger<EventsController> logger)
    {
        _sagaStuckHandler = sagaStuckHandler;
        _importCompletedHandler = importCompletedHandler;
        _importFailedHandler = importFailedHandler;
        _escalationCreatedHandler = escalationCreatedHandler;
        _fileProcessingErrorHandler = fileProcessingErrorHandler;
        _filePickedUpHandler = filePickedUpHandler;
        _slaBreachHandler = slaBreachHandler;
        _planSourceFailedHandler = planSourceFailedHandler;
        _aggregateStalledHandler = aggregateStalledHandler;
        _logger = logger;
    }

    /// <summary>
    /// Process a saga stuck event
    /// </summary>
    [HttpPost("saga-stuck")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleSagaStuck([FromBody] ClientSagaStuckEvent evt)
    {
        if (evt.SagaId == Guid.Empty)
        {
            return BadRequest(new { error = "SagaId is required" });
        }

        _logger.LogInformation("Received SagaStuckEvent: SagaId={SagaId}, ClientId={ClientId}",
            evt.SagaId, evt.ClientId);

        // Map from Client event to Api event for handler
        var apiEvent = new ApiSagaStuckEvent
        {
            SagaId = evt.SagaId,
            ClientId = Guid.TryParse(evt.ClientId, out var clientGuid) ? clientGuid : Guid.Empty,
            ClientName = evt.ClientName,
            StuckDuration = evt.StuckDuration,
            TenantId = evt.TenantId
        };

        await _sagaStuckHandler.Handle(apiEvent);

        return Accepted(new { message = "Event processed", sagaId = evt.SagaId });
    }

    /// <summary>
    /// Process an import completed event
    /// </summary>
    [HttpPost("import-completed")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleImportCompleted([FromBody] ImportCompletedEvent evt)
    {
        if (evt.SagaId == Guid.Empty)
        {
            return BadRequest(new { error = "SagaId is required" });
        }

        _logger.LogInformation("Received ImportCompletedEvent: SagaId={SagaId}, ClientId={ClientId}, TotalRecords={TotalRecords}",
            evt.SagaId, evt.ClientId, evt.TotalRecords);

        await _importCompletedHandler.Handle(evt);

        return Accepted(new { message = "Event processed", sagaId = evt.SagaId });
    }

    /// <summary>
    /// Process an import failed event
    /// </summary>
    [HttpPost("import-failed")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleImportFailed([FromBody] ImportFailedEvent evt)
    {
        if (evt.SagaId == Guid.Empty)
        {
            return BadRequest(new { error = "SagaId is required" });
        }

        _logger.LogWarning("Received ImportFailedEvent: SagaId={SagaId}, ClientId={ClientId}, Error={ErrorMessage}",
            evt.SagaId, evt.ClientId, evt.ErrorMessage);

        await _importFailedHandler.Handle(evt);

        return Accepted(new { message = "Event processed", sagaId = evt.SagaId });
    }

    /// <summary>
    /// Process an escalation created event
    /// </summary>
    [HttpPost("escalation-created")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleEscalationCreated([FromBody] EscalationCreatedEvent evt)
    {
        if (evt.EscalationId == Guid.Empty)
        {
            return BadRequest(new { error = "EscalationId is required" });
        }

        _logger.LogWarning("Received EscalationCreatedEvent: EscalationId={EscalationId}, Type={EscalationType}, ClientId={ClientId}",
            evt.EscalationId, evt.EscalationType, evt.ClientId);

        await _escalationCreatedHandler.Handle(evt);

        return Accepted(new { message = "Event processed", escalationId = evt.EscalationId });
    }

    /// <summary>
    /// Process a file processing error event
    /// </summary>
    [HttpPost("file-processing-error")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleFileProcessingError([FromBody] FileProcessingErrorEvent evt)
    {
        if (string.IsNullOrEmpty(evt.ClientId))
        {
            return BadRequest(new { error = "ClientId is required" });
        }

        _logger.LogWarning("Received FileProcessingErrorEvent: ClientId={ClientId}, ErrorType={ErrorType}",
            evt.ClientId, evt.ErrorType);

        await _fileProcessingErrorHandler.Handle(evt);

        return Accepted(new { message = "Event processed", clientId = evt.ClientId, errorType = evt.ErrorType });
    }

    /// <summary>
    /// Process a file picked up event
    /// </summary>
    [HttpPost("file-picked-up")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleFilePickedUp([FromBody] FilePickedUpEvent evt)
    {
        if (evt.SagaId == Guid.Empty)
        {
            return BadRequest(new { error = "SagaId is required" });
        }

        if (string.IsNullOrEmpty(evt.ClientId))
        {
            return BadRequest(new { error = "ClientId is required" });
        }

        _logger.LogInformation("Received FilePickedUpEvent: SagaId={SagaId}, ClientId={ClientId}, FileName={FileName}",
            evt.SagaId, evt.ClientId, evt.FileName);

        await _filePickedUpHandler.Handle(evt);

        return Accepted(new { message = "Event processed", sagaId = evt.SagaId });
    }

    /// <summary>
    /// Process an SLA breach event
    /// </summary>
    [HttpPost("sla-breach")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleSLABreach([FromBody] ClientSLABreachEvent evt)
    {
        if (evt.SagaId == Guid.Empty)
        {
            return BadRequest(new { error = "SagaId is required" });
        }

        _logger.LogWarning("Received SLABreachEvent: SagaId={SagaId}, ClientId={ClientId}, SLAType={SLAType}",
            evt.SagaId, evt.ClientId, evt.SLAType);

        var apiEvent = new ApiSLABreachEvent
        {
            SagaId = evt.SagaId,
            ClientId = Guid.TryParse(evt.ClientId, out var clientGuid) ? clientGuid : Guid.Empty,
            ClientName = evt.ClientName,
            SLAType = evt.SLAType,
            ThresholdMinutes = evt.ThresholdMinutes,
            ActualMinutes = evt.ActualMinutes,
            CurrentState = evt.CurrentState,
            Severity = MapSeverity(evt.Severity),
            DetectedAt = evt.DetectedAt,
            TenantId = evt.TenantId,
            CorrelationId = evt.CorrelationId
        };

        await _slaBreachHandler.Handle(apiEvent);

        return Accepted(new { message = "Event processed", sagaId = evt.SagaId });
    }

    /// <summary>
    /// Process a PlanSource operation failed event
    /// </summary>
    [HttpPost("plansource-failed")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandlePlanSourceOperationFailed([FromBody] ClientPlanSourceOperationFailedEvent evt)
    {
        if (evt.SagaId == Guid.Empty)
        {
            return BadRequest(new { error = "SagaId is required" });
        }

        _logger.LogWarning("Received PlanSourceOperationFailedEvent: SagaId={SagaId}, ClientId={ClientId}, Operation={Operation}",
            evt.SagaId, evt.ClientId, evt.OperationType);

        var apiEvent = new ApiPlanSourceOperationFailedEvent
        {
            SagaId = evt.SagaId,
            ClientId = Guid.TryParse(evt.ClientId, out var clientGuid) ? clientGuid : Guid.Empty,
            ClientName = evt.ClientName,
            OperationType = evt.OperationType,
            ErrorMessage = evt.ErrorMessage,
            ErrorCode = evt.ErrorCode,
            IsRetryable = evt.IsRetryable,
            AttemptNumber = evt.AttemptNumber,
            MaxRetries = evt.MaxRetries,
            CurrentState = evt.CurrentState,
            Severity = MapSeverity(evt.Severity),
            FailedAt = evt.FailedAt,
            TenantId = evt.TenantId,
            CorrelationId = evt.CorrelationId
        };

        await _planSourceFailedHandler.Handle(apiEvent);

        return Accepted(new { message = "Event processed", sagaId = evt.SagaId });
    }

    /// <summary>
    /// Process an aggregate generation stalled event
    /// </summary>
    [HttpPost("aggregate-stalled")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleAggregateGenerationStalled([FromBody] ClientAggregateGenerationStalledEvent evt)
    {
        if (evt.SagaId == Guid.Empty)
        {
            return BadRequest(new { error = "SagaId is required" });
        }

        _logger.LogWarning("Received AggregateGenerationStalledEvent: SagaId={SagaId}, ClientId={ClientId}, WaitCount={WaitCount}",
            evt.SagaId, evt.ClientId, evt.WaitCount);

        var apiEvent = new ApiAggregateGenerationStalledEvent
        {
            SagaId = evt.SagaId,
            ClientId = Guid.TryParse(evt.ClientId, out var clientGuid) ? clientGuid : Guid.Empty,
            ClientName = evt.ClientName,
            WaitCount = evt.WaitCount,
            MaxWaitCount = evt.MaxWaitCount,
            MinutesWaiting = evt.MinutesWaiting,
            FileName = evt.FileName,
            Severity = MapSeverity(evt.Severity),
            DetectedAt = evt.DetectedAt,
            TenantId = evt.TenantId,
            CorrelationId = evt.CorrelationId
        };

        await _aggregateStalledHandler.Handle(apiEvent);

        return Accepted(new { message = "Event processed", sagaId = evt.SagaId });
    }

    private static Domain.Enums.NotificationSeverity MapSeverity(Client.Models.NotificationSeverity severity)
    {
        return severity switch
        {
            Client.Models.NotificationSeverity.Info => Domain.Enums.NotificationSeverity.Info,
            Client.Models.NotificationSeverity.Warning => Domain.Enums.NotificationSeverity.Warning,
            Client.Models.NotificationSeverity.Urgent => Domain.Enums.NotificationSeverity.Urgent,
            Client.Models.NotificationSeverity.Critical => Domain.Enums.NotificationSeverity.Critical,
            _ => Domain.Enums.NotificationSeverity.Info
        };
    }
}
