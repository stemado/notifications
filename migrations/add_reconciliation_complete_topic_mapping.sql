-- Migration: Add TopicTemplateMapping for ReconciliationComplete
-- Date: 2026-01-08
-- Description: Creates topic-template mapping to link CensusReconciliation/ReconciliationComplete
--              events to Template 7 ("Census Processing Complete")
--
-- This mapping enables the routing system to automatically use the correct email template
-- when saga completion events are published through the routing API.

-- Check if mapping already exists (idempotent migration)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM topic_template_mappings
        WHERE service = 'CensusReconciliation'
        AND topic = 'ReconciliationComplete'
        AND client_id IS NULL
    ) THEN
        INSERT INTO topic_template_mappings (
            id,
            service,
            topic,
            client_id,
            template_id,
            is_enabled,
            priority,
            created_at,
            updated_at,
            updated_by
        ) VALUES (
            gen_random_uuid(),
            'CensusReconciliation',
            'ReconciliationComplete',
            NULL,  -- NULL = default mapping for all clients
            7,     -- Template 7: "Census Processing Complete"
            TRUE,
            0,     -- Default priority
            NOW(),
            NOW(),
            'migration'
        );
        RAISE NOTICE 'Created topic-template mapping: CensusReconciliation/ReconciliationComplete -> Template 7';
    ELSE
        RAISE NOTICE 'Topic-template mapping already exists for CensusReconciliation/ReconciliationComplete';
    END IF;
END $$;

-- Also add mappings for other common reconciliation event types

-- ReconciliationEscalation
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM topic_template_mappings
        WHERE service = 'CensusReconciliation'
        AND topic = 'ReconciliationEscalation'
        AND client_id IS NULL
    ) THEN
        INSERT INTO topic_template_mappings (
            id, service, topic, client_id, template_id, is_enabled, priority, created_at, updated_at, updated_by
        ) VALUES (
            gen_random_uuid(),
            'CensusReconciliation',
            'ReconciliationEscalation',
            NULL,
            8,  -- Escalation Alert template (verify this ID exists)
            TRUE,
            0,
            NOW(),
            NOW(),
            'migration'
        );
        RAISE NOTICE 'Created topic-template mapping: CensusReconciliation/ReconciliationEscalation -> Template 8';
    ELSE
        RAISE NOTICE 'Topic-template mapping already exists for CensusReconciliation/ReconciliationEscalation';
    END IF;
END $$;

-- WorkflowStuck
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM topic_template_mappings
        WHERE service = 'CensusReconciliation'
        AND topic = 'WorkflowStuck'
        AND client_id IS NULL
    ) THEN
        INSERT INTO topic_template_mappings (
            id, service, topic, client_id, template_id, is_enabled, priority, created_at, updated_at, updated_by
        ) VALUES (
            gen_random_uuid(),
            'CensusReconciliation',
            'WorkflowStuck',
            NULL,
            8,  -- Use Escalation Alert template
            TRUE,
            0,
            NOW(),
            NOW(),
            'migration'
        );
        RAISE NOTICE 'Created topic-template mapping: CensusReconciliation/WorkflowStuck -> Template 8';
    ELSE
        RAISE NOTICE 'Topic-template mapping already exists for CensusReconciliation/WorkflowStuck';
    END IF;
END $$;

-- DailyImportSuccess (for import events)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM topic_template_mappings
        WHERE service = 'CensusReconciliation'
        AND topic = 'DailyImportSuccess'
        AND client_id IS NULL
    ) THEN
        INSERT INTO topic_template_mappings (
            id, service, topic, client_id, template_id, is_enabled, priority, created_at, updated_at, updated_by
        ) VALUES (
            gen_random_uuid(),
            'CensusReconciliation',
            'DailyImportSuccess',
            NULL,
            7,  -- Same template as ReconciliationComplete
            TRUE,
            0,
            NOW(),
            NOW(),
            'migration'
        );
        RAISE NOTICE 'Created topic-template mapping: CensusReconciliation/DailyImportSuccess -> Template 7';
    ELSE
        RAISE NOTICE 'Topic-template mapping already exists for CensusReconciliation/DailyImportSuccess';
    END IF;
END $$;

-- DailyImportFailure
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM topic_template_mappings
        WHERE service = 'CensusReconciliation'
        AND topic = 'DailyImportFailure'
        AND client_id IS NULL
    ) THEN
        INSERT INTO topic_template_mappings (
            id, service, topic, client_id, template_id, is_enabled, priority, created_at, updated_at, updated_by
        ) VALUES (
            gen_random_uuid(),
            'CensusReconciliation',
            'DailyImportFailure',
            NULL,
            9,  -- Import Failure template (verify this ID exists)
            TRUE,
            0,
            NOW(),
            NOW(),
            'migration'
        );
        RAISE NOTICE 'Created topic-template mapping: CensusReconciliation/DailyImportFailure -> Template 9';
    ELSE
        RAISE NOTICE 'Topic-template mapping already exists for CensusReconciliation/DailyImportFailure';
    END IF;
END $$;

-- Verify the mappings were created
SELECT
    id,
    service,
    topic,
    client_id,
    template_id,
    is_enabled,
    priority,
    created_at
FROM topic_template_mappings
WHERE service = 'CensusReconciliation'
ORDER BY topic;
