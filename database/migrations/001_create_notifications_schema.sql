-- Migration: Create Notifications Schema
-- Phase 1: Full architecture implementation

-- Core Notifications Table
CREATE TABLE Notifications (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Ownership & Scope
    UserId UUID NOT NULL,           -- Who sees this
    TenantId UUID NULL,              -- NULL = ops team, NotNull = client portal

    -- Content
    Severity VARCHAR(20) NOT NULL,   -- info, warning, urgent, critical
    Title VARCHAR(200) NOT NULL,
    Message TEXT NOT NULL,

    -- Source
    SagaId UUID NULL,                -- Link to saga
    ClientId UUID NULL,              -- Link to client
    EventId UUID NULL,               -- Link to domain event (if event-sourced)
    EventType VARCHAR(100) NULL,     -- Type of event that triggered this

    -- Lifecycle
    CreatedAt TIMESTAMP NOT NULL DEFAULT NOW(),
    AcknowledgedAt TIMESTAMP NULL,
    DismissedAt TIMESTAMP NULL,
    ExpiresAt TIMESTAMP NULL,

    -- Behavior
    RepeatInterval INT NULL,         -- Minutes between repeats
    LastRepeatedAt TIMESTAMP NULL,
    RequiresAck BOOLEAN DEFAULT FALSE,

    -- Grouping/Deduplication
    GroupKey VARCHAR(200) NULL,      -- e.g., "saga:stuck:{sagaId}"
    GroupCount INT DEFAULT 1,

    -- Actions & Metadata
    ActionsJson JSONB NULL,          -- Serialized NotificationAction[]
    MetadataJson JSONB NULL          -- Additional context
);

-- Indexes for Phase 1
CREATE INDEX idx_notifications_user_unread
    ON Notifications(UserId, AcknowledgedAt)
    WHERE AcknowledgedAt IS NULL;

CREATE INDEX idx_notifications_tenant
    ON Notifications(TenantId, CreatedAt DESC);

CREATE INDEX idx_notifications_group_key
    ON Notifications(GroupKey)
    WHERE AcknowledgedAt IS NULL;

CREATE INDEX idx_notifications_saga
    ON Notifications(SagaId, CreatedAt DESC);

-- Delivery Channels (Phase 2 - table created now for future use)
CREATE TABLE NotificationDeliveries (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    NotificationId UUID NOT NULL REFERENCES Notifications(Id) ON DELETE CASCADE,
    Channel VARCHAR(20) NOT NULL,    -- signalr, email, sms, slack
    DeliveredAt TIMESTAMP NULL,
    FailedAt TIMESTAMP NULL,
    ErrorMessage TEXT NULL,
    AttemptCount INT DEFAULT 0
);

CREATE INDEX idx_deliveries_notification
    ON NotificationDeliveries(NotificationId);

-- User Preferences (Phase 2 - table created now for future use)
CREATE TABLE UserNotificationPreferences (
    UserId UUID NOT NULL,
    Channel VARCHAR(20) NOT NULL,
    MinSeverity VARCHAR(20) NOT NULL, -- Only notify if >= this
    Enabled BOOLEAN DEFAULT TRUE,
    PRIMARY KEY (UserId, Channel)
);

-- Subscriptions (Phase 2 - table created now for future use)
CREATE TABLE NotificationSubscriptions (
    UserId UUID NOT NULL,
    ClientId UUID NULL,              -- NULL = all clients
    SagaId UUID NULL,                -- NULL = all sagas for client
    MinSeverity VARCHAR(20) NOT NULL,
    PRIMARY KEY (UserId, ClientId, SagaId)
);
