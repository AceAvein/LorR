@echo off
REM Force DirectX 11 to avoid D3D12 device-removed crashes (887a0005).
set UNITY="%ProgramFiles%\Unity\Hub\Editor\6000.2.5f1\Editor\Unity.exe"
if not exist %UNITY% (
  echo Unity 6000.2.5f1 not found at %UNITY%
  pause
  exit /b 1
)
start "" %UNITY% -force-d3d11 -projectPath "%~dp0"
