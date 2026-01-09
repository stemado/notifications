using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationService.Routing.Migrations
{
    /// <summary>
    /// Creates the topics registry table for storing topic metadata.
    /// This enables flow visualization by providing display names, descriptions,
    /// trigger conditions, and payload schemas for notification topics.
    ///
    /// Related: notification-flow-visualization-spec.md
    /// </summary>
    public partial class AddTopicsRegistry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the topics table
            migrationBuilder.Sql(@"
                CREATE TABLE topics (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    service VARCHAR(50) NOT NULL,
                    topic VARCHAR(50) NOT NULL,
                    display_name VARCHAR(100) NOT NULL,
                    description TEXT,
                    trigger_description TEXT,
                    payload_schema JSONB,
                    docs_url VARCHAR(500),
                    is_active BOOLEAN NOT NULL DEFAULT true,
                    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
                    updated_by VARCHAR(200),
                    CONSTRAINT uq_topics_service_topic UNIQUE (service, topic)
                );

                CREATE INDEX idx_topics_service_active ON topics (service, is_active) WHERE is_active = true;
            ");

            // Seed known CensusReconciliation topics
            migrationBuilder.Sql(@"
                INSERT INTO topics (service, topic, display_name, description, trigger_description, payload_schema, docs_url, updated_by)
                VALUES
                (
                    'CensusReconciliation',
                    'ReconciliationComplete',
                    'Reconciliation Complete',
                    'Published when a census reconciliation saga completes successfully. This is the primary success notification for census processing workflows.',
                    'Triggered when: Reconciliation workflow reaches Complete state with no critical errors',
                    '{""type"": ""object"", ""properties"": {""sagaId"": {""type"": ""string"", ""description"": ""Unique identifier for the reconciliation saga""}, ""clientId"": {""type"": ""string"", ""description"": ""PlanSource client ID""}, ""clientName"": {""type"": ""string"", ""description"": ""Human-readable client name""}, ""fileCount"": {""type"": ""integer"", ""description"": ""Number of files processed""}, ""processedAt"": {""type"": ""string"", ""format"": ""date-time"", ""description"": ""Timestamp when processing completed""}}}',
                    NULL,
                    'migration-AddTopicsRegistry'
                ),
                (
                    'CensusReconciliation',
                    'DailyImportSuccess',
                    'Daily Import Success',
                    'Published when a daily census import completes successfully. Indicates that census data was processed and changes were applied to PlanSource.',
                    'Triggered when: Import workflow completes with status=Success and changes were applied',
                    '{""type"": ""object"", ""properties"": {""clientId"": {""type"": ""string"", ""description"": ""PlanSource client ID""}, ""clientName"": {""type"": ""string"", ""description"": ""Human-readable client name""}, ""fileName"": {""type"": ""string"", ""description"": ""Name of the processed file""}, ""recordCount"": {""type"": ""integer"", ""description"": ""Number of records processed""}, ""changesApplied"": {""type"": ""integer"", ""description"": ""Number of changes applied to PlanSource""}}}',
                    NULL,
                    'migration-AddTopicsRegistry'
                ),
                (
                    'CensusReconciliation',
                    'DailyImportFailure',
                    'Daily Import Failure',
                    'Published when a daily census import fails. This could be due to file errors, validation failures, or PlanSource API issues.',
                    'Triggered when: Import workflow fails or times out after max retries',
                    '{""type"": ""object"", ""properties"": {""clientId"": {""type"": ""string"", ""description"": ""PlanSource client ID""}, ""clientName"": {""type"": ""string"", ""description"": ""Human-readable client name""}, ""errorMessage"": {""type"": ""string"", ""description"": ""Error description""}, ""errorCode"": {""type"": ""string"", ""description"": ""Error classification code""}, ""failedAt"": {""type"": ""string"", ""format"": ""date-time"", ""description"": ""Timestamp when failure occurred""}}}',
                    NULL,
                    'migration-AddTopicsRegistry'
                ),
                (
                    'OneTimeSpreadsheetService',
                    'OTSScheduledRunSuccess',
                    'OTS Run Success',
                    'Published when a scheduled One-Time Spreadsheet processing run completes successfully.',
                    'Triggered when: OTS scheduled job completes all processing without errors',
                    '{""type"": ""object"", ""properties"": {""clientId"": {""type"": ""string""}, ""clientName"": {""type"": ""string""}, ""runId"": {""type"": ""string""}, ""recordsProcessed"": {""type"": ""integer""}, ""runDate"": {""type"": ""string"", ""format"": ""date-time""}}}',
                    NULL,
                    'migration-AddTopicsRegistry'
                ),
                (
                    'OneTimeSpreadsheetService',
                    'OTSScheduledRunFailure',
                    'OTS Run Failure',
                    'Published when a scheduled One-Time Spreadsheet processing run fails.',
                    'Triggered when: OTS scheduled job encounters an error or times out',
                    '{""type"": ""object"", ""properties"": {""clientId"": {""type"": ""string""}, ""clientName"": {""type"": ""string""}, ""runId"": {""type"": ""string""}, ""errorMessage"": {""type"": ""string""}, ""failedAt"": {""type"": ""string"", ""format"": ""date-time""}}}',
                    NULL,
                    'migration-AddTopicsRegistry'
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS topics;");
        }
    }
}
