@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
call "%SCRIPT_DIR%casa_da_mulher.cmd" equipe
exit /b %ERRORLEVEL%
