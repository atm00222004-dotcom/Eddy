-- ====================================================================
-- PostgreSQL Database Schema Setup for Eddy Current NDT Inspection App
-- Database: Eddy
-- Target Table: public.Logs
-- ====================================================================

-- 1. Create Database (Run separately if database 'Eddy' does not exist)
-- CREATE DATABASE "Eddy";

-- 2. Create "Logs" Table
CREATE TABLE IF NOT EXISTS public."Logs"
(
    "Id"                     BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "SerialNumber"           VARCHAR(100) NOT NULL,
    "BatchId"                VARCHAR(100) NOT NULL,
    "OperatorName"           VARCHAR(100) NOT NULL,
    "InspectionTimestamp"   TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ChannelNumber"          INT NOT NULL,
    "FrequencyHz"            DOUBLE PRECISION NOT NULL,
    "XValue"                 DOUBLE PRECISION NOT NULL,
    "YValue"                 DOUBLE PRECISION NOT NULL,
    "ResultPass"             BOOLEAN NOT NULL,
    "DefectType"             VARCHAR(100) NULL,
    "MachineId"              VARCHAR(150) NOT NULL,
    "CreatedAt"              TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
)
TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."Logs"
    OWNER to postgres;

-- 3. Column Descriptions / Comments
COMMENT ON TABLE  public."Logs"                       IS 'Stores individual inspection log entries for industrial Eddy Current NDT testing.';
COMMENT ON COLUMN public."Logs"."Id"                  IS 'Surrogate primary key auto-incrementing identity.';
COMMENT ON COLUMN public."Logs"."SerialNumber"        IS 'Unique serial number or barcode of the inspected component.';
COMMENT ON COLUMN public."Logs"."BatchId"             IS 'Production batch or lot number identifier.';
COMMENT ON COLUMN public."Logs"."OperatorName"        IS 'Name or ID of the operator performing the inspection.';
COMMENT ON COLUMN public."Logs"."InspectionTimestamp" IS 'Timestamp when the inspection occurred (UTC / Timezone aware).';
COMMENT ON COLUMN public."Logs"."ChannelNumber"       IS 'Sensor channel identifier (e.g., Channel 1, 2, 3, 4).';
COMMENT ON COLUMN public."Logs"."FrequencyHz"         IS 'Eddy current test frequency in Hz or kHz.';
COMMENT ON COLUMN public."Logs"."XValue"              IS 'X-component / Real part of the impedance signal.';
COMMENT ON COLUMN public."Logs"."YValue"              IS 'Y-component / Imaginary part of the impedance signal.';
COMMENT ON COLUMN public."Logs"."ResultPass"          IS 'True if the part passed inspection (OK), False if rejected (NOK).';
COMMENT ON COLUMN public."Logs"."DefectType"          IS 'Category/type of defect identified if failed (e.g., Crack, Surface Flaw, Hardness Error). NULL if passed.';
COMMENT ON COLUMN public."Logs"."MachineId"           IS 'Hardware fingerprint / Machine station ID.';
COMMENT ON COLUMN public."Logs"."CreatedAt"           IS 'Database record creation timestamp.';

-- 4. Performance Indexes
-- Index on BatchId for fast batch inspection reports
CREATE INDEX IF NOT EXISTS "IX_Logs_BatchId" 
    ON public."Logs" ("BatchId");

-- Index on SerialNumber for fast component traceability lookups
CREATE INDEX IF NOT EXISTS "IX_Logs_SerialNumber" 
    ON public."Logs" ("SerialNumber");

-- Index on InspectionTimestamp for date-range analytics and historical filtering
CREATE INDEX IF NOT EXISTS "IX_Logs_InspectionTimestamp" 
    ON public."Logs" ("InspectionTimestamp" DESC);

-- Composite Index on MachineId and ResultPass for machine performance & Pass/Fail metrics
CREATE INDEX IF NOT EXISTS "IX_Logs_Machine_Result" 
    ON public."Logs" ("MachineId", "ResultPass");
