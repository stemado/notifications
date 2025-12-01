using Microsoft.AspNetCore.Mvc;
using NotificationService.Api.Events;
using NotificationService.Client.Events;

// Alias to avoid ambiguity between Api and Client SagaStuckEvent
using ApiSagaStuckEvent = NotificationService.Api.Events.SagaStuckEvent;
using ClientSagaStuckEvent = NotificationService.Client.Events.SagaStuckEvent;

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
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IEventHandler<ApiSagaStuckEvent> sagaStuckHandler,
        IEventHandler<ImportCompletedEvent> importCompletedHandler,
        IEventHandler<ImportFailedEvent> importFailedHandler,
        IEventHandler<EscalationCreatedEvent> escalationCreatedHandler,
        IEventHandler<FileProcessingErrorEvent> fileProcessingErrorHandler,
        ILogger<EventsController> logger)
    {
        _sagaStuckHandler = sagaStuckHandler;
        _importCompletedHandler = importCompletedHandler;
        _importFailedHandler = importFailedHandler;
        _escalationCreatedHandler = escalationCreatedHandler;
        _fileProcessingErrorHandler = fileProcessingErrorHandler;
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
}
