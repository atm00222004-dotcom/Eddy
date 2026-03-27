-- Table: public.Logs

-- DROP TABLE IF EXISTS public."Logs";

CREATE TABLE IF NOT EXISTS public."Logs"
(
    "Id" integer NOT NULL DEFAULT nextval('"logs_Id_seq"'::regclass),
    "TimeStamp" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "ConfigurationJson" jsonb,
    "GraphDataJson" jsonb,
    "PartJson" jsonb,
    "BatchName" character varying(255) COLLATE pg_catalog."default",
    "Result" boolean DEFAULT false,
    CONSTRAINT logs_pkey PRIMARY KEY ("Id")
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."Logs"
    OWNER to postgres;