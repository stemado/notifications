-- Migration: Create Notifications Schema
-- Phase 1: Full architecture implementation

CREATE TABLE notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Ownership & Scope
    user_id UUID NOT NULL,           -- Who sees this
    tenant_id UUID NULL,              -- NULL = ops team, NotNull = client portal

    -- Content
    severity VARCHAR(20) NOT NULL,   -- info, warning, urgent, critical
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,

    -- Source
    saga_id UUID NULL,                -- Link to saga
    client_id UUID NULL,              -- Link to client
    event_id UUID NULL,               -- Link to domain event (if event-sourced)
    event_type VARCHAR(100) NULL,     -- Type of event that triggered this

    -- Lifecycle
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    acknowledged_at TIMESTAMP NULL,
    dismissed_at TIMESTAMP NULL,
    expires_at TIMESTAMP NULL,

    -- Behavior
    repeat_interval INT NULL,         -- Minutes between repeats
    last_repeated_at TIMESTAMP NULL,
    requires_ack BOOLEAN DEFAULT FALSE,

    -- Grouping/Deduplication
    group_key VARCHAR(200) NULL,      -- e.g., "saga:stuck:{sagaId}"
    group_count INT DEFAULT 1,

    -- Actions & Metadata
    actions_json JSONB NULL,          -- Serialized NotificationAction[]
    metadata_json JSONB NULL          -- Additional context
);

-- Indexes for Phase 1
CREATE INDEX idx_notifications_user_unread
    ON notifications(user_id, acknowledged_at)
    WHERE acknowledged_at IS NULL;

CREATE INDEX idx_notifications_tenant
    ON notifications(tenant_id, created_at DESC);

CREATE INDEX idx_notifications_group_key
    ON notifications(group_key)
    WHERE acknowledged_at IS NULL;

CREATE INDEX idx_notifications_saga
    ON notifications(saga_id, created_at DESC);

-- Delivery Channels (Phase 2 - table created now for future use)
CREATE TABLE notification_deliveries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    notification_id UUID NOT NULL REFERENCES notifications(id) ON DELETE CASCADE,
    channel VARCHAR(20) NOT NULL,    -- signalr, email, sms, slack
    delivered_at TIMESTAMP NULL,
    failed_at TIMESTAMP NULL,
    error_message TEXT NULL,
    attempt_count INT DEFAULT 0
);

CREATE INDEX idx_deliveries_notification
    ON notification_deliveries(notification_id);

-- User Preferences (Phase 2 - table created now for future use)
CREATE TABLE user_notification_preferences (
    user_id UUID NOT NULL,
    channel VARCHAR(20) NOT NULL,
    min_severity VARCHAR(20) NOT NULL, -- Only notify if >= this
    enabled BOOLEAN DEFAULT TRUE,
    PRIMARY KEY (user_id, channel)
);

-- Subscriptions (Phase 2 - table created now for future use)
CREATE TABLE notification_subscriptions (
    user_id UUID NOT NULL,
    client_id UUID NULL,              -- NULL = all clients
    saga_id UUID NULL,                -- NULL = all sagas for client
    min_severity VARCHAR(20) NOT NULL,
    PRIMARY KEY (user_id, client_id, saga_id)
);
