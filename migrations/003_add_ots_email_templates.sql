-- Migration: Add OTS (OneTimeSpreadsheets) email templates
-- Date: 2026-01-04
-- Description: Add email templates for OTS scheduled run success and failure notifications

-- ============================================================================
-- Template 15: OTS Run Success
-- ============================================================================
INSERT INTO email_templates (name, description, subject, html_content, text_content, variables, test_data, default_recipients, template_type, is_active, created_at, updated_at)
VALUES (
    'OTS Run Success',
    'Notification for successful OTS scheduled run completion',
    '{{ ClientName }} - OTS Processing Complete - {{ RunDate }}',
    '<!DOCTYPE html>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>OTS Processing Complete</title>
</head>
<body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, ''Helvetica Neue'', Arial, sans-serif; line-height: 1.6; color: #333333; background-color: #f2f2f5;">
    <table role="presentation" style="width: 100%; border-collapse: collapse; background-color: #f2f2f5;">
        <tr>
            <td align="center" style="padding: 20px 0;">
                <table role="presentation" style="width: 600px; max-width: 100%; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1); overflow: hidden;">
                    <!-- Header -->
                    <tr>
                        <td style="background-color: #28a745; padding: 30px 40px;">
                            <h2 style="margin: 0; color: #ffffff !important; font-size: 26px; font-weight: 600;">
                                OTS Processing Complete
                            </h2>
                            <p style="margin: 8px 0 0 0; color: #ffffff !important; font-size: 16px; font-weight: 400; opacity: 0.9;">
                                {{ ClientName }}
                            </p>
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style="padding: 30px 40px;">
                            <table role="presentation" style="width: 100%; border-collapse: collapse;">
                                <tr>
                                    <td style="padding: 10px 0; border-bottom: 1px solid #e9ecef;">
                                        <strong style="color: #6c757d;">Run Date</strong>
                                    </td>
                                    <td style="padding: 10px 0; border-bottom: 1px solid #e9ecef; text-align: right;">
                                        {{ RunDate }}
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 10px 0; border-bottom: 1px solid #e9ecef;">
                                        <strong style="color: #6c757d;">Status</strong>
                                    </td>
                                    <td style="padding: 10px 0; border-bottom: 1px solid #e9ecef; text-align: right;">
                                        <span style="color: #28a745; font-weight: 600;">Success</span>
                                    </td>
                                </tr>
                            </table>

                            <!-- Processing Summary -->
                            <h3 style="margin: 25px 0 15px 0; color: #333333; font-size: 18px; border-bottom: 2px solid #e9ecef; padding-bottom: 10px;">
                                Processing Summary
                            </h3>
                            <table role="presentation" style="width: 100%; border-collapse: collapse; background-color: #f8f9fa; border-radius: 8px;">
                                <tr>
                                    <td style="padding: 15px; width: 50%;">
                                        <div style="text-align: center;">
                                            <div style="font-size: 32px; font-weight: 700; color: #333333;">{{ RecordsProcessed }}</div>
                                            <div style="font-size: 12px; color: #6c757d; text-transform: uppercase;">Records Processed</div>
                                        </div>
                                    </td>
                                    <td style="padding: 15px; width: 50%;">
                                        <div style="text-align: center;">
                                            <div style="font-size: 32px; font-weight: 700; color: #28a745;">{{ ActiveCount }}</div>
                                            <div style="font-size: 12px; color: #6c757d; text-transform: uppercase;">Active Records</div>
                                        </div>
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 15px; width: 50%;">
                                        <div style="text-align: center;">
                                            <div style="font-size: 32px; font-weight: 700; color: #ffc107;">{{ WaiveCount }}</div>
                                            <div style="font-size: 12px; color: #6c757d; text-transform: uppercase;">Waive/Drop Records</div>
                                        </div>
                                    </td>
                                    <td style="padding: 15px; width: 50%;">
                                        <div style="text-align: center;">
                                            <div style="font-size: 32px; font-weight: 700; color: #17a2b8;">{{ Duration }}</div>
                                            <div style="font-size: 12px; color: #6c757d; text-transform: uppercase;">Duration</div>
                                        </div>
                                    </td>
                                </tr>
                            </table>

                            <!-- Output Files -->
                            <h3 style="margin: 25px 0 15px 0; color: #333333; font-size: 18px; border-bottom: 2px solid #e9ecef; padding-bottom: 10px;">
                                Output Files
                            </h3>
                            <table role="presentation" style="width: 100%; border-collapse: collapse;">
                                <tr>
                                    <td style="padding: 10px 0;">
                                        <strong style="color: #6c757d;">Output Path:</strong>
                                        <div style="margin-top: 5px; padding: 10px; background-color: #f8f9fa; border-radius: 4px; font-family: monospace; font-size: 12px; word-break: break-all;">
                                            {{ OutputFilePath }}
                                        </div>
                                    </td>
                                </tr>
                                {% if ValidationReportPath %}
                                <tr>
                                    <td style="padding: 10px 0;">
                                        <strong style="color: #6c757d;">Validation Report:</strong>
                                        <div style="margin-top: 5px; padding: 10px; background-color: #f8f9fa; border-radius: 4px; font-family: monospace; font-size: 12px; word-break: break-all;">
                                            {{ ValidationReportPath }}
                                        </div>
                                    </td>
                                </tr>
                                {% endif %}
                            </table>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style="background-color: #e9ecef; padding: 15px 40px; text-align: center;">
                            <p style="margin: 0; font-size: 12px; color: #6c757d;">
                                Reference ID: {{ ReferenceId }}
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
    'OTS Processing Complete

Client: {{ ClientName }}
Run Date: {{ RunDate }}
Status: Success

Processing Summary:
- Records Processed: {{ RecordsProcessed }}
- Active Records: {{ ActiveCount }}
- Waive/Drop Records: {{ WaiveCount }}
- Duration: {{ Duration }}

Output Files:
- Output Path: {{ OutputFilePath }}
{% if ValidationReportPath %}- Validation Report: {{ ValidationReportPath }}{% endif %}

Reference ID: {{ ReferenceId }}',
    '{"ClientName": "Client display name", "RunDate": "Date and time of execution", "RecordsProcessed": "Total records processed", "ActiveCount": "Count of active enrollment records", "WaiveCount": "Count of waive/drop records", "Duration": "Processing duration", "OutputFilePath": "Path to output file(s)", "ValidationReportPath": "Path to validation report (optional)", "ReferenceId": "Unique execution reference ID"}',
    '{"ClientName": "Putnam County Schools", "RunDate": "January 5, 2026 08:15:32 EST", "RecordsProcessed": "1,247", "ActiveCount": "1,189", "WaiveCount": "58", "Duration": "2m 34s", "OutputFilePath": "\\\\anf-fs01\\EDI\\Conversions_PlanSource_Main\\Putnam\\Payroll\\OTS\\PUTNAM_COUNTY_PAYROLL_OE_2026.xlsx", "ValidationReportPath": "", "ReferenceId": "OTS-2026-01-05-PUTNAM-001"}',
    NULL,
    'success',
    TRUE,
    NOW(),
    NOW()
);

-- ============================================================================
-- Template 16: OTS Run Failure
-- ============================================================================
INSERT INTO email_templates (name, description, subject, html_content, text_content, variables, test_data, default_recipients, template_type, is_active, created_at, updated_at)
VALUES (
    'OTS Run Failure',
    'Notification for failed OTS scheduled run',
    'ALERT: {{ ClientName }} - OTS Processing Failed - {{ RunDate }}',
    '<!DOCTYPE html>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>OTS Processing Failed</title>
</head>
<body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, ''Helvetica Neue'', Arial, sans-serif; line-height: 1.6; color: #333333; background-color: #f2f2f5;">
    <table role="presentation" style="width: 100%; border-collapse: collapse; background-color: #f2f2f5;">
        <tr>
            <td align="center" style="padding: 20px 0;">
                <table role="presentation" style="width: 600px; max-width: 100%; border-collapse: collapse; background-color: #ffffff; border-radius: 12px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1); overflow: hidden;">
                    <!-- Header -->
                    <tr>
                        <td style="background-color: #dc3545; padding: 30px 40px;">
                            <h2 style="margin: 0; color: #ffffff !important; font-size: 26px; font-weight: 600;">
                                OTS Processing Failed
                            </h2>
                            <p style="margin: 8px 0 0 0; color: #ffffff !important; font-size: 16px; font-weight: 400; opacity: 0.9;">
                                {{ ClientName }}
                            </p>
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style="padding: 30px 40px;">
                            <table role="presentation" style="width: 100%; border-collapse: collapse;">
                                <tr>
                                    <td style="padding: 10px 0; border-bottom: 1px solid #e9ecef;">
                                        <strong style="color: #6c757d;">Run Date</strong>
                                    </td>
                                    <td style="padding: 10px 0; border-bottom: 1px solid #e9ecef; text-align: right;">
                                        {{ RunDate }}
                                    </td>
                                </tr>
                                <tr>
                                    <td style="padding: 10px 0; border-bottom: 1px solid #e9ecef;">
                                        <strong style="color: #6c757d;">Status</strong>
                                    </td>
                                    <td style="padding: 10px 0; border-bottom: 1px solid #e9ecef; text-align: right;">
                                        <span style="color: #dc3545; font-weight: 600;">Failed</span>
                                    </td>
                                </tr>
                            </table>

                            <!-- Error Details -->
                            <h3 style="margin: 25px 0 15px 0; color: #333333; font-size: 18px; border-bottom: 2px solid #e9ecef; padding-bottom: 10px;">
                                Error Details
                            </h3>
                            <div style="background-color: #f8d7da; border: 1px solid #f5c6cb; border-radius: 8px; padding: 20px; margin: 15px 0;">
                                <p style="margin: 0 0 10px 0; font-weight: 600; color: #721c24;">
                                    {{ ErrorMessage }}
                                </p>
                                {% if ErrorCode %}
                                <p style="margin: 0; color: #721c24; font-size: 14px;">
                                    <strong>Error Code:</strong> {{ ErrorCode }}
                                </p>
                                {% endif %}
                            </div>

                            {% if StackTrace %}
                            <!-- Stack Trace -->
                            <h3 style="margin: 25px 0 15px 0; color: #333333; font-size: 18px; border-bottom: 2px solid #e9ecef; padding-bottom: 10px;">
                                Stack Trace
                            </h3>
                            <pre style="background-color: #f1f1f1; padding: 15px; overflow-x: auto; font-size: 11px; border-radius: 4px; white-space: pre-wrap; word-wrap: break-word;">{{ StackTrace }}</pre>
                            {% endif %}

                            <!-- Recommended Actions -->
                            <h3 style="margin: 25px 0 15px 0; color: #333333; font-size: 18px; border-bottom: 2px solid #e9ecef; padding-bottom: 10px;">
                                Recommended Actions
                            </h3>
                            <ol style="margin: 0; padding-left: 20px; color: #495057;">
                                <li style="margin-bottom: 8px;">Check input file availability at configured paths</li>
                                <li style="margin-bottom: 8px;">Verify network share connectivity</li>
                                <li style="margin-bottom: 8px;">Review application logs for detailed error information</li>
                                <li style="margin-bottom: 8px;">Contact support if issue persists</li>
                            </ol>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style="background-color: #e9ecef; padding: 15px 40px; text-align: center;">
                            <p style="margin: 0; font-size: 12px; color: #6c757d;">
                                Reference ID: {{ ReferenceId }} | Failed At: {{ FailedAt }}
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>',
    'OTS Processing FAILED

Client: {{ ClientName }}
Run Date: {{ RunDate }}
Status: FAILED

Error Details:
{{ ErrorMessage }}
{% if ErrorCode %}Error Code: {{ ErrorCode }}{% endif %}

{% if StackTrace %}
Stack Trace:
{{ StackTrace }}
{% endif %}

Recommended Actions:
1. Check input file availability at configured paths
2. Verify network share connectivity
3. Review application logs for detailed error information
4. Contact support if issue persists

Reference ID: {{ ReferenceId }}
Failed At: {{ FailedAt }}

Please investigate immediately.',
    '{"ClientName": "Client display name", "RunDate": "Date and time of execution attempt", "ErrorMessage": "Detailed error message", "ErrorCode": "Error code (optional)", "StackTrace": "Stack trace for debugging (optional)", "FailedAt": "Timestamp when failure occurred", "ReferenceId": "Unique execution reference ID"}',
    '{"ClientName": "Emanuel County Schools", "RunDate": "January 5, 2026 08:00:15 EST", "ErrorMessage": "Input file not found: Emanuel_County_Board_of_Education_2026_Payroll.csv", "ErrorCode": "FILE_NOT_FOUND", "StackTrace": "", "FailedAt": "January 5, 2026 08:00:16 EST", "ReferenceId": "OTS-2026-01-05-EMANUEL-001"}',
    NULL,
    'error',
    TRUE,
    NOW(),
    NOW()
);

-- ============================================================================
-- Topic-Template Mappings for OTS
-- ============================================================================

-- Map OTSScheduledRunSuccess topic to OTS Run Success template
INSERT INTO topic_template_mappings
(id, service, topic, client_id, template_id, is_enabled, priority, created_at, updated_at)
VALUES (
    gen_random_uuid(),
    'OneTimeSpreadsheetService',
    'OTSScheduledRunSuccess',
    NULL,
    (SELECT id FROM email_templates WHERE name = 'OTS Run Success'),
    true,
    10,
    NOW(),
    NOW()
);

-- Map OTSScheduledRunFailure topic to OTS Run Failure template
INSERT INTO topic_template_mappings
(id, service, topic, client_id, template_id, is_enabled, priority, created_at, updated_at)
VALUES (
    gen_random_uuid(),
    'OneTimeSpreadsheetService',
    'OTSScheduledRunFailure',
    NULL,
    (SELECT id FROM email_templates WHERE name = 'OTS Run Failure'),
    true,
    10,
    NOW(),
    NOW()
);

-- ============================================================================
-- Verification queries
-- ============================================================================
-- SELECT id, name, template_type, is_active FROM email_templates WHERE name LIKE 'OTS%' ORDER BY id;
-- SELECT * FROM topic_template_mappings WHERE service = 'OneTimeSpreadsheetService';
