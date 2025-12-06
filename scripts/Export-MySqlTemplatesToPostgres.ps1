<#
.SYNOPSIS
    Export email templates from MySQL and generate PostgreSQL INSERT statements

.DESCRIPTION
    This script connects to MySQL, reads all email templates, converts Jinja2 syntax
    to Scriban/Liquid, and generates a PostgreSQL migration file.

.EXAMPLE
    .\Export-MySqlTemplatesToPostgres.ps1 -OutputFile "..\migrations\005_full_template_migration.sql"
#>

param(
    [string]$MySqlServer = "192.168.150.52",
    [string]$MySqlDatabase = "import_pulse",
    [string]$MySqlUser = "sdoherty",
    [string]$MySqlPassword = "{u]@9ja-G,nG+DYa-Hn0",
    [string]$OutputFile = "..\migrations\005_full_template_migration.sql"
)

# Jinja2 to Scriban/Liquid syntax converter
function Convert-Jinja2ToLiquid {
    param([string]$Content)

    if ([string]::IsNullOrEmpty($Content)) { return $Content }

    $converted = $Content

    # elif -> elsif
    $converted = $converted -replace '\{%\s*elif\s+', '{% elsif '

    # else if -> elsif
    $converted = $converted -replace '\{%\s*else\s+if\s+', '{% elsif '

    # set -> assign
    $converted = $converted -replace '\{%\s*set\s+', '{% assign '

    # loop.* -> forloop.*
    $converted = $converted -replace 'loop\.index0', 'forloop.index0'
    $converted = $converted -replace 'loop\.index', 'forloop.index'
    $converted = $converted -replace 'loop\.first', 'forloop.first'
    $converted = $converted -replace 'loop\.last', 'forloop.last'
    $converted = $converted -replace 'loop\.length', 'forloop.length'

    # Add spaces around pipe in filters: {{ var|filter }} -> {{ var | filter }}
    $converted = $converted -replace '\{\{\s*(\w+)\|(\w+)', '{{ $1 | $2'

    return $converted
}

# Load MySQL .NET connector
try {
    Add-Type -Path "C:\Program Files (x86)\MySQL\MySQL Connector NET 8.0\Assemblies\v4.8\MySql.Data.dll" -ErrorAction SilentlyContinue
} catch {
    Write-Host "MySQL .NET Connector not found. Trying NuGet package..." -ForegroundColor Yellow
}

# Build connection string
$connectionString = "Server=$MySqlServer;Database=$MySqlDatabase;Uid=$MySqlUser;Pwd=$MySqlPassword;SslMode=none"

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " MySQL to PostgreSQL Email Template Migration" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

try {
    $connection = New-Object MySql.Data.MySqlClient.MySqlConnection($connectionString)
    $connection.Open()
    Write-Host "Connected to MySQL successfully" -ForegroundColor Green

    $query = @"
SELECT id, name, description, subject, html_content, text_content,
       variables, test_data, default_recipients, template_type,
       is_active, created_at, updated_at
FROM email_templates
ORDER BY id
"@

    $command = New-Object MySql.Data.MySqlClient.MySqlCommand($query, $connection)
    $reader = $command.ExecuteReader()

    $templates = @()
    while ($reader.Read()) {
        $templates += [PSCustomObject]@{
            id = $reader["id"]
            name = $reader["name"]
            description = if ($reader["description"] -eq [DBNull]::Value) { $null } else { $reader["description"] }
            subject = $reader["subject"]
            html_content = $reader["html_content"]
            text_content = if ($reader["text_content"] -eq [DBNull]::Value) { $null } else { $reader["text_content"] }
            variables = if ($reader["variables"] -eq [DBNull]::Value) { $null } else { $reader["variables"] }
            test_data = if ($reader["test_data"] -eq [DBNull]::Value) { $null } else { $reader["test_data"] }
            default_recipients = if ($reader["default_recipients"] -eq [DBNull]::Value) { $null } else { $reader["default_recipients"] }
            template_type = $reader["template_type"]
            is_active = $reader["is_active"]
            created_at = $reader["created_at"]
            updated_at = $reader["updated_at"]
        }
    }
    $reader.Close()
    $connection.Close()

    Write-Host "Retrieved $($templates.Count) templates" -ForegroundColor Green

    # Generate PostgreSQL migration
    $sql = @"
-- ============================================================================
-- Full Email Template Migration: MySQL -> PostgreSQL
-- Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
-- ============================================================================
-- Source: MySQL import_pulse.email_templates
-- Target: PostgreSQL NotificationService.email_templates
--
-- Syntax conversions applied:
--   elif -> elsif
--   else if -> elsif
--   set -> assign
--   loop.* -> forloop.*
-- ============================================================================

-- Clear existing templates (optional - uncomment if needed)
-- TRUNCATE TABLE email_templates RESTART IDENTITY CASCADE;

"@

    foreach ($template in $templates) {
        # Convert syntax
        $htmlContent = Convert-Jinja2ToLiquid -Content $template.html_content
        $textContent = Convert-Jinja2ToLiquid -Content $template.text_content
        $subject = Convert-Jinja2ToLiquid -Content $template.subject

        # Escape for PostgreSQL (use dollar quoting)
        $htmlEscaped = if ($htmlContent) { "`$html`$$htmlContent`$html`$" } else { "NULL" }
        $textEscaped = if ($textContent) { "`$text`$$textContent`$text`$" } else { "NULL" }
        $subjectEscaped = "'" + ($subject -replace "'", "''") + "'"
        $nameEscaped = "'" + ($template.name -replace "'", "''") + "'"
        $descEscaped = if ($template.description) { "'" + ($template.description -replace "'", "''") + "'" } else { "NULL" }
        $varsEscaped = if ($template.variables) { "'" + ($template.variables -replace "'", "''") + "'" } else { "NULL" }
        $testDataEscaped = if ($template.test_data) { "'" + ($template.test_data -replace "'", "''") + "'" } else { "NULL" }
        $recipientsEscaped = if ($template.default_recipients) { "'" + ($template.default_recipients -replace "'", "''") + "'" } else { "NULL" }
        $typeEscaped = "'" + $template.template_type + "'"
        $isActive = if ($template.is_active) { "true" } else { "false" }

        $sql += @"

-- Template: $($template.name) (ID: $($template.id))
INSERT INTO email_templates (name, description, subject, html_content, text_content, variables, test_data, default_recipients, template_type, is_active, created_at, updated_at)
VALUES (
    $nameEscaped,
    $descEscaped,
    $subjectEscaped,
    $htmlEscaped,
    $textEscaped,
    $varsEscaped::jsonb,
    $testDataEscaped::jsonb,
    $recipientsEscaped,
    $typeEscaped,
    $isActive,
    NOW(),
    NOW()
)
ON CONFLICT (name) DO UPDATE SET
    description = EXCLUDED.description,
    subject = EXCLUDED.subject,
    html_content = EXCLUDED.html_content,
    text_content = EXCLUDED.text_content,
    variables = EXCLUDED.variables,
    test_data = EXCLUDED.test_data,
    default_recipients = EXCLUDED.default_recipients,
    template_type = EXCLUDED.template_type,
    is_active = EXCLUDED.is_active,
    updated_at = NOW();

"@
    }

    $sql += @"

-- ============================================================================
-- Verification
-- ============================================================================
SELECT name, template_type, is_active, LENGTH(html_content) as html_length
FROM email_templates
ORDER BY name;
"@

    # Write output file
    $outputPath = Join-Path $PSScriptRoot $OutputFile
    $sql | Out-File -FilePath $outputPath -Encoding UTF8

    Write-Host ""
    Write-Host "Migration file created: $outputPath" -ForegroundColor Green
    Write-Host "Templates migrated: $($templates.Count)" -ForegroundColor Green
    Write-Host ""
    Write-Host "To apply, run against your PostgreSQL database:" -ForegroundColor Yellow
    Write-Host "  psql -h <host> -d notification_service -f $OutputFile" -ForegroundColor White

} catch {
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red

    # Alternative: output as JSON for manual conversion
    Write-Host ""
    Write-Host "MySQL connector not available. Use Claude MCP tools instead:" -ForegroundColor Yellow
    Write-Host "  The templates have been exported to: mysql_email_templates_export.json" -ForegroundColor White
}
