namespace NotificationService.Api.Events;

/// <summary>
/// Generic event handler interface
/// </summary>
/// <typeparam name="TEvent">The event type to handle</typeparam>
public interface IEventHandler<in TEvent> where TEvent : class
{
    Task Handle(TEvent evt);
}
