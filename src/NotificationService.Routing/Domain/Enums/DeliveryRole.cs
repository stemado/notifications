namespace NotificationService.Routing.Domain.Enums;

/// <summary>
/// Defines the delivery role for a recipient in an outbound notification
/// </summary>
public enum DeliveryRole
{
    /// <summary>
    /// Primary recipient (To field)
    /// </summary>
    To,

    /// <summary>
    /// Carbon copy recipient (CC field)
    /// </summary>
    Cc,

    /// <summary>
    /// Blind carbon copy recipient (BCC field)
    /// </summary>
    Bcc
}
