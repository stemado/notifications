-- ============================================================================
-- QUICK FIX: Update Census Processing Complete Template with Full Content
-- ============================================================================
-- Run this SQL directly against the PostgreSQL NotificationService database
-- to fix the truncated template content.
--
-- Date: 2025-12-05
-- ============================================================================

UPDATE email_templates 
SET html_content = $template$<!DOCTYPE html>
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
                                {{ ReportDate }} - {{ ReportTime }}
                            </p>
                        </td>
                    </tr>

                    <tr>
                        <td style="padding: 30px 40px;">

                            <table role="presentation" style="width: 100%; border-collapse: collapse; background-color: #f8f9fa; border-radius: 8px; margin-bottom: 30px;">
                                <tr>
                                    <td style="padding: 20px; border-left: 4px solid #003366;">
                                        <table role="presentation" style="width: 100%; border-collapse: collapse;">
                                            <tr>
                                                <td style="padding: 5px 0;">
                                                    <strong style="color: #333333; display: inline-block; min-width: 180px;">Client:</strong>
                                                    <span style="color: #333333;">{{ ClientName }}</span>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 5px 0;">
                                                    <strong style="color: #333333; display: inline-block; min-width: 180px;">Processing Period:</strong>
                                                    <span style="color: #333333;">{{ ReportDate }} ({{ ReportTime }})</span>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 5px 0;">
                                                    <strong style="color: #333333; display: inline-block; min-width: 180px;">Total Imports Processed:</strong>
                                                    <span style="color: #333333;">{{ TotalImports }}</span>
                                                </td>
                                            </tr>
                                            <tr>
                                                <td style="padding: 5px 0;">
                                                    <strong style="color: #333333; display: inline-block; min-width: 180px;">Requires Attention:</strong>
                                                    {% if RequiresAttention %}
                                                        <span style="display: inline-block; background-color: #dc3545; color: #ffffff !important; padding: 2px 8px; border-radius: 4px; font-size: 14px; font-weight: 500;">Yes</span>
                                                    {% else %}
                                                        <span style="display: inline-block; background-color: #28a745; color: #ffffff !important; padding: 2px 8px; border-radius: 4px; font-size: 14px; font-weight: 500;">No</span>
                                                    {% endif %}
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <table role="presentation" style="width: 100%; border-collapse: collapse; margin: 20px 0; box-shadow: 0 1px 3px rgba(0,0,0,0.1); border-radius: 8px; overflow: hidden;">
                                <thead>
                                    <tr>
                                        <th style="background-color: #f8f9fa; border-bottom: 2px solid #dee2e6; padding: 14px; text-align: left; font-weight: 600; color: #495057;">Import Type</th>
                                        <th style="background-color: #f8f9fa; border-bottom: 2px solid #dee2e6; padding: 14px; text-align: left; font-weight: 600; color: #495057;">Total Count</th>
                                        <th style="background-color: #f8f9fa; border-bottom: 2px solid #dee2e6; padding: 14px; text-align: left; font-weight: 600; color: #495057;">Failed</th>
                                        <th style="background-color: #f8f9fa; border-bottom: 2px solid #dee2e6; padding: 14px; text-align: left; font-weight: 600; color: #495057;">Skipped</th>
                                        <th style="background-color: #f8f9fa; border-bottom: 2px solid #dee2e6; padding: 14px; text-align: left; font-weight: 600; color: #495057;">Unmatched</th>
                                        <th style="background-color: #f8f9fa; border-bottom: 2px solid #dee2e6; padding: 14px; text-align: left; font-weight: 600; color: #495057;">Status</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {% for item in ImportSummaries %}
                                    <tr>
                                        <td style="border-bottom: 1px solid #dee2e6; padding: 12px; background-color: #ffffff;">{{ item.ImportType }}</td>
                                        <td style="border-bottom: 1px solid #dee2e6; padding: 12px; background-color: #ffffff;">{{ item.TotalCount }}</td>
                                        <td style="border-bottom: 1px solid #dee2e6; padding: 12px; background-color: #ffffff;">{{ item.FailedCount }}</td>
                                        <td style="border-bottom: 1px solid #dee2e6; padding: 12px; background-color: #ffffff;">{{ item.SkippedCount }}</td>
                                        <td style="border-bottom: 1px solid #dee2e6; padding: 12px; background-color: #ffffff;">{{ item.UnmatchedCount }}</td>
                                        <td style="border-bottom: 1px solid #dee2e6; padding: 12px; background-color: #ffffff;">
                                            {% if item.Status == "Success" %}
                                                <span style="display: inline-block; background-color: #28a745; color: #ffffff !important; padding: 4px 10px; border-radius: 4px; font-size: 14px; font-weight: 500;">{{ item.Status }}</span>
                                            {% elsif item.Status == "Failed" %}
                                                <span style="display: inline-block; background-color: #dc3545; color: #ffffff !important; padding: 4px 10px; border-radius: 4px; font-size: 14px; font-weight: 500;">{{ item.Status }}</span>
                                            {% else %}
                                                <span style="display: inline-block; background-color: #ffc107; color: #333333; padding: 4px 10px; border-radius: 4px; font-size: 14px; font-weight: 500;">{{ item.Status }}</span>
                                            {% endif %}
                                        </td>
                                    </tr>
                                    {% endfor %}
                                </tbody>
                            </table>

                            {% if HasFailedRecords %}
                            <div style="margin: 30px 0;">
                                <h3 style="color: #003366; font-size: 20px; margin: 0 0 15px 0; padding-bottom: 10px; border-bottom: 2px solid #e9ecef; font-weight: 600;">Failed Import Details</h3>
                                <table role="presentation" style="width: 100%; border-collapse: collapse;">
                                    {% for item in ImportSummaries %}
                                        {% if item.FailedRecords %}
                                            {% for failed_record in item.FailedRecords %}
                                            <tr>
                                                <td style="padding: 8px 0; border-bottom: 1px solid #f0f0f0;">
                                                    <strong style="color: #003366;">{{ item.ImportType }}:</strong> {{ failed_record.ErrorMessage }} (Employee ID: {{ failed_record.EmployeeId }})
                                                </td>
                                            </tr>
                                            {% endfor %}
                                        {% endif %}
                                    {% endfor %}
                                </table>
                            </div>
                            {% endif %}

                            {% if HasSkippedRecords %}
                            <div style="margin: 30px 0;">
                                <h3 style="color: #003366; font-size: 20px; margin: 0 0 15px 0; padding-bottom: 10px; border-bottom: 2px solid #e9ecef; font-weight: 600;">Skipped Records</h3>
                                <p style="margin: 0 0 15px 0; color: #333333;">The following records were skipped during processing due to data quality issues:</p>
                                <table role="presentation" style="width: 100%; border-collapse: collapse;">
                                    {% for item in ImportSummaries %}
                                        {% if item.SkippedRecords %}
                                            {% for skipped_record in item.SkippedRecords %}
                                            <tr>
                                                <td style="padding: 8px 0; border-bottom: 1px solid #f0f0f0;">
                                                    <strong style="color: #003366;">{{ item.ImportType }}:</strong> {{ skipped_record.Reason }} (Employee ID: {{ skipped_record.EmployeeId }}, Last Four SSN: {{ skipped_record.LastFourSsn }})
                                                </td>
                                            </tr>
                                            {% endfor %}
                                        {% endif %}
                                    {% endfor %}
                                </table>
                            </div>
                            {% endif %}

                            {% if HasUnmatchedRecords %}
                            <table role="presentation" style="width: 100%; border-collapse: collapse; background-color: #fff3cd; border: 1px solid #ffeeba; border-radius: 8px; margin: 20px 0;">
                                <tr>
                                    <td style="padding: 20px;">
                                        <h3 style="color: #856404; margin: 0 0 10px 0;">
                                            <span style="font-size: 24px; margin-right: 10px; vertical-align: middle;">⚠</span>Unmatched Client Records
                                        </h3>
                                        <p style="margin: 0 0 15px 0; color: #856404;">The following client records were expected but not found in the import:</p>
                                        <table role="presentation" style="width: 100%; border-collapse: collapse;">
                                            {% for item in ImportSummaries %}
                                                {% if item.UnmatchedRecords %}
                                                    {% for unmatched_record in item.UnmatchedRecords %}
                                                    <tr>
                                                        <td style="padding: 8px 0; border-bottom: 1px solid #f0f0f0;">
                                                            <strong style="color: #003366;">{{ item.ImportType }}:</strong> {{ unmatched_record.Name }} (Employee ID: {{ unmatched_record.EmployeeId }}, Last Four SSN: {{ unmatched_record.LastFourSsn }})
                                                        </td>
                                                    </tr>
                                                    {% endfor %}
                                                {% endif %}
                                            {% endfor %}
                                        </table>
                                    </td>
                                </tr>
                            </table>
                            {% endif %}

                        </td>
                    </tr>

                    <tr>
                        <td style="background-color: #f7f7f7; padding: 20px 30px; text-align: left; border-top: 1px solid #e0e0e0;">
                            <p style="margin: 0 0 10px 0; font-size: 14px; color: #666666;">This report reflects import activity as of {{ ReportDate }} {{ ReportTime }}. Issues will be included in subsequent reports until resolved.</p>
                            <p style="margin: 0; font-size: 14px; color: #666666;">
                                Reference: <span style="font-family: monospace; background-color: #f8f9fa; padding: 2px 6px; border-radius: 3px; font-size: 13px;">{{ ReferenceId }}</span>
                            </p>
                        </td>
                    </tr>

                </table>

            </td>
        </tr>
    </table>

</body>
</html>$template$,
    updated_at = NOW()
WHERE name = 'Census Processing Complete';

-- Verify the update
SELECT name, LENGTH(html_content) as html_length, updated_at 
FROM email_templates 
WHERE name = 'Census Processing Complete';
