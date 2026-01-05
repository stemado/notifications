namespace NotificationService.Routing.Domain.Enums;

/// <summary>
/// Defines the specific notification topics that can be routed
/// </summary>
public enum NotificationTopic
{
    // Census Automation
    /// <summary>
    /// Daily import completed successfully
    /// </summary>
    DailyImportSuccess,

    /// <summary>
    /// Daily import failed
    /// </summary>
    DailyImportFailure,

    /// <summary>
    /// Schema validation error detected
    /// </summary>
    SchemaValidationError,

    /// <summary>
    /// Record count mismatch detected
    /// </summary>
    RecordCountMismatch,

    /// <summary>
    /// File processing has started
    /// </summary>
    FileProcessingStarted,

    /// <summary>
    /// File processing completed
    /// </summary>
    FileProcessingCompleted,

    // Payroll
    /// <summary>
    /// Payroll file was generated successfully
    /// </summary>
    PayrollFileGenerated,

    /// <summary>
    /// Payroll file generation failed
    /// </summary>
    PayrollFileError,

    /// <summary>
    /// Payroll file is pending approval
    /// </summary>
    PayrollFilePending,

    /// <summary>
    /// Payroll file was approved
    /// </summary>
    PayrollFileApproved,

    // Reconciliation
    /// <summary>
    /// Reconciliation workflow completed
    /// </summary>
    ReconciliationComplete,

    /// <summary>
    /// Workflow was escalated for attention
    /// </summary>
    ReconciliationEscalation,

    /// <summary>
    /// Workflow is stuck and needs intervention
    /// </summary>
    WorkflowStuck,

    /// <summary>
    /// Manual intervention is required
    /// </summary>
    ManualInterventionRequired,

    /// <summary>
    /// Workflow retry limit exceeded
    /// </summary>
    RetryLimitExceeded,

    // General
    /// <summary>
    /// System-level alert
    /// </summary>
    SystemAlert,

    /// <summary>
    /// Service health check failure
    /// </summary>
    HealthCheckFailure,

    /// <summary>
    /// Custom topic for ad-hoc notifications
    /// </summary>
    Custom,

    // Service Errors
    /// <summary>
    /// Import processor service error
    /// </summary>
    ImportProcessorError,

    /// <summary>
    /// Import history processor service error
    /// </summary>
    ImportHistoryProcessorError,

    /// <summary>
    /// Census orchestration service error
    /// </summary>
    OrchestrationServiceError,

    /// <summary>
    /// Service health has degraded
    /// </summary>
    ServiceHealthDegraded,

    /// <summary>
    /// Service has recovered from degraded state
    /// </summary>
    ServiceRecovered,

    /// <summary>
    /// Database connection error
    /// </summary>
    DatabaseConnectionError,

    /// <summary>
    /// External service timeout
    /// </summary>
    ExternalServiceTimeout,

    /// <summary>
    /// Unhandled exception in service
    /// </summary>
    UnhandledException,

    // OTS (OneTimeSpreadsheets)
    /// <summary>
    /// OTS scheduled run completed successfully
    /// </summary>
    OTSScheduledRunSuccess,

    /// <summary>
    /// OTS scheduled run failed
    /// </summary>
    OTSScheduledRunFailure
}
