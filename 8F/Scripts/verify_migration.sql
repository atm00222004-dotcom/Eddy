SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'Logs' 
ORDER BY ordinal_position;

SELECT "Id", "TimeStamp", "Result", "FrequencyData" 
FROM public."Logs" 
WHERE "Id" >= 6;
