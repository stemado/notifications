# NotificationService.Api - Proper Dispatch Integration Plan

## Problem Statement

The notification dispatch system was designed to:
1. Create notifications
2. Route them through channels based on user preferences
3. Track delivery via `NotificationDelivery` records

**Current State**: The infrastructure exists and works for event-driven notifications (escalations, import failures, etc.), but two key entry points bypass the dispatcher:

1. **NotificationsController.CreateNotification** - Creates notification + pushes to SignalR only
2. **TemplatesController.SendTemplatedEmail** - Sends directly via email service with no tracking

## Architecture Overview

### What Works (Event-Driven Flow)
```
Event (EscalationCreated, ImportFailed, etc.)
    │
    ▼
EventHandler (e.g., EscalationCreatedNotificationHandler)
    │
    ├──► INotificationService.CreateAsync() → Creates Notification record
    │
    └──► INotificationDispatcher.DispatchAsync()
             │
             ├──► Check user subscriptions
             │
             └──► For each enabled channel:
                      │
                      ├──► SignalRChannel.DeliverAsync() → Push to WebSocket
                      ├──► EmailChannel.DeliverAsync() → Creates NotificationDelivery → Sends email
                      ├──► SmsChannel.DeliverAsync() → Creates NotificationDelivery → Sends SMS
                      └──► etc.
```

### What's Broken

**NotificationsController (line 114-119)**:
```csharp
var notification = await _notificationService.CreateAsync(request);

// ONLY SignalR - dispatcher never called!
await _hubContext.Clients.User(...).SendAsync("NewNotification", notification);
```

**TemplatesController (line 384)**:
```csharp
// Direct send - no tracking at all!
await _emailService.SendEmailAsync(...);
```

---

## Implementation Plan

### Phase 1: Fix NotificationsController Dispatch

**Goal**: All notifications created via the API should go through the dispatcher for multi-channel delivery with tracking.

**File**: `NotificationService.Api/Controllers/NotificationsController.cs`

#### Task 1.1: Inject INotificationDispatcher
```csharp
private readonly INotificationDispatcher _dispatcher;

public NotificationsController(
    INotificationService notificationService,
    IHubContext<NotificationHub> hubContext,
    INotificationDispatcher dispatcher,  // ADD
    ILogger<NotificationsController> logger)
{
    _dispatcher = dispatcher;  // ADD
    // ...
}
```

#### Task 1.2: Update CreateNotification endpoint
```csharp
[HttpPost]
public async Task<ActionResult<Notification>> CreateNotification([FromBody] CreateNotificationRequest request)
{
    try
    {
        var notification = await _notificationService.CreateAsync(request);

        // REPLACE direct SignalR push with dispatcher
        await _dispatcher.DispatchAsync(notification);

        _logger.LogInformation("Created and dispatched notification {NotificationId} for user {UserId}",
            notification.Id, notification.UserId);

        return CreatedAtAction(nameof(GetNotification), new { id = notification.Id }, notification);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating notification");
        return StatusCode(500, "Error creating notification");
    }
}
```

#### Task 1.3: Update CreateOrUpdateNotification endpoint (same pattern)

---

### Phase 2: Add Tracked Email Sending to TemplatesController

**Goal**: Templated emails should create `NotificationDelivery` records for tracking.

**File**: `NotificationService.Api/Controllers/TemplatesController.cs`

#### Option A: Create NotificationDelivery Directly (Simpler)

For ad-hoc email sends that don't need a full notification record:

##### Task 2.1: Inject INotificationDeliveryRepository
```csharp
private readonly INotificationDeliveryRepository _deliveryRepository;

public TemplatesController(
    IEmailTemplateRepository templateRepository,
    ITemplateRenderingService renderingService,
    IEmailService emailService,
    INotificationDeliveryRepository deliveryRepository,  // ADD
    ILogger<TemplatesController> logger)
{
    _deliveryRepository = deliveryRepository;  // ADD
    // ...
}
```

##### Task 2.2: Update SendTemplatedEmail to track delivery
```csharp
[HttpPost("send")]
public async Task<IActionResult> SendTemplatedEmail([FromBody] SendTemplatedEmailRequest request, CancellationToken ct)
{
    // ... validation and template lookup ...

    // Create delivery record BEFORE sending
    var delivery = new NotificationDelivery
    {
        Id = Guid.NewGuid(),
        NotificationId = Guid.Empty, // No associated notification for ad-hoc emails
        Channel = NotificationChannel.Email,
        Status = DeliveryStatus.Processing,
        AttemptCount = 1,
        MaxAttempts = 1,
        CreatedAt = DateTime.UtcNow
    };

    try
    {
        var result = await _emailService.SendEmailAsync(
            request.Recipients,
            renderedSubject,
            renderedBody,
            true,
            ct);

        if (result.Success)
        {
            delivery.Status = DeliveryStatus.Delivered;
            delivery.DeliveredAt = DateTime.UtcNow;
            delivery.ResponseData = JsonSerializer.Serialize(new
            {
                messageId = result.MessageId,
                provider = result.Provider,
                templateName = request.TemplateName,
                recipients = request.Recipients
            });

            await _deliveryRepository.CreateAsync(delivery);

            return Ok(new SendEmailResponse(
                Success: true,
                MessageId: result.MessageId,
                DeliveryId: delivery.Id,  // Return for tracking
                Message: $"Email sent and tracked via {result.Provider}",
                SentAt: result.SentAt
            ));
        }

        delivery.Status = DeliveryStatus.Failed;
        delivery.FailedAt = DateTime.UtcNow;
        delivery.ErrorMessage = result.ErrorMessage;
        await _deliveryRepository.CreateAsync(delivery);

        return BadRequest(new SendEmailResponse(
            Success: false,
            DeliveryId: delivery.Id,
            ErrorMessage: result.ErrorMessage
        ));
    }
    catch (Exception ex)
    {
        delivery.Status = DeliveryStatus.Failed;
        delivery.FailedAt = DateTime.UtcNow;
        delivery.ErrorMessage = ex.Message;
        await _deliveryRepository.CreateAsync(delivery);
        throw;
    }
}
```

#### Option B: Create Full Notification + Dispatch (More Complete)

For emails that should also appear in the notification UI:

##### Task 2.3: Add SendTrackedEmail endpoint
```csharp
[HttpPost("send-tracked")]
public async Task<IActionResult> SendTrackedEmail(
    [FromBody] SendTrackedEmailRequest request,
    [FromServices] INotificationService notificationService,
    [FromServices] INotificationDispatcher dispatcher,
    CancellationToken ct)
{
    // Validate template exists
    var template = await _templateRepository.GetByNameAsync(request.TemplateName, ct);
    if (template == null)
        return NotFound($"Template '{request.TemplateName}' not found");

    // Create notification record
    var notification = await notificationService.CreateAsync(new CreateNotificationRequest
    {
        UserId = request.UserId ?? Guid.Parse("00000000-0000-0000-0000-000000000001"), // System user
        Title = $"Email: {template.Subject}",
        Message = $"Email sent to {string.Join(", ", request.Recipients)}",
        Severity = NotificationSeverity.Info,
        Metadata = new Dictionary<string, object>
        {
            ["templateName"] = request.TemplateName,
            ["recipients"] = request.Recipients,
            ["isTemplatedEmail"] = true
        }
    });

    // Dispatch through all enabled channels (including email)
    await dispatcher.DispatchAsync(notification);

    return Ok(new { notificationId = notification.Id, message = "Email queued for delivery" });
}
```

---

### Phase 3: Update Frontend

**Goal**: Revert the incorrect changes and use the proper API.

#### Task 3.1: Revert template detail pages

**Files**:
- `Cal.ImportPulse/app/operations/app/orchestration/templates/[id]/page.tsx`
- `Cal.ImportPulse/app/operations/app/dashboard/templates/[id]/page.tsx`

Revert `handleSendTest` to use `sendTemplatedEmail()` instead of `notificationApi.create()`.

#### Task 3.2: Update response handling for delivery tracking

Once the backend returns `DeliveryId`, update the UI to show tracking info:
```typescript
const result = await sendTemplatedEmail({...});
setTestSuccess(true);
setTestMessage(`Email sent! Delivery ID: ${result.deliveryId}`);
```

---

### Phase 4: Update SendEmailResponse DTO

**File**: `NotificationService.Api/Controllers/TemplatesController.cs` (or separate DTOs file)

```csharp
public record SendEmailResponse(
    bool Success,
    string? MessageId = null,
    Guid? DeliveryId = null,  // ADD for tracking reference
    string? Message = null,
    DateTime? SentAt = null,
    string? ErrorMessage = null
);
```

**Frontend type** (`Cal.ImportPulse/app/operations/types/notifications.ts`):
```typescript
export interface SendEmailResponse {
  success: boolean;
  messageId?: string;
  deliveryId?: string;  // ADD
  message?: string;
  sentAt?: string;
  errorMessage?: string;
}
```

---

## Summary of Changes

| Component | Change | Priority |
|-----------|--------|----------|
| NotificationsController | Inject + call `INotificationDispatcher.DispatchAsync()` | High |
| TemplatesController | Add delivery tracking to `SendTemplatedEmail` | High |
| SendEmailResponse | Add `DeliveryId` field | Medium |
| Frontend templates pages | Revert to use `sendTemplatedEmail()` | High |
| Frontend types | Add `deliveryId` to response type | Medium |

## Testing Checklist

- [ ] Create notification via API → verify delivery records created for each enabled channel
- [ ] Send templated email → verify `NotificationDelivery` record created
- [ ] Check delivery history endpoint shows new records
- [ ] Check delivery stats endpoint reflects new sends
- [ ] Verify SignalR still pushes notifications to connected clients
- [ ] Test email failure → verify delivery record shows failed status

## Files to Modify

### Backend
1. `NotificationService.Api/Controllers/NotificationsController.cs`
2. `NotificationService.Api/Controllers/TemplatesController.cs`
3. `NotificationService.Domain/DTOs/SendEmailResponse.cs` (or inline record)

### Frontend
1. `Cal.ImportPulse/app/operations/app/orchestration/templates/[id]/page.tsx`
2. `Cal.ImportPulse/app/operations/app/dashboard/templates/[id]/page.tsx`
3. `Cal.ImportPulse/app/operations/types/notifications.ts`
