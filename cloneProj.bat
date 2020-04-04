@echo off

set srcdir=Sync
set postfix=_clone
set dstdir=%srcdir%%postfix%

if exist %dstdir% (

	echo %dstdir% already exists, return.

) else (

	md %dstdir%
	echo Copying...
	xcopy %srcdir%\*.* %dstdir%\*.* /S /A /Q /EXCLUDE:exclude.txt
	mklink /d %~dp0%dstdir%\Assets %~dp0%srcdir%\Assets
	mklink /d %~dp0%dstdir%\ProjectSettings %~dp0%srcdir%\ProjectSettings
	echo clone %srcdir% to %dstdir% finished.
)