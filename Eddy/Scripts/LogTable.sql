-- Table: public.Logs for EddyTube Database

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

CREATE INDEX IF NOT EXISTS "IX_Logs_BatchName" ON public."Logs" ("BatchName");
CREATE INDEX IF NOT EXISTS "IX_Logs_TimeStamp" ON public."Logs" ("TimeStamp" DESC);
CREATE INDEX IF NOT EXISTS "IX_Logs_Result" ON public."Logs" ("Result");