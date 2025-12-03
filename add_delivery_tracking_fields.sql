-- Add new columns to notification_deliveries table
-- Only add columns that don't already exist

DO $$
BEGIN
    -- Add status column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'notification_deliveries' AND column_name = 'status') THEN
        ALTER TABLE notification_deliveries
        ADD COLUMN status VARCHAR(20) NOT NULL DEFAULT 'Pending';
    END IF;

    -- Add max_attempts column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'notification_deliveries' AND column_name = 'max_attempts') THEN
        ALTER TABLE notification_deliveries
        ADD COLUMN max_attempts INTEGER NOT NULL DEFAULT 3;
    END IF;

    -- Add next_retry_at column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'notification_deliveries' AND column_name = 'next_retry_at') THEN
        ALTER TABLE notification_deliveries
        ADD COLUMN next_retry_at TIMESTAMP WITH TIME ZONE NULL;
    END IF;

    -- Add created_at column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'notification_deliveries' AND column_name = 'created_at') THEN
        ALTER TABLE notification_deliveries
        ADD COLUMN created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW();
    END IF;

    -- Add response_data column if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                   WHERE table_name = 'notification_deliveries' AND column_name = 'response_data') THEN
        ALTER TABLE notification_deliveries
        ADD COLUMN response_data JSONB NULL;
    END IF;
END $$;

-- Create indexes if they don't exist
CREATE INDEX IF NOT EXISTS idx_deliveries_queue
ON notification_deliveries (status, next_retry_at)
WHERE status IN ('Pending', 'Failed');

CREATE INDEX IF NOT EXISTS idx_deliveries_channel_history
ON notification_deliveries (channel, created_at DESC);

-- Display results
SELECT 'Migration completed successfully!' AS status;
