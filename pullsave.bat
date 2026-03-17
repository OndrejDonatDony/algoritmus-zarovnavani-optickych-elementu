@echo off
cd /d "%~dp0"

git status --porcelain >nul 2>&1
if errorlevel 1 (
  echo Git not available or not a git repo here.
  pause
  exit /b 1
)

REM If there are local changes, stash them first
git diff --quiet
if errorlevel 1 (
  echo Local changes detected - stashing...
  git stash push -u -m "autostash before pull"
  set DIDSTASH=1
) else (
  set DIDSTASH=0
)

echo Pulling from origin...
git pull --rebase

if "%DIDSTASH%"=="1" (
  echo Restoring stashed changes...
  git stash pop
)

echo Done.
pause