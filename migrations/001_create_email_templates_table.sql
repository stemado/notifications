-- Migration: Create email_templates table for NotificationService
-- Date: 2025-11-30
-- Description: Phase 0 - Migrate email template management to NotificationService.Api

-- ============================================================================
-- Create email_templates table
-- ============================================================================
CREATE TABLE IF NOT EXISTS email_templates (
    id                  INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name                VARCHAR(100) NOT NULL,
    description         VARCHAR(500),
    subject             VARCHAR(500) NOT NULL,
    html_content        TEXT,
    text_content        TEXT,
    variables           JSONB,
    test_data           JSONB,
    default_recipients  JSONB,
    template_type       VARCHAR(50) NOT NULL DEFAULT 'notification',
    is_active           BOOLEAN NOT NULL DEFAULT TRUE,
    created_at          TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMP NOT NULL DEFAULT NOW()
);

-- ============================================================================
-- Create indexes
-- ============================================================================

-- Unique index on template name
CREATE UNIQUE INDEX IF NOT EXISTS idx_email_templates_name
    ON email_templates (name);

-- Index for querying active templates by type
CREATE INDEX IF NOT EXISTS idx_email_templates_active_type
    ON email_templates (is_active, template_type);

-- ============================================================================
-- Add comments for documentation
-- ============================================================================
COMMENT ON TABLE email_templates IS 'Email notification templates with Liquid/Jinja2 syntax support';
COMMENT ON COLUMN email_templates.id IS 'Primary key - auto-generated sequential ID';
COMMENT ON COLUMN email_templates.name IS 'Unique template name (e.g., daily_import_summary, escalation_alert)';
COMMENT ON COLUMN email_templates.description IS 'Human-readable description of the template purpose';
COMMENT ON COLUMN email_templates.subject IS 'Email subject line (supports template variables)';
COMMENT ON COLUMN email_templates.html_content IS 'HTML body content with template variables';
COMMENT ON COLUMN email_templates.text_content IS 'Plain text body content (fallback for non-HTML clients)';
COMMENT ON COLUMN email_templates.variables IS 'JSON object defining template variables and descriptions';
COMMENT ON COLUMN email_templates.test_data IS 'JSON object with sample data for preview/testing';
COMMENT ON COLUMN email_templates.default_recipients IS 'JSON array of default recipient email addresses';
COMMENT ON COLUMN email_templates.template_type IS 'Template category (notification, success, error, escalation)';
COMMENT ON COLUMN email_templates.is_active IS 'Whether template is active and available for use';
COMMENT ON COLUMN email_templates.created_at IS 'Timestamp when template was created';
COMMENT ON COLUMN email_templates.updated_at IS 'Timestamp when template was last modified';

-- ============================================================================
-- Verification query (run after migration to confirm)
-- ============================================================================
-- SELECT column_name, data_type, is_nullable, column_default
-- FROM information_schema.columns
-- WHERE table_name = 'email_templates'
-- ORDER BY ordinal_position;
