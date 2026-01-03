using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationService.Routing.Migrations
{
    /// <summary>
    /// Updates the service name for DailyImportSuccess routing policies from
    /// 'CensusAutomation' to 'CensusReconciliation'.
    ///
    /// Root Cause: The ReconciliationCompletedEventHandler emits events with
    /// Service="CensusReconciliation", but existing policies were configured
    /// with Service="CensusAutomation", causing a mismatch.
    ///
    /// Related: notification-flow-fix-spec.md
    /// </summary>
    public partial class UpdateDailyImportSuccessServiceName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update all DailyImportSuccess policies to use CensusReconciliation
            // The event is emitted from the reconciliation saga, so CensusReconciliation
            // is the semantically correct service name
            migrationBuilder.Sql(@"
                UPDATE routing_policies
                SET service = 'CensusReconciliation',
                    updated_at = NOW(),
                    updated_by = 'migration-UpdateDailyImportSuccessServiceName'
                WHERE topic = 'DailyImportSuccess'
                  AND service = 'CensusAutomation';
            ");

            // Also update DailyImportFailure policies for consistency
            // Both events come from the same ReconciliationCompletedEventHandler
            migrationBuilder.Sql(@"
                UPDATE routing_policies
                SET service = 'CensusReconciliation',
                    updated_at = NOW(),
                    updated_by = 'migration-UpdateDailyImportSuccessServiceName'
                WHERE topic = 'DailyImportFailure'
                  AND service = 'CensusAutomation';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert back to CensusAutomation if needed
            migrationBuilder.Sql(@"
                UPDATE routing_policies
                SET service = 'CensusAutomation',
                    updated_at = NOW(),
                    updated_by = 'migration-UpdateDailyImportSuccessServiceName-rollback'
                WHERE topic = 'DailyImportSuccess'
                  AND service = 'CensusReconciliation';
            ");

            migrationBuilder.Sql(@"
                UPDATE routing_policies
                SET service = 'CensusAutomation',
                    updated_at = NOW(),
                    updated_by = 'migration-UpdateDailyImportSuccessServiceName-rollback'
                WHERE topic = 'DailyImportFailure'
                  AND service = 'CensusReconciliation';
            ");
        }
    }
}
