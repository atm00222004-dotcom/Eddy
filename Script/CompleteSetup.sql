-- ====================================================================
-- MASTER DATABASE INITIALIZATION & MIGRATION SCRIPT FOR EDDY APPLICATION
-- Target Database: Eddy
-- ====================================================================

-- 1. Create "Logs" Table if missing
CREATE TABLE IF NOT EXISTS public."Logs"
(
    "Id" BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY
);
ALTER TABLE IF EXISTS public."Logs" OWNER TO postgres;

-- 2. Safely add any missing legacy or modern columns to "Logs" table
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "ChId" INT DEFAULT 1;
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "Result" BOOLEAN DEFAULT TRUE;
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "TimeStamp" TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "FDData" JSON;
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "PartData" JSON;
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "PartName" TEXT;
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "BatchName" TEXT;
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "SrNo" TEXT;
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "BatchNo" BIGINT DEFAULT 0;
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "Ch1Result" BOOLEAN DEFAULT TRUE;
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "Ch2Result" BOOLEAN DEFAULT TRUE;
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "Ch3Result" BOOLEAN DEFAULT TRUE;
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "Ch4Result" BOOLEAN DEFAULT TRUE;

ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "SerialNumber" VARCHAR(100);
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "BatchId" VARCHAR(100);
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "OperatorName" VARCHAR(100);
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "InspectionTimestamp" TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP;
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "ChannelNumber" INT DEFAULT 1;
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "FrequencyHz" DOUBLE PRECISION DEFAULT 0;
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "XValue" DOUBLE PRECISION DEFAULT 0;
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "YValue" DOUBLE PRECISION DEFAULT 0;
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "ResultPass" BOOLEAN DEFAULT TRUE;
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "DefectType" VARCHAR(100);
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "MachineId" VARCHAR(150);
ALTER TABLE public."Logs" ADD COLUMN IF NOT EXISTS "CreatedAt" TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP;

-- 3. Create Performance Indexes
CREATE INDEX IF NOT EXISTS "IX_Logs_BatchName" ON public."Logs" ("BatchName");
CREATE INDEX IF NOT EXISTS "IX_Logs_SrNo" ON public."Logs" ("SrNo");
CREATE INDEX IF NOT EXISTS "IX_Logs_TimeStamp" ON public."Logs" ("TimeStamp" DESC);
CREATE INDEX IF NOT EXISTS "IX_Logs_BatchId" ON public."Logs" ("BatchId");
CREATE INDEX IF NOT EXISTS "IX_Logs_SerialNumber" ON public."Logs" ("SerialNumber");

-- 4. Create "Operators" Table
CREATE TABLE IF NOT EXISTS public."Operators"
(
    "Id" SERIAL PRIMARY KEY,
    "OperatorName" VARCHAR(100) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE
);
ALTER TABLE IF EXISTS public."Operators" OWNER TO postgres;

-- 5. Create "PartFamilies" Table
CREATE TABLE IF NOT EXISTS public."PartFamilies"
(
    "Id" SERIAL PRIMARY KEY,
    "FamilyName" VARCHAR(100) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE
);
ALTER TABLE IF EXISTS public."PartFamilies" OWNER TO postgres;

-- 6. Create "Parts" Table
CREATE TABLE IF NOT EXISTS public."Parts"
(
    "Id" SERIAL PRIMARY KEY,
    "PartFamilyId" INT NOT NULL,
    "PartNumber" VARCHAR(100) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT "FK_Parts_PartFamilies" FOREIGN KEY ("PartFamilyId") REFERENCES public."PartFamilies"("Id")
);
ALTER TABLE IF EXISTS public."Parts" OWNER TO postgres;

-- 7. Create "configurationkeylogs" Table (lowercase for unquoted PostgreSQL queries)
CREATE TABLE IF NOT EXISTS public.configurationkeylogs
(
    id SERIAL PRIMARY KEY,
    productname VARCHAR(200) NOT NULL DEFAULT '',
    customername VARCHAR(200) NOT NULL DEFAULT '',
    machineid VARCHAR(200) NOT NULL DEFAULT '',
    configurationfilename VARCHAR(200) NOT NULL DEFAULT '',
    generatedfilename VARCHAR(200) NOT NULL DEFAULT '',
    generatedfile BYTEA NOT NULL,
    generateddate TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updateddate TIMESTAMPTZ NULL
);
ALTER TABLE IF EXISTS public.configurationkeylogs OWNER TO postgres;

-- 8. Insert Default Master Data
INSERT INTO public."Operators" ("OperatorName") VALUES
('Operator 1'), ('Operator 2'), ('Operator 3'), ('Operator 4')
ON CONFLICT DO NOTHING;

INSERT INTO public."PartFamilies" ("FamilyName") VALUES
('Main Plate'), ('Cover Plate'), ('Hub Flange'), ('Hub'), ('Support Washer')
ON CONFLICT DO NOTHING;

INSERT INTO public."Parts" ("PartFamilyId", "PartNumber") VALUES
(1, 'L-03120-0HQ5-00'), (1, 'L-03120-1554-00'), (1, 'L-03120-1982-00'),
(2, 'L-03124-0GP4-00'), (2, 'L-03124-0P20-01'), (2, 'L-03124-1732-00'),
(3, 'L-03125-0GY7-01'), (3, 'L-03125-0GBF-05'), (3, 'L-03125-2165-05'),
(4, 'L-03133-0GF8-06'), (4, 'L-03133-1418-01'), (4, 'L-03133-1418-05'), (4, 'L-03133-1717-08'),
(5, 'L-03158-0GK5-00'), (5, 'L-03158-0P22-00'), (5, 'L-03158-1489-00')
ON CONFLICT DO NOTHING;
