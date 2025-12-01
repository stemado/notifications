-- ============================================================================
-- MySQL Query to Generate PostgreSQL INSERT Statements
-- ============================================================================
-- Run this query against the MySQL import_pulse database to generate
-- complete PostgreSQL INSERT statements with full HTML content.
--
-- Usage:
-- 1. Run this query in MySQL
-- 2. Copy the generated output
-- 3. Run the output against PostgreSQL NotificationService database
-- ============================================================================

SELECT CONCAT(
    'INSERT INTO email_templates (name, description, subject, html_content, text_content, variables, test_data, default_recipients, template_type, is_active, created_at, updated_at) VALUES (',
    QUOTE(name), ', ',
    IFNULL(QUOTE(description), 'NULL'), ', ',
    QUOTE(subject), ', ',
    IFNULL(QUOTE(html_content), 'NULL'), ', ',
    IFNULL(QUOTE(text_content), 'NULL'), ', ',
    IFNULL(CONCAT('''', REPLACE(JSON_UNQUOTE(JSON_OBJECT('data', variables)), '''', ''''''), ''''), 'NULL'), ', ',
    IFNULL(CONCAT('''', REPLACE(JSON_UNQUOTE(JSON_OBJECT('data', test_data)), '''', ''''''), ''''), 'NULL'), ', ',
    IFNULL(CONCAT('''', REPLACE(JSON_UNQUOTE(JSON_OBJECT('data', default_recipients)), '''', ''''''), ''''), 'NULL'), ', ',
    QUOTE(template_type), ', ',
    IF(is_active, 'TRUE', 'FALSE'), ', ',
    QUOTE(created_at), ', ',
    QUOTE(updated_at),
    ');'
) AS postgresql_insert
FROM email_templates
ORDER BY id;
