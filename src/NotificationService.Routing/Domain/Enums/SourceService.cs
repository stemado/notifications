namespace NotificationService.Routing.Domain.Enums;

/// <summary>
/// Identifies the source service that publishes outbound notifications
/// </summary>
public enum SourceService
{
    /// <summary>
    /// Census automation and file processing
    /// </summary>
    CensusAutomation,

    /// <summary>
    /// Payroll file generation service
    /// </summary>
    PayrollFileGeneration,

    /// <summary>
    /// Census reconciliation workflow service
    /// </summary>
    CensusReconciliation,

    /// <summary>
    /// Census orchestration service
    /// </summary>
    CensusOrchestration,

    /// <summary>
    /// PlanSource integration service
    /// </summary>
    PlanSourceIntegration,

    /// <summary>
    /// Import history processor service
    /// </summary>
    ImportHistoryProcessor,

    /// <summary>
    /// Import processor service (PlanSource imports)
    /// </summary>
    ImportProcessor,

    /// <summary>
    /// OneTimeSpreadsheets service for OTS payroll processing
    /// </summary>
    OneTimeSpreadsheetService
}
