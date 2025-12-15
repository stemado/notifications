namespace NotificationService.Routing.Domain.Enums;

/// <summary>
/// Defines the intended purpose of a recipient group
/// </summary>
public enum GroupPurpose
{
    /// <summary>
    /// Group is used for production notification routing only
    /// </summary>
    Production,

    /// <summary>
    /// Group is designated for test email sends only
    /// </summary>
    TestOnly,

    /// <summary>
    /// Group can be used for both production routing and test emails
    /// </summary>
    Both
}
