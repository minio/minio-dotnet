:: Usage: runsample.cmd <example.cs>

@ECHO off
SETLOCAL ENABLEEXTENSIONS
SET filename=%1
ECHO "setting done: %filename%"

IF NOT DEFINED %filename (
	ECHO "Usage: %0 <example.cs>"
	EXIT /B 1
)

CALL :run %filename%
EXIT /B 0

:run
SET src=..\..\%1
SET execname=%~n1.exe
CD bin\Debug
csc.exe /r:Minio.dll /out:%execname% %src%
COPY /y Minio.Examples.dll.config %execname%.config
%execname%
EXIT /B %ERRORLEVEL%