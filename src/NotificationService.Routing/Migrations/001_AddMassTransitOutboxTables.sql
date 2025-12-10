-- MassTransit Outbox Tables for PostgreSQL
-- These tables are required for the transactional outbox pattern
-- Run this script against the notifications database

-- Outbox Message table - stores messages pending delivery to the message broker
CREATE TABLE IF NOT EXISTS "OutboxMessage" (
    "SequenceNumber" BIGSERIAL NOT NULL,
    "EnqueueTime" TIMESTAMP WITH TIME ZONE,
    "SentTime" TIMESTAMP WITH TIME ZONE NOT NULL,
    "Headers" TEXT,
    "Properties" TEXT,
    "InboxMessageId" UUID,
    "InboxConsumerId" UUID,
    "OutboxId" UUID,
    "MessageId" UUID NOT NULL,
    "ContentType" VARCHAR(256) NOT NULL,
    "MessageType" TEXT NOT NULL,
    "Body" TEXT NOT NULL,
    "ConversationId" UUID,
    "CorrelationId" UUID,
    "InitiatorId" UUID,
    "RequestId" UUID,
    "SourceAddress" VARCHAR(256),
    "DestinationAddress" VARCHAR(256),
    "ResponseAddress" VARCHAR(256),
    "FaultAddress" VARCHAR(256),
    "ExpirationTime" TIMESTAMP WITH TIME ZONE,
    CONSTRAINT "PK_OutboxMessage" PRIMARY KEY ("SequenceNumber")
);

-- Index for outbox message delivery
CREATE INDEX IF NOT EXISTS "IX_OutboxMessage_EnqueueTime" ON "OutboxMessage" ("EnqueueTime");
CREATE INDEX IF NOT EXISTS "IX_OutboxMessage_OutboxId_SequenceNumber" ON "OutboxMessage" ("OutboxId", "SequenceNumber");

-- Outbox State table - tracks outbox delivery state per producer
CREATE TABLE IF NOT EXISTS "OutboxState" (
    "OutboxId" UUID NOT NULL,
    "LockId" UUID NOT NULL,
    "RowVersion" BYTEA,
    "Created" TIMESTAMP WITH TIME ZONE NOT NULL,
    "Delivered" TIMESTAMP WITH TIME ZONE,
    "LastSequenceNumber" BIGINT,
    CONSTRAINT "PK_OutboxState" PRIMARY KEY ("OutboxId")
);

CREATE INDEX IF NOT EXISTS "IX_OutboxState_Created" ON "OutboxState" ("Created");

-- Inbox State table - tracks message consumption for idempotency
CREATE TABLE IF NOT EXISTS "InboxState" (
    "Id" BIGSERIAL NOT NULL,
    "MessageId" UUID NOT NULL,
    "ConsumerId" UUID NOT NULL,
    "LockId" UUID NOT NULL,
    "RowVersion" BYTEA,
    "Received" TIMESTAMP WITH TIME ZONE NOT NULL,
    "ReceiveCount" INT NOT NULL,
    "ExpirationTime" TIMESTAMP WITH TIME ZONE,
    "Consumed" TIMESTAMP WITH TIME ZONE,
    "Delivered" TIMESTAMP WITH TIME ZONE,
    "LastSequenceNumber" BIGINT,
    CONSTRAINT "PK_InboxState" PRIMARY KEY ("Id"),
    CONSTRAINT "AK_InboxState_MessageId_ConsumerId" UNIQUE ("MessageId", "ConsumerId")
);

CREATE INDEX IF NOT EXISTS "IX_InboxState_Delivered" ON "InboxState" ("Delivered");

-- Grant permissions if needed (adjust schema and user as necessary)
-- GRANT ALL ON TABLE "OutboxMessage" TO your_app_user;
-- GRANT ALL ON TABLE "OutboxState" TO your_app_user;
-- GRANT ALL ON TABLE "InboxState" TO your_app_user;
-- GRANT USAGE, SELECT ON SEQUENCE "OutboxMessage_SequenceNumber_seq" TO your_app_user;
-- GRANT USAGE, SELECT ON SEQUENCE "InboxState_Id_seq" TO your_app_user;
