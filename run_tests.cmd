@echo off
echo ===================================================
echo 8F Eddy Current Inspection System — Test Runner
echo ===================================================
echo.

dotnet test "%~dp08F.sln" --verbosity normal

if %ERRORLEVEL% EQU 0 (
    echo.
    echo [SUCCESS] All automated tests passed cleanly!
) else (
    echo.
    echo [FAILURE] Test suite encountered failures. Please inspect the log output above.
)

pause
