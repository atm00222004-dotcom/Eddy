-- ============================================================================
-- MIGRATION: 8F Frequency Data JSONB Consolidation & Drop X/Y Columns
-- Database: EddyShorter
-- Target Table: public."Logs"
-- ============================================================================

DO $$
BEGIN
    -- 1. Ensure FrequencyData jsonb column exists
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' AND table_name = 'Logs' AND column_name = 'FrequencyData'
    ) THEN
        ALTER TABLE public."Logs" ADD COLUMN "FrequencyData" jsonb;
        RAISE NOTICE 'Added column "FrequencyData" jsonb to public."Logs"';
    END IF;

    -- 2. Populate FrequencyData from existing ResultData if present
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' AND table_name = 'Logs' AND column_name = 'ResultData'
    ) THEN
        UPDATE public."Logs" 
        SET "FrequencyData" = "ResultData"::jsonb 
        WHERE "FrequencyData" IS NULL AND "ResultData" IS NOT NULL;
        RAISE NOTICE 'Migrated existing "ResultData" values into "FrequencyData"';

        -- Convert ResultData column to jsonb as well for consistency
        ALTER TABLE public."Logs" ALTER COLUMN "ResultData" TYPE jsonb USING "ResultData"::jsonb;
        RAISE NOTICE 'Converted "ResultData" to jsonb';
    END IF;

    -- 3. Safely drop X and Y columns
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' AND table_name = 'Logs' AND column_name = 'X'
    ) THEN
        ALTER TABLE public."Logs" DROP COLUMN "X";
        RAISE NOTICE 'Dropped column "X" from public."Logs"';
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' AND table_name = 'Logs' AND column_name = 'Y'
    ) THEN
        ALTER TABLE public."Logs" DROP COLUMN "Y";
        RAISE NOTICE 'Dropped column "Y" from public."Logs"';
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' AND table_name = 'Logs' AND column_name = 'x'
    ) THEN
        ALTER TABLE public."Logs" DROP COLUMN "x";
        RAISE NOTICE 'Dropped column "x" from public."Logs"';
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' AND table_name = 'Logs' AND column_name = 'y'
    ) THEN
        ALTER TABLE public."Logs" DROP COLUMN "y";
        RAISE NOTICE 'Dropped column "y" from public."Logs"';
    END IF;
END $$;
