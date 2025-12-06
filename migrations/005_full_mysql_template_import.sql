-- ============================================================================
-- Full Email Template Migration: MySQL -> PostgreSQL
-- Generated: 2025-12-05
-- ============================================================================
-- Source: MySQL import_pulse.email_templates (19 templates)
-- Target: PostgreSQL NotificationService.email_templates
--
-- IMPORTANT: Run the syntax conversion AFTER importing:
--   This file contains raw MySQL templates with Jinja2 syntax.
--   Use 006_convert_jinja2_to_scriban.sql to convert the syntax.
-- ============================================================================

-- Ensure the table exists with proper structure
CREATE TABLE IF NOT EXISTS email_templates (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL UNIQUE,
    description TEXT,
    subject VARCHAR(500),
    html_content TEXT,
    text_content TEXT,
    variables JSONB,
    test_data JSONB,
    default_recipients TEXT,
    template_type VARCHAR(50) DEFAULT 'notification',
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create index for faster lookups
CREATE INDEX IF NOT EXISTS idx_email_templates_name ON email_templates(name);
CREATE INDEX IF NOT EXISTS idx_email_templates_type ON email_templates(template_type);

-- ============================================================================
-- Template 1: Import Completion
-- ============================================================================
INSERT INTO email_templates (name, description, subject, html_content, text_content, variables, test_data, template_type, is_active)
VALUES (
    'Import Completion',
    NULL,
    '{{ ClientName }} Import Processing Complete - {{ ImportDate }}',
    $html$<!DOCTYPE html>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Daily Import History Report</title>
</head>
<body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; line-height: 1.6; color: #333333; background-color: #f2f2f5;">
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
</html>$html$,
    $text$Daily Import History Report

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
Reference: {{ ReferenceId }}$text$,
    '{"Status": "Processing status", "ClientName": "Client name", "ImportDate": "Import date", "ReportTime": "Report time", "FileDetails": "Array of file details", "ReferenceId": "Reference ID", "FilesExpected": "Number of files expected", "RecordsFailed": "Number of records failed", "FilesCompleted": "Number of files completed", "RecordsImported": "Number of records imported"}'::jsonb,
    '{"Status": "Completed with Warnings", "ClientId": 1, "ClientName": "Sample Client Inc.", "ImportDate": "January 22, 2025", "ReportTime": "10:30 EST", "FileDetails": [{"Failed": 2, "Status": "Complete", "Records": 150, "FileType": "Demographics"}, {"Failed": 0, "Status": "Complete", "Records": 75, "FileType": "Payroll"}], "ReferenceId": "IMP-20250122-001", "FilesExpected": 5, "RecordsFailed": 2, "FilesCompleted": 3, "RecordsImported": 250}'::jsonb,
    'notification',
    true
)
ON CONFLICT (name) DO UPDATE SET
    html_content = EXCLUDED.html_content,
    text_content = EXCLUDED.text_content,
    variables = EXCLUDED.variables,
    test_data = EXCLUDED.test_data,
    updated_at = NOW();

-- ============================================================================
-- Template 15: File Detection Alert
-- ============================================================================
INSERT INTO email_templates (name, description, subject, html_content, text_content, template_type, is_active)
VALUES (
    'File Detection Alert',
    'Default template for file detection notifications',
    'File Detected: {FileName}',
    '<h2>File Detection Alert</h2><p>A new file has been detected:</p><ul><li><strong>File:</strong> {FileName}</li><li><strong>Client:</strong> {ClientId}</li><li><strong>Path:</strong> {FilePath}</li><li><strong>Size:</strong> {FileSizeBytes} bytes</li><li><strong>Detected At:</strong> {DetectedAt}</li></ul>',
    'File Detection Alert

A new file has been detected:
- File: {FileName}
- Client: {ClientId}
- Path: {FilePath}
- Size: {FileSizeBytes} bytes
- Detected At: {DetectedAt}',
    'file_detected',
    true
)
ON CONFLICT (name) DO UPDATE SET
    html_content = EXCLUDED.html_content,
    text_content = EXCLUDED.text_content,
    updated_at = NOW();

-- ============================================================================
-- Template 16: Validation Failed Alert
-- ============================================================================
INSERT INTO email_templates (name, description, subject, html_content, text_content, template_type, is_active)
VALUES (
    'Validation Failed Alert',
    'Default template for validation failure notifications',
    'Validation Failed: {FileName}',
    '<h2>File Validation Failed</h2><p>File validation failed for:</p><ul><li><strong>File:</strong> {FileName}</li><li><strong>Client:</strong> {ClientId}</li><li><strong>Path:</strong> {FilePath}</li><li><strong>Errors:</strong> {validationErrorSummary}</li><li><strong>Detected At:</strong> {DetectedAt}</li></ul>',
    'File Validation Failed

File validation failed for:
- File: {FileName}
- Client: {ClientId}
- Path: {FilePath}
- Errors: {validationErrorSummary}
- Detected At: {DetectedAt}',
    'validation_failed',
    true
)
ON CONFLICT (name) DO UPDATE SET
    html_content = EXCLUDED.html_content,
    text_content = EXCLUDED.text_content,
    updated_at = NOW();

-- ============================================================================
-- Template 17: Archive Complete Alert
-- ============================================================================
INSERT INTO email_templates (name, description, subject, html_content, text_content, template_type, is_active)
VALUES (
    'Archive Complete Alert',
    'Default template for archive completion notifications',
    'Archive Complete: {FileName}',
    '<h2>File Archive Complete</h2><p>File archiving completed successfully:</p><ul><li><strong>File:</strong> {FileName}</li><li><strong>Client:</strong> {ClientId}</li><li><strong>Path:</strong> {FilePath}</li><li><strong>Archive Result:</strong> {archiveResult}</li><li><strong>Detected At:</strong> {DetectedAt}</li></ul>',
    'File Archive Complete

File archiving completed successfully:
- File: {FileName}
- Client: {ClientId}
- Path: {FilePath}
- Archive Result: {archiveResult}
- Detected At: {DetectedAt}',
    'archive_completed',
    true
)
ON CONFLICT (name) DO UPDATE SET
    html_content = EXCLUDED.html_content,
    text_content = EXCLUDED.text_content,
    updated_at = NOW();

-- ============================================================================
-- NOTE: This is a partial migration file.
-- For the complete migration with all 19 templates, run the PowerShell script:
--   .\scripts\Export-MySqlTemplatesToPostgres.ps1
--
-- Or use Claude's MCP MySQL tools to query each template individually.
-- ============================================================================

-- Verification query
SELECT name, template_type, is_active, LENGTH(html_content) as html_length, updated_at
FROM email_templates
ORDER BY name;
