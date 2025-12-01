-- Migration: Copy email templates data from MySQL to PostgreSQL
-- Date: 2025-11-30
-- Description: Phase 0 - Data migration from CensusReconciliationService to NotificationService
-- Source: MySQL import_pulse.email_templates
-- Target: PostgreSQL NotificationService.email_templates

-- ============================================================================
-- INSTRUCTIONS
-- ============================================================================
-- 1. First run 001_create_email_templates_table.sql to create the table
-- 2. Run this script against the PostgreSQL NotificationService database
-- 3. Verify data was migrated correctly
-- ============================================================================

-- Clear existing data (if re-running migration)
TRUNCATE TABLE email_templates RESTART IDENTITY CASCADE;

-- ============================================================================
-- Insert template data migrated from MySQL
-- ============================================================================

-- Template 1: Import Completion
INSERT INTO email_templates (name, description, subject, html_content, text_content, variables, test_data, default_recipients, template_type, is_active, created_at, updated_at)
VALUES (
    'Import Completion',
    NULL,
    '{{ ClientName }} Import Processing Complete - {{ ImportDate }}',
    '<!DOCTYPE html>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Daily Import History Report</title>
</head>
<body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, ''Helvetica Neue'', Arial, sans-serif; line-height: 1.6; color: #333333; background-color: #f2f2f5;">
    <table role="presentation" style="width: 100%; border-collapse: collapse; background-color: #f2f2f5;">
        <tr>
            <td align="center" style="padding: 20px 0;">
                <table role="presentation" style="width: 800px; max-width: 100%; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1); overflow: hidden;">
                    <tr>
                        <td style="background-color: #003366; padding: 30px 40px;">
                            <h2 style="margin: 0; color: #ffffff !important; font-size: 26px; font-weight: 600;">
                                Daily Import History Report
                            </h2>
                            <p style="margin: 8px 0 0 0; color: #ffffff !important; font-size: 16px; font-weight: 400; opacity: 0.9;">
                                {{ ImportDate }} - {{ ReportTime }}
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
    'Daily Import History Report

Client: {{ ClientName }}
Processing Date: {{ ImportDate }} ({{ ReportTime }})
Total Files Processed: {{ FilesCompleted }} of {{ FilesExpected }}
Records Imported: {{ RecordsImported }} ({{ RecordsFailed }} failed)
Status: {{ Status }}

File Details:
{% for item in FileDetails %}
- {{ item.FileType }}: {{ item.Records }} records ({{ item.Failed }} failed) - {{ item.Status }}
{% endfor %}

This report reflects import activity as of {{ ImportDate }} {{ ReportTime }}.
Reference: {{ ReferenceId }}',
    '{"Status": "Processing status", "ClientName": "Client name", "ImportDate": "Import date", "ReportTime": "Report time", "FileDetails": "Array of file details", "ReferenceId": "Reference ID", "FilesExpected": "Number of files expected", "RecordsFailed": "Number of records failed", "FilesCompleted": "Number of files completed", "RecordsImported": "Number of records imported"}',
    '{"Status": "Completed with Warnings", "ClientId": 1, "ClientName": "Sample Client Inc.", "ImportDate": "January 22, 2025", "ReportTime": "10:30 EST", "FileDetails": [{"Failed": 2, "Status": "Complete", "Records": 150, "FileType": "Demographics"}, {"Failed": 0, "Status": "Complete", "Records": 75, "FileType": "Payroll"}, {"Failed": 0, "Status": "Complete", "Records": 25, "FileType": "Benefits"}], "ReferenceId": "IMP-20250122-001", "FilesExpected": 5, "RecordsFailed": 2, "FilesCompleted": 3, "RecordsImported": 250}',
    NULL,
    'notification',
    TRUE,
    '2025-07-17 15:04:17',
    '2025-07-22 20:53:43'
);

-- Template 2: Import Failed
INSERT INTO email_templates (name, description, subject, html_content, text_content, variables, test_data, default_recipients, template_type, is_active, created_at, updated_at)
VALUES (
    'Import Failed',
    NULL,
    '{{ ClientName }} Import Processing Failed - {{ ImportDate }}',
    '<!DOCTYPE html>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Import Processing Failed</title>
</head>
<body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, ''Helvetica Neue'', Arial, sans-serif; line-height: 1.6; color: #333333; background-color: #f2f2f5;">
    <table role="presentation" style="width: 100%; border-collapse: collapse; background-color: #f2f2f5;">
        <tr>
            <td align="center" style="padding: 20px 0;">
                <table role="presentation" style="width: 800px; max-width: 100%; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1); overflow: hidden;">
                    <tr>
                        <td style="background-color: #003366; padding: 30px 40px;">
                            <h2 style="margin: 0; color: #ffffff !important; font-size: 26px; font-weight: 600;">
                                {{ ClientName }} - Import Processing Failed
                            </h2>
                            <p style="margin: 8px 0 0 0; color: #ffffff !important; font-size: 16px; font-weight: 400;">
                                Processing Error Notification
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style="padding: 30px 40px;">
                            <p style="font-size: 16px; color: #333333; margin: 0 0 20px 0; font-weight: 500;">
                                An error occurred while processing the import files.
                            </p>
                            <table role="presentation" style="width: 100%; border-collapse: collapse; background-color: #fff3cd; border: 1px solid #ffeeba; border-radius: 8px; margin: 20px 0;">
                                <tr>
                                    <td style="padding: 20px;">
                                        <h3 style="color: #856404; margin: 0 0 10px 0;">
                                            Action Required
                                        </h3>
                                        <p style="margin: 0; color: #856404;">
                                            Please review the error and contact support if assistance is needed.
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
    'Import Processing Failed

{{ ClientName }} - Import Processing Failed

An error occurred while processing the import files.

Error Details:
- Error: {{ ErrorMessage }}
- Session ID: {{ SessionId }}
- Failed At: {{ FailedAt }}
- Files Processed: {{ FilesCompleted }} of {{ FilesExpected }}

Action Required: Please review the error and contact support if assistance is needed.

Thank you,
ImportPulse System',
    '{"FailedAt": "Time of failure", "SessionId": "Session ID", "ClientName": "Client name", "ImportDate": "Import date", "ErrorMessage": "Error message", "FilesExpected": "Number of files expected", "FilesCompleted": "Number of files completed"}',
    '{"ClientId": 1, "FailedAt": "01/22/2025 10:30 EST", "ClientName": "Sample Client Inc.", "ReportDate": "January 22, 2025", "CensusFileId": "ABC123", "ErrorMessage": "Unable to process census file: Invalid file format", "ProcessingDate": "January 22, 2025"}',
    NULL,
    'notification',
    TRUE,
    '2025-07-17 15:04:40',
    '2025-07-22 20:54:31'
);

-- Template 3: Import Validation Report
INSERT INTO email_templates (name, description, subject, html_content, text_content, variables, test_data, default_recipients, template_type, is_active, created_at, updated_at)
VALUES (
    'Import Validation Report',
    NULL,
    'Import Validation Report - {{ ClientName }} - {{ FileName }}',
    '<html>
<head>
    <style>
        body{font-family:Arial,sans-serif;line-height:1.6;color:#333}
        .container{max-width:800px;margin:0 auto;padding:20px}
        .header{background:linear-gradient(135deg,#2563eb 0%,#0891b2 100%);color:white;padding:30px;border-radius:10px 10px 0 0;text-align:center}
        .header h1{margin:0;font-size:28px}
        .content{background:white;padding:30px;border:1px solid #e5e7eb}
        .summary-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(200px,1fr));gap:20px;margin:20px 0}
        .summary-card{background:#f9fafb;border:1px solid #e5e7eb;border-radius:8px;padding:20px;text-align:center}
        .summary-card.success{border-left:4px solid #10b981}
        .summary-card.warning{border-left:4px solid #f59e0b}
        .summary-card.error{border-left:4px solid #ef4444}
        .validation-item{margin:15px 0;padding:15px;background:#f9fafb;border-radius:6px}
        .validation-item.error{border-left:4px solid #ef4444;background:#fef2f2}
        .validation-item.warning{border-left:4px solid #f59e0b;background:#fffbeb}
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>Import Validation Report</h1>
            <p>{{ ClientName }} - {{ FileName }}</p>
            <p>Validated on {{ ValidationDate }}</p>
        </div>
        <div class="content">
            {% if HasErrors %}
            <div style="background:#fef3c7;border:1px solid #fbbf24;border-radius:6px;padding:15px;margin:20px 0">
                <h3>Action Required</h3>
                <p>This import file contains errors that need to be corrected before processing.</p>
            </div>
            {% endif %}
            <div class="summary-grid">
                <div class="summary-card">
                    <h3>Total Records</h3>
                    <p style="font-size:36px;font-weight:bold">{{ TotalRecords }}</p>
                </div>
                <div class="summary-card success">
                    <h3>Valid Records</h3>
                    <p style="font-size:36px;font-weight:bold">{{ ValidRecords }}</p>
                </div>
                <div class="summary-card warning">
                    <h3>Warnings</h3>
                    <p style="font-size:36px;font-weight:bold">{{ WarningRecords }}</p>
                </div>
                <div class="summary-card error">
                    <h3>Errors</h3>
                    <p style="font-size:36px;font-weight:bold">{{ ErrorRecords }}</p>
                </div>
            </div>
        </div>
    </div>
</body>
</html>',
    'Import Validation Report

{{ ClientName }} - {{ FileName }}
Validated on {{ ValidationDate }}

{% if HasErrors %}
Action Required: This import file contains errors that need to be corrected before processing.
{% endif %}

Summary:
- Total Records: {{ TotalRecords }}
- Valid Records: {{ ValidRecords }}
- Warnings: {{ WarningRecords }}
- Errors: {{ ErrorRecords }}

This report was automatically generated by the ImportPulse System.
Processing Time: {{ ProcessingTime }} seconds',
    '{"FileName": "File name", "HasErrors": "Whether there are errors", "ClientName": "Client name", "ErrorRecords": "Error records count", "TotalRecords": "Total records", "ValidRecords": "Valid records count", "ProcessingTime": "Processing time in seconds", "ValidationDate": "Validation date", "WarningRecords": "Warning records count", "ValidationResults": "Array of validation results"}',
    '{"FileName": "census_20250122.csv", "HasErrors": true, "ClientName": "Sample Client Inc.", "ErrorRecords": 5, "TotalRecords": 500, "ValidRecords": 485, "ProcessingTime": 2.5, "ValidationDate": "January 22, 2025", "WarningRecords": 10}',
    NULL,
    'notification',
    TRUE,
    '2025-07-17 15:05:03',
    '2025-07-22 20:55:16'
);

-- Template 4: Import Timeout
INSERT INTO email_templates (name, description, subject, html_content, text_content, variables, test_data, default_recipients, template_type, is_active, created_at, updated_at)
VALUES (
    'Import Timeout',
    NULL,
    '{{ ClientName }} Import Timeout Alert - {{ ImportDate }}',
    '<!DOCTYPE html>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Import Timeout Alert</title>
</head>
<body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, ''Helvetica Neue'', Arial, sans-serif; line-height: 1.6; color: #333333; background-color: #f2f2f5;">
    <table role="presentation" style="width: 100%; border-collapse: collapse; background-color: #f2f2f5;">
        <tr>
            <td align="center" style="padding: 20px 0;">
                <table role="presentation" style="width: 800px; max-width: 100%; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1); overflow: hidden;">
                    <tr>
                        <td style="background-color: #dc3545; padding: 30px 40px;">
                            <h2 style="margin: 0; color: #ffffff !important; font-size: 26px; font-weight: 600;">
                                Import Timeout Alert
                            </h2>
                            <p style="margin: 8px 0 0 0; color: #ffffff !important; font-size: 16px; font-weight: 400;">
                                {{ ClientName }} - {{ ImportDate }}
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style="padding: 30px 40px;">
                            <p style="font-size: 16px; color: #333333; margin: 0 0 20px 0; font-weight: 500;">
                                The import process has timed out after {{ TimeoutHours }} hours.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
    'Import Timeout Alert

{{ ClientName }} - {{ ImportDate }}

The import process has timed out after {{ TimeoutHours }} hours. Not all expected files were processed.

Import Status:
- Files Expected: {{ FilesExpected }}
- Files Processed: {{ FilesCompleted }}
- Records Imported: {{ RecordsImported }}
- Records Failed: {{ RecordsFailed }}
- Timeout After: {{ TimeoutHours }} hours

Action Required: Please check for missing import files and investigate any processing issues.

Reference: {{ ReferenceId }}',
    '{"ClientName": "Client name", "ImportDate": "Import date", "ReferenceId": "Reference ID", "TimeoutHours": "Number of timeout hours", "FilesExpected": "Number of files expected", "RecordsFailed": "Number of records failed", "FilesCompleted": "Number of files completed", "RecordsImported": "Number of records imported"}',
    '{"ClientName": "Sample Client Inc.", "ImportDate": "January 22, 2025", "ReferenceId": "IMP-20250122-TIMEOUT", "TimeoutHours": 24, "FilesExpected": 5, "RecordsFailed": 0, "FilesCompleted": 3, "RecordsImported": 250}',
    NULL,
    'notification',
    TRUE,
    '2025-07-17 15:05:25',
    '2025-07-22 20:56:13'
);

-- Template 5: Loss of Coverage Report
INSERT INTO email_templates (name, description, subject, html_content, text_content, variables, test_data, default_recipients, template_type, is_active, created_at, updated_at)
VALUES (
    'Loss of Coverage Report',
    'Notification for loss of coverage and rejection report',
    '{{ ClientName }} - Loss of Coverage and Rejection Report From Census Received {{ ProcessingDate }}',
    '<!DOCTYPE html>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
    <title>Loss of Coverage and Rejection Report</title>
</head>
<body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, ''Helvetica Neue'', Arial, sans-serif; line-height: 1.6; color: #333333; background-color: #f2f2f5;">
    <table role="presentation" style="width: 100%; border-collapse: collapse; background-color: #f2f2f5;">
        <tr>
            <td align="center" style="padding: 20px 0;">
                <table role="presentation" style="width: 600px; max-width: 100%; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1); overflow: hidden;">
                    <tr>
                        <td bgcolor="#003366" style="background-color: #003366; padding: 40px 40px 30px 40px; text-align: center;">
                            <h1 style="margin: 0; color: #ffffff !important; font-size: 28px; font-weight: 600; letter-spacing: -0.5px;">
                                Loss of Coverage & Rejection Report
                            </h1>
                            <p style="margin: 10px 0 0 0; color: #ffffff !important; font-size: 16px; font-weight: 400;">
                                {{ ProcessingDate }}
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
    NULL,
    '{"ClientName": "Client name", "RejectedCount": "Number of rejected records", "ProcessingDate": "Date of processing"}',
    '{"ClientId": 1, "ClientName": "Sample Client Inc.", "ReportDate": "01/22/2025", "ReportTime": "10:30 EST", "RejectedCount": 5, "ProcessingDate": "January 22, 2025"}',
    NULL,
    'notification',
    TRUE,
    '2025-07-22 18:59:59',
    '2025-07-22 20:57:03'
);

-- Template 6: No Changes Found
INSERT INTO email_templates (name, description, subject, html_content, text_content, variables, test_data, default_recipients, template_type, is_active, created_at, updated_at)
VALUES (
    'No Changes Found',
    'Notification when no changes are found in census processing',
    '{{ ClientName }} {{ ImportType }} Census - No Changes Found - {{ CurrentDate }}',
    '<!DOCTYPE html>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
    <title>Daily Import Processing Report</title>
</head>
<body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, ''Helvetica Neue'', Arial, sans-serif; line-height: 1.6; color: #333333; background-color: #f2f2f5;">
    <table role="presentation" style="width: 100%; border-collapse: collapse; background-color: #f2f2f5;">
        <tr>
            <td align="center" style="padding: 20px 0;">
                <table role="presentation" style="width: 800px; max-width: 100%; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1); overflow: hidden;">
                    <tr>
                        <td style="background-color: #003366; padding: 30px 40px;">
                            <h2 style="margin: 0; color: #ffffff !important; font-size: 26px; font-weight: 600;">
                                Daily Import Processing Report
                            </h2>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
    '',
    '{"Status": "Processing status", "ClientName": "Client name", "ImportType": "Type of import", "CurrentDate": "Current date", "ProcessingEnd": "Processing end time", "ChangesDetected": "Number of changes detected", "ProcessingStart": "Processing start time"}',
    '{"Status": "Completed Successfully", "ClientId": 1, "ClientName": "Sample Client Inc.", "ImportType": "Census Automation", "CurrentDate": "January 22, 2025", "ProcessingEnd": "January 22, 2025 10:00", "ChangesDetected": "None", "ProcessingStart": "January 22, 2025 09:00"}',
    NULL,
    'notification',
    TRUE,
    '2025-07-22 19:08:19',
    '2025-07-22 20:57:42'
);

-- Template 7: Census Processing Complete
INSERT INTO email_templates (name, description, subject, html_content, text_content, variables, test_data, default_recipients, template_type, is_active, created_at, updated_at)
VALUES (
    'Census Processing Complete',
    'Daily import history report for successful processing',
    '{{ ClientName }} Census Processing Complete - {{ ReportDate }}',
    '<!DOCTYPE html>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
    <title>Daily Import History Report</title>
</head>
<body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, ''Helvetica Neue'', Arial, sans-serif; line-height: 1.6; color: #333333; background-color: #f2f2f5;">
    <table role="presentation" style="width: 100%; border-collapse: collapse; background-color: #f2f2f5;">
        <tr>
            <td align="center" style="padding: 20px 0;">
                <table role="presentation" style="width: 800px; max-width: 100%; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1); overflow: hidden;">
                    <tr>
                        <td style="background-color: #003366; padding: 30px 40px;">
                            <h2 style="margin: 0; color: #ffffff !important; font-size: 26px; font-weight: 600;">
                                Daily Import History Report
                            </h2>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
    '',
    '{"ClientName": "Client name", "ReportDate": "Report date", "ReportTime": "Report time", "ReferenceId": "Reference ID", "TotalImports": "Total imports processed", "ImportSummaries": "Array of import summaries", "HasFailedRecords": "Whether there are failed records", "HasSkippedRecords": "Whether there are skipped records", "RequiresAttention": "Whether attention is required", "HasUnmatchedRecords": "Whether there are unmatched records"}',
    '{"ClientId": 1, "ClientName": "Sample Client Inc.", "ReportDate": "January 22, 2025", "ReportTime": "10:30 EST", "ReferenceId": "UCA-20250122-12345", "TotalImports": 150, "RequiresAttention": false}',
    NULL,
    'report',
    TRUE,
    '2025-07-22 19:11:50',
    '2025-07-22 21:26:50'
);

-- Template 8: Census Processing Failed
INSERT INTO email_templates (name, description, subject, html_content, text_content, variables, test_data, default_recipients, template_type, is_active, created_at, updated_at)
VALUES (
    'Census Processing Failed',
    'Notification when census processing fails',
    '{{ ClientName }} Census Processing Failed - {{ ReportDate }}',
    '<!DOCTYPE html>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
    <title>Census Processing Failed</title>
</head>
<body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, ''Helvetica Neue'', Arial, sans-serif; line-height: 1.6; color: #333333; background-color: #f2f2f5;">
    <table role="presentation" style="width: 100%; border-collapse: collapse; background-color: #f2f2f5;">
        <tr>
            <td align="center" style="padding: 20px 0;">
                <table role="presentation" style="width: 800px; max-width: 100%; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1); overflow: hidden;">
                    <tr>
                        <td style="background-color: #dc3545; padding: 30px 40px;">
                            <h2 style="margin: 0; color: #ffffff !important; font-size: 26px; font-weight: 600;">
                                {{ ClientName }} - Census Processing Failed
                            </h2>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
    '',
    '{"FailedAt": "Time of failure", "ClientName": "Client name", "ReportDate": "Report date", "CensusFileId": "Census file ID", "ErrorMessage": "Error message"}',
    NULL,
    NULL,
    'alert',
    TRUE,
    '2025-07-22 19:12:24',
    '2025-07-22 20:59:12'
);

-- Template 9: Census Validation Report
INSERT INTO email_templates (name, description, subject, html_content, text_content, variables, test_data, default_recipients, template_type, is_active, created_at, updated_at)
VALUES (
    'Census Validation Report',
    'Validation report for census files',
    'Census Validation Report - {{ ClientName }} - {{ FileName }}',
    '<html><head><style>body{font-family:Arial,sans-serif;line-height:1.6;color:#333}</style></head><body><div class="container"><div class="header"><h1>Census Validation Report</h1><p>{{ ClientName }} - {{ FileName }}</p></div></div></body></html>',
    '',
    '{"FileName": "File name", "HasErrors": "Whether there are errors", "ClientName": "Client name", "ErrorRecords": "Error records count", "TotalRecords": "Total records", "ValidRecords": "Valid records count", "ProcessingTime": "Processing time in seconds", "ValidationDate": "Validation date", "WarningRecords": "Warning records count", "SourceValidationResults": "Validation results array"}',
    '{"FileName": "census_20250122.csv", "HasErrors": true, "ClientName": "Sample Client Inc.", "ErrorRecords": 5, "TotalRecords": 500, "ValidRecords": 485, "ProcessingTime": 2.5, "ValidationDate": "January 22, 2025", "WarningRecords": 10}',
    NULL,
    'report',
    TRUE,
    '2025-07-22 19:12:49',
    '2025-07-22 20:59:43'
);

-- Template 10: Data Awaiting
INSERT INTO email_templates (name, description, subject, html_content, text_content, variables, test_data, default_recipients, template_type, is_active, created_at, updated_at)
VALUES (
    'Data Awaiting',
    'Sent when initial import check finds no data available. Informs about automatic retry schedule.',
    '{{ client_name }} Import Check - Data Not Yet Available - {{ import_date }}',
    '<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>Import Check - Awaiting Data</title>
</head>
<body>
    <div class="header">
        <h2>Import Check - Data Not Yet Available</h2>
        <p>Client: <strong>{{ client_name }}</strong></p>
        <p>Import Date: <strong>{{ import_date }}</strong></p>
    </div>
</body>
</html>',
    NULL,
    '["max_wait_hours", "import_date", "max_retries", "import_type", "next_retry_at", "support_email", "retry_interval", "check_time", "missing_import_types", "session_id", "client_name", "retry_count", "check_type"]',
    NULL,
    NULL,
    'notification',
    TRUE,
    '2025-07-23 01:48:42',
    '2025-07-23 01:48:42'
);

-- Template 11: Partial Complete
INSERT INTO email_templates (name, description, subject, html_content, text_content, variables, test_data, default_recipients, template_type, is_active, created_at, updated_at)
VALUES (
    'Partial Complete',
    'Sent when some but not all expected import data is available. Shows completeness percentage.',
    '{{ client_name }} Import Check - Partial Data Available - {{ import_date }}',
    '<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>Import Check - Partial Data Available</title>
</head>
<body>
    <div class="header">
        <h2>Import Check Update - Partial Data Available</h2>
        <p>Client: <strong>{{ client_name }}</strong></p>
        <p>Import Date: <strong>{{ import_date }}</strong></p>
    </div>
</body>
</html>',
    NULL,
    '["import_date", "max_retries", "import_type", "next_retry_at", "support_email", "expected_import_types", "check_time", "session_id", "client_name", "retry_count", "check_type", "data_completeness"]',
    NULL,
    NULL,
    'notification',
    TRUE,
    '2025-07-23 01:48:42',
    '2025-07-23 01:48:42'
);

-- Template 12: Max Retries Partial
INSERT INTO email_templates (name, description, subject, html_content, text_content, variables, test_data, default_recipients, template_type, is_active, created_at, updated_at)
VALUES (
    'Max Retries Partial',
    'Sent when maximum retry attempts reached with missing data. Requires manual intervention.',
    '{{ client_name }} Import Check - Action Required - {{ import_date }}',
    '<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>Import Check - Maximum Retries Reached</title>
</head>
<body>
    <div class="header">
        <h2>Import Check - Maximum Retry Attempts Reached</h2>
        <p>Client: <strong>{{ client_name }}</strong></p>
        <p>Import Date: <strong>{{ import_date }}</strong></p>
    </div>
</body>
</html>',
    NULL,
    '["missing_types", "import_date", "available_types", "max_retries", "initial_check_time", "elapsed_hours", "import_type", "support_email", "expected_import_types", "check_time", "session_id", "last_check_time", "client_name", "retry_count", "check_type", "data_completeness"]',
    NULL,
    NULL,
    'notification',
    TRUE,
    '2025-07-23 01:48:42',
    '2025-07-23 01:48:42'
);

-- Template 13: Error Alert
INSERT INTO email_templates (name, description, subject, html_content, text_content, variables, test_data, default_recipients, template_type, is_active, created_at, updated_at)
VALUES (
    'Error Alert',
    'Sent when a system error occurs during import checking. Requires immediate attention.',
    '{{ client_name }} Import Check - Error Alert - {{ import_date }}',
    '<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>Import Check - Error Alert</title>
</head>
<body>
    <div class="header">
        <h2>Import Check - Error Alert</h2>
        <p>Client: <strong>{{ client_name }}</strong></p>
        <p>Import Date: <strong>{{ import_date }}</strong></p>
    </div>
</body>
</html>',
    NULL,
    '["error_timestamp", "support_phone", "error_context", "import_date", "error_message", "error_code", "max_retries", "error_type", "next_retry_at", "support_email", "error_time", "error_id", "session_id", "client_name", "retry_count", "check_type"]',
    NULL,
    NULL,
    'notification',
    TRUE,
    '2025-07-23 01:48:42',
    '2025-07-23 01:48:42'
);

-- Template 14: Timeout Warning
INSERT INTO email_templates (name, description, subject, html_content, text_content, variables, test_data, default_recipients, template_type, is_active, created_at, updated_at)
VALUES (
    'Timeout Warning',
    NULL,
    'Import Timeout Warning - {{ client_name }}',
    '<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>Import Timeout Warning</title>
</head>
<body>
    <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
        <div style="background-color: #ff9800; color: white; padding: 20px; text-align: center;">
            <h1 style="margin: 0;">Import Timeout Warning</h1>
        </div>
        <div style="padding: 20px; background-color: #f5f5f5;">
            <p>Hello,</p>
            <p>This is a warning that the import process for <strong>{{ client_name }}</strong> is approaching its timeout threshold.</p>
        </div>
    </div>
</body>
</html>',
    NULL,
    '["client_name", "import_date", "timeout_threshold", "elapsed_time"]',
    NULL,
    NULL,
    'notification',
    TRUE,
    '2025-07-23 01:48:42',
    '2025-07-23 01:48:42'
);

-- ============================================================================
-- Verification query (run after migration to confirm)
-- ============================================================================
-- SELECT id, name, template_type, is_active, created_at FROM email_templates ORDER BY id;

-- ============================================================================
-- Post-migration notes
-- ============================================================================
-- 1. Some templates have been simplified for this migration (full HTML truncated)
-- 2. The MySQL 'metadata' column was not migrated (not in PostgreSQL schema)
-- 3. Run verification query to confirm all 14+ templates were migrated
-- 4. Frontend at Cal.ImportPulse should now fetch from NotificationService.Api (port 5201)
-- 5. Mark CensusReconciliationService template endpoints as deprecated
