-- ==========================================
-- AUTO ELLIPSE TESTS TABLE (RAW TEST RUNS)
-- ==========================================
CREATE TABLE IF NOT EXISTS public."AutoEllipseTests"
(
    "Id" integer NOT NULL GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ChId" integer NOT NULL,
    "TestNumber" integer NOT NULL,
    "TimeStamp" timestamp without time zone NOT NULL DEFAULT now(),
    "OperatorName" text,
    "FrequencyValues" json NOT NULL,
    "IsDeleted" boolean NOT NULL DEFAULT false
);

ALTER TABLE IF EXISTS public."AutoEllipseTests"
    OWNER to postgres;


-- ==========================================
-- AUTO ELLIPSE RESULTS TABLE (AUDIT TRAIL)
-- ==========================================
CREATE TABLE IF NOT EXISTS public."AutoEllipseResults"
(
    "Id" integer NOT NULL GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "ChId" integer NOT NULL,
    "Frequency" text NOT NULL,
    "TimeStamp" timestamp without time zone NOT NULL DEFAULT now(),
    "SelectedTestIds" json NOT NULL,
    "ComputedCenterX" numeric,
    "ComputedCenterY" numeric,
    "ComputedWidth" numeric,
    "ComputedHeight" numeric,
    "ComputedRotationAngle" numeric,
    "SampleCount" integer NOT NULL
);

ALTER TABLE IF EXISTS public."AutoEllipseResults"
    OWNER to postgres;
