@echo off
setlocal
cd /d %~dp0
set "CLOUDTAIL_PROFILE=presentation"
set "HOST=127.0.0.1"
set "PORT=8020"
if exist ".venv\Scripts\python.exe" (set "PY=.venv\Scripts\python.exe") else (set "PY=python")
echo [Cloudtail] PROFILE=%CLOUDTAIL_PROFILE%  %HOST%:%PORT%
"%PY%" -m uvicorn --app-dir . cloudtail_backend.main:app --host %HOST% --port %PORT% --reload
