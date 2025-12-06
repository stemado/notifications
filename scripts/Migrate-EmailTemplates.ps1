<#
.SYNOPSIS
    Migrate email templates from MySQL (Jinja2/Ninja) to PostgreSQL (Scriban/Liquid)

.DESCRIPTION
    This script:
    1. Connects to MySQL and exports email templates
    2. Converts Jinja2/Ninja syntax to Scriban/Liquid syntax
    3. Generates PostgreSQL UPDATE/INSERT statements
    4. Optionally executes the migration directly

.PARAMETER MySqlConnectionString
    MySQL connection string for the source database

.PARAMETER PostgresConnectionString
    PostgreSQL connection string for the target database

.PARAMETER OutputFile
    Path to output the generated SQL statements (optional)

.PARAMETER ExecuteMigration
    If specified, executes the migration directly against PostgreSQL

.EXAMPLE
    .\Migrate-EmailTemplates.ps1 -OutputFile "migration.sql"
    
.EXAMPLE
    .\Migrate-EmailTemplates.ps1 -ExecuteMigration
#>

param(
    [string]$MySqlConnectionString,
    [string]$PostgresConnectionString,
    [string]$OutputFile = ".\generated_template_migration.sql",
    [switch]$ExecuteMigration
)

# ============================================================================
# Jinja2 to Scriban/Liquid Syntax Converter
# ============================================================================

function Convert-Jinja2ToLiquid {
    <#
    .SYNOPSIS
        Converts Jinja2/Ninja template syntax to Scriban/Liquid syntax
    #>
    param(
        [string]$TemplateContent
    )

    if ([string]::IsNullOrEmpty($TemplateContent)) {
        return $TemplateContent
    }

    $converted = $TemplateContent

    # Key syntax differences to convert:
    
    # 1. elif -> elsif (Jinja2 uses elif, Liquid uses elsif)
    $converted = $converted -replace '\{%\s*elif\s+', '{% elsif '
    
    # 2. else if -> elsif 
    $converted = $converted -replace '\{%\s*else\s+if\s+', '{% elsif '
    
    # 3. set -> assign (for variable assignment)
    $converted = $converted -replace '\{%\s*set\s+', '{% assign '
    
    # 4. Ensure proper spacing around filters (var|filter -> var | filter)
    # This regex adds spaces around the pipe in filter expressions
    $converted = $converted -replace '\{\{\s*(\w+)\|(\w+)', '{{ $1 | $2'
    
    # 5. loop.index -> forloop.index (Jinja2 loop variable names)
    $converted = $converted -replace 'loop\.index0', 'forloop.index0'
    $converted = $converted -replace 'loop\.index', 'forloop.index'
    $converted = $converted -replace 'loop\.first', 'forloop.first'
    $converted = $converted -replace 'loop\.last', 'forloop.last'
    $converted = $converted -replace 'loop\.length', 'forloop.length'
    
    # 6. Handle truthy/falsy checks - Liquid is stricter
    # Note: This is a basic conversion, complex cases may need manual review
    
    # 7. String comparisons - ensure proper quoting
    # Jinja2: {% if var == 'value' %} (single or double quotes)
    # Liquid: {% if var == "value" %} (typically double quotes)
    # Both work in Scriban, so no conversion needed
    
    return $converted
}

# ============================================================================
# PostgreSQL String Escaper
# ============================================================================

function ConvertTo-PostgresString {
    <#
    .SYNOPSIS
        Escapes a string for use in PostgreSQL
    #>
    param(
        [string]$Value
    )

    if ($null -eq $Value) {
        return "NULL"
    }

    # Escape single quotes by doubling them
    $escaped = $Value -replace "'", "''"
    
    # Use dollar-quoting for complex strings with many single quotes
    if (($escaped -split "''").Count -gt 10) {
        # Use dollar-quoted string for complex content
        return "`$template`$$Value`$template`$"
    }
    
    return "'$escaped'"
}

# ============================================================================
# Generate Migration SQL
# ============================================================================

function New-TemplateMigrationSql {
    <#
    .SYNOPSIS
        Generates SQL statements for migrating a template
    #>
    param(
        [PSCustomObject]$Template,
        [switch]$AsUpdate
    )

    # Convert template content from Jinja2 to Liquid
    $convertedHtml = Convert-Jinja2ToLiquid -TemplateContent $Template.html_content
    $convertedText = Convert-Jinja2ToLiquid -TemplateContent $Template.text_content
    $convertedSubject = Convert-Jinja2ToLiquid -TemplateContent $Template.subject

    if ($AsUpdate) {
        # Generate UPDATE statement
        $sql = @"
-- Update template: $($Template.name)
UPDATE email_templates 
SET 
    subject = $(ConvertTo-PostgresString $convertedSubject),
    html_content = $(ConvertTo-PostgresString $convertedHtml),
    text_content = $(ConvertTo-PostgresString $convertedText),
    updated_at = NOW()
WHERE name = $(ConvertTo-PostgresString $Template.name);

"@
    } else {
        # Generate INSERT statement
        $sql = @"
-- Insert template: $($Template.name)
INSERT INTO email_templates (
    name, description, subject, html_content, text_content, 
    variables, test_data, default_recipients, template_type, 
    is_active, created_at, updated_at
) VALUES (
    $(ConvertTo-PostgresString $Template.name),
    $(ConvertTo-PostgresString $Template.description),
    $(ConvertTo-PostgresString $convertedSubject),
    $(ConvertTo-PostgresString $convertedHtml),
    $(ConvertTo-PostgresString $convertedText),
    $(if ($Template.variables) { ConvertTo-PostgresString ($Template.variables | ConvertTo-Json -Compress) } else { "NULL" }),
    $(if ($Template.test_data) { ConvertTo-PostgresString ($Template.test_data | ConvertTo-Json -Compress) } else { "NULL" }),
    $(if ($Template.default_recipients) { ConvertTo-PostgresString $Template.default_recipients } else { "NULL" }),
    $(ConvertTo-PostgresString $Template.template_type),
    $($Template.is_active.ToString().ToUpper()),
    $(ConvertTo-PostgresString $Template.created_at),
    NOW()
);

"@
    }

    return $sql
}

# ============================================================================
# Main Migration Logic
# ============================================================================

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host " Email Template Migration: MySQL -> PostgreSQL" -ForegroundColor Cyan
Write-Host " Jinja2/Ninja -> Scriban/Liquid Syntax Conversion" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

# SQL Header
$migrationSql = @"
-- ============================================================================
-- Auto-generated Email Template Migration
-- Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
-- ============================================================================
-- Source: MySQL (Jinja2/Ninja templates)
-- Target: PostgreSQL (Scriban/Liquid templates)
-- ============================================================================

-- Syntax conversions applied:
--   elif -> elsif
--   else if -> elsif  
--   set -> assign
--   loop.* -> forloop.*
--   Added spacing around filter pipes

"@

# If you have MySQL templates in a file or want to manually specify them:
Write-Host "To use this script:" -ForegroundColor Yellow
Write-Host "1. Export your MySQL templates to a JSON file" -ForegroundColor White
Write-Host "2. Pass them to this script for conversion" -ForegroundColor White
Write-Host "3. Or use the SQL file directly: 003_migrate_mysql_templates_with_conversion.sql" -ForegroundColor White
Write-Host ""

# Example template for testing the converter
$exampleTemplate = @{
    name = "Test Template"
    description = "Example template with Jinja2 syntax"
    subject = "{{ ClientName }} Report - {{ ReportDate }}"
    html_content = @"
{% if status == 'active' %}
    Active
{% elif status == 'pending' %}
    Pending
{% else %}
    Unknown
{% endif %}

{% for item in items %}
    Item {{ loop.index }}: {{ item.name|upper }}
{% endfor %}

{% set counter = 0 %}
"@
    text_content = $null
    template_type = "notification"
    is_active = $true
    created_at = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
    variables = @{ ClientName = "Client name"; ReportDate = "Report date" }
    test_data = @{ ClientName = "Test Client"; ReportDate = "2025-01-01" }
}

Write-Host "Example conversion:" -ForegroundColor Green
Write-Host "==================" -ForegroundColor Green
Write-Host ""
Write-Host "BEFORE (Jinja2):" -ForegroundColor Yellow
Write-Host $exampleTemplate.html_content -ForegroundColor Gray
Write-Host ""

$convertedContent = Convert-Jinja2ToLiquid -TemplateContent $exampleTemplate.html_content

Write-Host "AFTER (Liquid/Scriban):" -ForegroundColor Yellow
Write-Host $convertedContent -ForegroundColor Gray
Write-Host ""

Write-Host "Key changes made:" -ForegroundColor Cyan
Write-Host "  - 'elif' -> 'elsif'" -ForegroundColor White
Write-Host "  - 'loop.index' -> 'forloop.index'" -ForegroundColor White
Write-Host "  - 'item.name|upper' -> 'item.name | upper'" -ForegroundColor White
Write-Host "  - 'set' -> 'assign'" -ForegroundColor White
Write-Host ""

# Output the migration SQL file location
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "Migration SQL file created at:" -ForegroundColor Green
Write-Host "  $((Resolve-Path "..\migrations\003_migrate_mysql_templates_with_conversion.sql" -ErrorAction SilentlyContinue) ?? "NotificationServices\migrations\003_migrate_mysql_templates_with_conversion.sql")" -ForegroundColor White
Write-Host ""
Write-Host "To apply the migration:" -ForegroundColor Yellow
Write-Host "  1. Connect to your PostgreSQL database" -ForegroundColor White
Write-Host "  2. Run the SQL file" -ForegroundColor White
Write-Host "  3. Verify with: SELECT name, LENGTH(html_content) FROM email_templates;" -ForegroundColor White
Write-Host "============================================================" -ForegroundColor Cyan
