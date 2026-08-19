-- ====================================================================
-- PostgreSQL Database Initialization Script for Eddy (EddyTube) Project
-- Target Database: EddyTube
-- Target Table: public.Logs
-- Location: D:\New folder\Eddy\Eddy\Scripts\Setup_EddyTube_Database.sql
-- ====================================================================

-- 1. Create Database (Run separately if database 'EddyTube' does not exist)
-- CREATE DATABASE "EddyTube";

-- 2. Create "Logs" Table
CREATE TABLE IF NOT EXISTS public."Logs"
(
    "Id"                 BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "TimeStamp"          TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ConfigurationJson"  JSONB NULL,
    "GraphDataJson"      JSONB NULL,
    "PartJson"           JSONB NULL,
    "BatchName"          VARCHAR(255) NULL,
    "Result"             BOOLEAN NOT NULL DEFAULT FALSE
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."Logs" OWNER TO postgres;

-- 3. Performance Indexes
CREATE INDEX IF NOT EXISTS "IX_Logs_BatchName" 
    ON public."Logs" ("BatchName");

CREATE INDEX IF NOT EXISTS "IX_Logs_TimeStamp" 
    ON public."Logs" ("TimeStamp" DESC);

CREATE INDEX IF NOT EXISTS "IX_Logs_Result" 
    ON public."Logs" ("Result");

-- 4. Column Descriptions / Comments
COMMENT ON TABLE  public."Logs"                      IS 'Stores inspection logs, configuration state, and signal graph vectors for EddyTube testing.';
COMMENT ON COLUMN public."Logs"."Id"                 IS 'Surrogate primary key.';
COMMENT ON COLUMN public."Logs"."TimeStamp"          IS 'Inspection timestamp.';
COMMENT ON COLUMN public."Logs"."ConfigurationJson" IS 'JSON configuration settings (Frequency, Gain, Phase, Thresholds).';
COMMENT ON COLUMN public."Logs"."GraphDataJson"     IS 'JSON signal vector graph data (X/Y coordinates, amplitudes).';
COMMENT ON COLUMN public."Logs"."PartJson"          IS 'JSON part details (PartNumber, BatchName, BatchNo, ImagePath).';
COMMENT ON COLUMN public."Logs"."BatchName"         IS 'Production batch name for filtering and summary reports.';
COMMENT ON COLUMN public."Logs"."Result"            IS 'True if inspection passed (OK), False if defect detected (NOK).';

-- 5. Mock Inspection Data Insertion
INSERT INTO public."Logs" ("TimeStamp", "ConfigurationJson", "GraphDataJson", "PartJson", "BatchName", "Result")
VALUES
-- Recent Records (Today)
(
    NOW() - INTERVAL '2 hours',
    '{"FrequenceNo": 8, "Factor": 100, "Gain": 32.0, "Phase": 45, "Channel": 1}'::jsonb,
    '{"ChannelId": 1, "X": [12, 18, 25, 30, 22, 15], "Y": [8, 14, 20, 28, 18, 10]}'::jsonb,
    '{"Name": "TUBE_BATCH_AUG", "PartNumber": "TB-50M-A1", "BatchName": "TUBE_BATCH_AUG", "BatchNo": 1, "BatchSize": 50}'::jsonb,
    'TUBE_BATCH_AUG',
    TRUE
),
(
    NOW() - INTERVAL '1 hour 45 minutes',
    '{"FrequenceNo": 8, "Factor": 100, "Gain": 32.0, "Phase": 45, "Channel": 1}'::jsonb,
    '{"ChannelId": 1, "X": [10, 14, 21, 29, 20, 13], "Y": [7, 12, 19, 26, 16, 9]}'::jsonb,
    '{"Name": "TUBE_BATCH_AUG", "PartNumber": "TB-50M-A1", "BatchName": "TUBE_BATCH_AUG", "BatchNo": 2, "BatchSize": 50}'::jsonb,
    'TUBE_BATCH_AUG',
    TRUE
),
(
    NOW() - INTERVAL '1 hour 30 minutes',
    '{"FrequenceNo": 8, "Factor": 100, "Gain": 32.0, "Phase": 45, "Channel": 1}'::jsonb,
    '{"ChannelId": 1, "X": [45, 95, 120, 180, 110, 60], "Y": [35, 80, 105, 160, 95, 48]}'::jsonb,
    '{"Name": "TUBE_BATCH_AUG", "PartNumber": "TB-50M-A1", "BatchName": "TUBE_BATCH_AUG", "BatchNo": 3, "BatchSize": 50}'::jsonb,
    'TUBE_BATCH_AUG',
    FALSE
),
(
    NOW() - INTERVAL '1 hour 15 minutes',
    '{"FrequenceNo": 8, "Factor": 100, "Gain": 32.0, "Phase": 45, "Channel": 1}'::jsonb,
    '{"ChannelId": 1, "X": [11, 16, 23, 28, 21, 14], "Y": [8, 13, 18, 25, 17, 10]}'::jsonb,
    '{"Name": "38.10x35.00", "PartNumber": "TB-38.10-01", "BatchName": "38.10x35.00", "BatchNo": 600, "BatchSize": 1000}'::jsonb,
    '38.10x35.00',
    TRUE
),
(
    NOW() - INTERVAL '45 minutes',
    '{"FrequenceNo": 8, "Factor": 100, "Gain": 35.0, "Phase": 30, "Channel": 1}'::jsonb,
    '{"ChannelId": 1, "X": [14, 19, 26, 32, 24, 16], "Y": [9, 15, 21, 29, 19, 11]}'::jsonb,
    '{"Name": "38.10x35.00", "PartNumber": "TB-38.10-01", "BatchName": "38.10x35.00", "BatchNo": 601, "BatchSize": 1000}'::jsonb,
    '38.10x35.00',
    TRUE
),
(
    NOW() - INTERVAL '30 minutes',
    '{"FrequenceNo": 8, "Factor": 100, "Gain": 35.0, "Phase": 30, "Channel": 1}'::jsonb,
    '{"ChannelId": 1, "X": [50, 110, 140, 210, 130, 70], "Y": [40, 90, 120, 185, 110, 55]}'::jsonb,
    '{"Name": "38.10x35.00", "PartNumber": "TB-38.10-01", "BatchName": "38.10x35.00", "BatchNo": 602, "BatchSize": 1000}'::jsonb,
    '38.10x35.00',
    FALSE
);
