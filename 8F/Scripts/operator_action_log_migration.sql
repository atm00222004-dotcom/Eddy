CREATE TABLE IF NOT EXISTS public."OperatorActionLog"
(
    "Id" bigint NOT NULL GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "EventTimestamp" timestamp without time zone NOT NULL,
    "ActionType" text NOT NULL,
    "MouseButton" text,
    "ControlName" text,
    "KeyName" text,
    "WindowTitle" text,
    "OperatorName" text,
    "SessionId" uuid NOT NULL,
    "ScreenX" integer,
    "ScreenY" integer
);

ALTER TABLE public."OperatorActionLog" ADD COLUMN IF NOT EXISTS "ScreenX" integer;
ALTER TABLE public."OperatorActionLog" ADD COLUMN IF NOT EXISTS "ScreenY" integer;

CREATE INDEX IF NOT EXISTS "IX_OperatorActionLog_EventTimestamp"
    ON public."OperatorActionLog" ("EventTimestamp");
