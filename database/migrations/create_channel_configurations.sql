-- Channel Configurations Table
-- Stores configuration for notification channels (Email, SMS, Teams, SignalR)

CREATE TABLE IF NOT EXISTS channel_configurations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    channel VARCHAR(50) NOT NULL UNIQUE,
    enabled BOOLEAN NOT NULL DEFAULT false,
    configured BOOLEAN NOT NULL DEFAULT false,
    configuration_json JSONB NOT NULL DEFAULT '{}',
    last_tested_at TIMESTAMPTZ,
    test_status VARCHAR(20), -- 'success', 'failed', 'pending'
    test_error TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_by VARCHAR(255)
);

-- Create index for quick channel lookup
CREATE INDEX IF NOT EXISTS idx_channel_configurations_channel ON channel_configurations(channel);

-- Create index for enabled channels
CREATE INDEX IF NOT EXISTS idx_channel_configurations_enabled ON channel_configurations(enabled) WHERE enabled = true;

-- Insert default channel configurations
INSERT INTO channel_configurations (channel, enabled, configured, configuration_json)
VALUES
    ('SignalR', true, true, '{"hubUrl": "/hubs/notifications", "autoReconnect": true}'),
    ('Email', false, false, '{"provider": "graph", "smtpPort": 587, "enableSsl": true}'),
    ('SMS', false, false, '{"provider": "twilio"}'),
    ('Teams', false, false, '{}')
ON CONFLICT (channel) DO NOTHING;

-- Add comment to table
COMMENT ON TABLE channel_configurations IS 'Stores notification channel configuration including credentials (encrypted in production)';
COMMENT ON COLUMN channel_configurations.configuration_json IS 'JSON blob containing channel-specific settings. Sensitive data should be encrypted.';
