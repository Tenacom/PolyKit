@echo off & setlocal EnableExtensions EnableDelayedExpansion
pushd "%~dp0"

:: .NET environment variables
set DOTNET_NOLOGO=true
set DOTNET_CLI_UI_LANGUAGE=en-US

:: Configuration values
set _ARTIFACTS_DIRECTORY=
set _NUPKG_DIRECTORY=
set _SOLUTION_NAME=
set _DEFAULT_TASK=
set _MSBUILD_CONFIGURATION=
set _MSBUILD_VERBOSITY=
set _MSBUILD_OPTIONS=
set _SKIP_INSPECT=
set _SKIP_TEST=
set _SKIP_PUSH=
set _VS_MSBUILD_EXE=
set _NUGET_PUSH_SOURCE=
set _NUGET_PUSH_API_KEY=
set _NUGET_PUSH_SYMBOL_SOURCE=
set _NUGET_PUSH_SYMBOL_API_KEY=

:: Load configuration
if exist build-config.cmd call build-config.cmd

:: Default for solution name is the same name as containing folder, including extension
if "%_SOLUTION_NAME%" == "" call :F_SetToCurrentDirectoryName _SOLUTION_NAME
set _SOLUTION_FILE=%_SOLUTION_NAME%.sln
if not exist "%_SOLUTION_FILE%" (
    echo *** Solution file '%_SOLUTION_FILE%' not found. >CON:
    exit /B 1
)

:: Other default configuration values
if "%_DEFAULT_TASK%"=="" set _DEFAULT_TASK=Pack
if "%_MSBUILD_CONFIGURATION%"=="" set _MSBUILD_CONFIGURATION=Release
if "%_MSBUILD_VERBOSITY%"=="" set _MSBUILD_VERBOSITY=normal
if "%_MSBUILD_OPTIONS%"=="" set _MSBUILD_OPTIONS=
if "%_SKIP_INSPECT%"=="" set _SKIP_INSPECT=0
if "%_SKIP_TEST%"=="" set _SKIP_TEST=0
if "%_SKIP_PUSH%"=="" set _SKIP_PUSH=0
if "%_VS_MSBUILD_EXE%"=="" set _VS_MSBUILD_EXE="%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if "%_NUGET_PUSH_SYMBOL_SOURCE%" == "" set _NUGET_PUSH_SYMBOL_SOURCE=%_NUGET_PUSH_SOURCE%
if "%_NUGET_PUSH_SYMBOL_API_KEY%" == "" set _NUGET_PUSH_SYMBOL_SOURCE=%_NUGET_PUSH_API_KEY%

:: Default for artifacts directory, as per Buildvana SDK defaults
if "%_ARTIFACTS_DIRECTORY%" == "" set _ARTIFACTS_DIRECTORY=artifacts

:: Default for NuGet package directory, as per Buildvana SDK defaults
if "%_NUPKG_DIRECTORY%" == "" set _NUPKG_DIRECTORY=%_ARTIFACTS_DIRECTORY%\%_MSBUILD_CONFIGURATION%

:: Use Visual Studio's MSBuild if specified
if /I "%1" equ "VS" (
    set _VS=1
    shift
)

:: Task to run
set _TASK=%1
if "%_TASK%"=="" set _TASK=%_DEFAULT_TASK%

:: Define task dependencies
if /I "%_TASK%"=="Clean" (
    call :F_Run_Tasks Clean
) else if /I "%_TASK%"=="Tools" (
    call :F_Run_Tasks Clean Tools
) else if /I "%_TASK%"=="Restore" (
    call :F_Run_Tasks Clean Tools Restore
) else if /I "%_TASK%"=="Build" (
    call :F_Run_Tasks Clean Tools Restore Build
) else if /I "%_TASK%"=="Inspect" (
    call :F_Run_Tasks Clean Tools Restore Build Inspect
) else if /I "%_TASK%"=="Test" (
    call :F_Run_Tasks Clean Tools Restore Build Inspect Test
) else if /I "%_TASK%"=="Pack" (
    call :F_Run_Tasks Clean Tools Restore Build Inspect Test Pack
) else if /I "%_TASK%"=="Push" (
    call :F_Run_Tasks Clean Tools Restore Build Inspect Test Pack Push
) else if /I "%_TASK%"=="All" (
    call :F_Run_Tasks Clean Tools Restore Build Inspect Test Pack Push
) else (
    echo *** Unknown task '%_TASK%'
)

popd
exit /B %ERRORLEVEL%

:: RUN TASKS

:F_Run_Tasks
set _LOGS_DIR=logs
mkdir "%~dp0%_LOGS_DIR%" >nul 2>&1
set _LOGFILE="%~dp0%_LOGS_DIR%\build.log"
if exist %_LOGFILE% del %_LOGFILE%
call :F_Timestamp

:L_Run_Tasks_Loop
if "%1"=="" exit /B 0
if "%_VS%" == "" ( call :T_%1 ) else ( call :T_VS_%1 )
if errorlevel 1 exit /B %ERRORLEVEL%
shift
goto :L_Run_Tasks_Loop

:: TASKS

:T_Clean
:T_VS_Clean
call :F_Label Clean output directories
if exist "%~dp0%_ARTIFACTS_DIRECTORY%\" call :F_Exec rmdir /S /Q "%~dp0%_ARTIFACTS_DIRECTORY%"
if exist "%~dp0.vs\" call :F_Exec rmdir /S /Q "%~dp0.vs"
if exist "%~dp0_ReSharper.Caches\" call :F_Exec rmdir /S /Q "%~dp0_ReSharper.Caches"
for /F "tokens=*" %%G in ('dir /B /AD /S bin 2^>nul ^& dir /B /AD /S obj 2^>nul') do call :F_Exec rmdir /S /Q "%%G"
exit /B %ERRORLEVEL%

:T_Tools
call :F_Label Restore .NET CLI tools
call :F_Exec dotnet tool restore
exit /B %ERRORLEVEL%

:T_VS_Tools
:: No .NET CLI = no tools to restore
exit /B 0

:T_Inspect
if %_SKIP_INSPECT% gtr 0 (
    call :F_Label Skip code inspection ^(disabled in configuration^)
    exit /B 0
)
call :F_Label Inspect code with ReSharper tools
call :F_Exec dotnet jb inspectcode "%_SOLUTION_FILE%" --no-build --output=%_LOGS_DIR%\inspect.log --format=Text
exit /B %ERRORLEVEL%

:T_VS_Inspect
call :F_Label Skip code inspection ^(always disabled when using Visual Studio's MSBuild^)
exit /B 0

:T_Restore
call :F_Label Restore dependencies
call :F_Exec dotnet restore --verbosity %_MSBUILD_VERBOSITY% %_MSBUILD_OPTIONS%
exit /B %ERRORLEVEL%

:T_VS_Restore
call :F_Label Restore dependencies
call :F_Exec %_VS_MSBUILD_EXE% -t:restore -v:%_MSBUILD_VERBOSITY% %_MSBUILD_OPTIONS%
exit /B %ERRORLEVEL%

:T_Build
call :F_Label Build solution
call :F_Exec dotnet build -c %_MSBUILD_CONFIGURATION% --verbosity %_MSBUILD_VERBOSITY% --no-restore -maxCpuCount:1 %_MSBUILD_OPTIONS%
exit /B %ERRORLEVEL%

:T_VS_Build
call :F_Label Build solution
call :F_Exec %_VS_MSBUILD_EXE% -t:build -p:Configuration=%_MSBUILD_CONFIGURATION% -v:%_MSBUILD_VERBOSITY% -restore:False -maxCpuCount:1 %_MSBUILD_OPTIONS%
exit /B %ERRORLEVEL%

:T_Test
if %_SKIP_TEST% gtr 0 (
    call :F_Label Skip unit tests ^(disabled in configuration^)
    exit /B 0
)
call :F_Label Run unit tests
call :F_Exec dotnet test -c %_MSBUILD_CONFIGURATION% --verbosity %_MSBUILD_VERBOSITY% --no-build %_MSBUILD_OPTIONS%
exit /B %ERRORLEVEL%

:T_VS_Test
call :F_Label Skipping unit tests ^(always disabled when using Visual Studio's MSBuild^)
exit /B 0

:T_Pack
call :F_Label Prepare for distribution
call :F_Exec dotnet pack -c %_MSBUILD_CONFIGURATION% --verbosity %_MSBUILD_VERBOSITY% --no-build -maxCpuCount:1 %_MSBUILD_OPTIONS%
exit /B %ERRORLEVEL%

:T_VS_Pack
call :F_Label Prepare for distribution
call :F_Exec %_VS_MSBUILD_EXE% -t:pack -p:Configuration=%_MSBUILD_CONFIGURATION% -v:%_MSBUILD_VERBOSITY% -p:NoBuild=true -maxCpuCount:1 %_MSBUILD_OPTIONS%
exit /B %ERRORLEVEL%

:T_Push
if %_SKIP_PUSH% gtr 0 (
    call :F_Label Skipping NuGet push ^(disabled in configuration^)
    exit /B 0
)
if not exist "%~dp0%_NUPKG_DIRECTORY%\*.nupkg" (
    call :F_Label Skipping NuGet push ^(no packages to push^)
    exit /B 0
)
if "%_NUGET_PUSH_SOURCE%" == "" (
    call :F_Error _NUGET_PUSH_SOURCE not configured!
    exit /B 1
)
if "%_NUGET_PUSH_API_KEY%" == "" (
    call :F_Error _NUGET_PUSH_API_KEY not configured!
    exit /B 1
)
call :F_Label Push packages to NuGet server
call :F_Exec pushd "%~dp0%_NUPKG_DIRECTORY%"
for /F "tokens=*" %%G in ('dir /B *.nupkg') do (
    call :F_Exec dotnet nuget push %%G --source %_NUGET_PUSH_SOURCE% --api-key %_NUGET_PUSH_API_KEY% --symbol-source %_NUGET_PUSH_SYMBOL_SOURCE% --symbol-api-key %_NUGET_PUSH_SYMBOL_API_KEY%
    if errorlevel 1 exit /B %ERRORLEVEL%
)
call :F_Exec popd
exit /B 0

:T_VS_Push
call :F_Label Skip NuGet push ^(always disabled when using Visual Studio's MSBuild^)
exit /B 0

:: SUB-ROUTINES

:F_CleanDirectory
echo --- rmdir /S /Q %1 >CON:
echo --- rmdir /S /Q %1 >>%_LOGFILE% 2>&1
rmdir /S /Q %1 >nul 2>&1
exit /B 0

:F_Exec
echo --- %* >CON:
echo --- %* >>%_LOGFILE% 2>&1
%* >>%_LOGFILE% 2>&1
set _EL=%ERRORLEVEL%
echo; >>%_LOGFILE% 2>&1
call :F_Display_Errorlevel %_EL%
exit /B %_EL%

:F_Timestamp
call :F_Timestamp_Core >CON:
call :F_Timestamp_Core >>%_LOGFILE% 2>&1
exit /B 0

:F_Timestamp_Core
echo;
echo ===^>^>^> '%_SOLUTION_NAME%'   %DATE% %TIME%
exit /B 0

:F_Label
call :F_Label_Core %* >CON:
call :F_Label_Core %* >>%_LOGFILE% 2>&1
exit /B 0

:F_Label_Core
echo;
echo ^>^>^> %*
exit /B 0

:F_Error
call :F_Error_Core %* >CON:
call :F_Error_Core %* >>%_LOGFILE% 2>&1
exit /B 0

:F_Error_Core
echo ^*^*^* %*
exit /B 0

:F_Display_Errorlevel
if "%1%"=="0" exit /B 0
call :F_Error ERRORLEVEL = %1
exit /B 0

:F_SetToCurrentDirectoryName
call :F_SetToCurrentDirectoryName_Core %1 "%CD%"
exit /B 0

:F_SetToCurrentDirectoryName_Core
set %1=%~nx2
exit /B 0

:: EOF
