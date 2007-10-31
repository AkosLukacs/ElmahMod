@echo off

REM ELMAH - Error Logging Modules and Handlers for ASP.NET
REM Copyright (c) 2007 Atif Aziz. All rights reserved.
REM
REM  Author(s):
REM
REM      Atif Aziz, http://www.raboof.com
REM
REM This library is free software; you can redistribute it and/or modify it 
REM under the terms of the New BSD License, a copy of which should have 
REM been delivered along with this distribution.
REM
REM THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 
REM "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
REM LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
REM PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT 
REM OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
REM SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
REM LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, 
REM DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY 
REM THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
REM (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
REM OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
REM
REM -------------------------------------------------------------------------
REM
REM This is a batch script that can used to build ELMAH binaries for 
REM Microsoft .NET Framework 1.x and 2.0. The binaries are created for 
REM only those versions that are found to be installed in the expected 
REM locations on the local machine.
REM
REM - Microsoft .NET Framework 1.0:
REM   %SystemRoot%\Microsoft.NET\Framework\v1.0.3705
REM
REM - Microsoft .NET Framework 1.1:
REM   %SystemRoot%\Microsoft.NET\Framework\v1.1.4322

REM - Microsoft .NET Framework 2.0:
REM   %SystemRoot%\Microsoft.NET\Framework\v2.0.50727

pushd "%~dp0"
setlocal

set NETFX_BASE_PATH=%SystemRoot%\Microsoft.NET\Framework

if "%1"=="" call :help
if "%1"=="all" call :all
if "%1"=="1.0" call :net-1-0
if "%1"=="1.1" call :net-1-1
if "%1"=="2.0" call :net-2-0
popd
goto :EOF

:all
call :net-1-0
call :net-1-1
call :net-2-0
goto :EOF

:net-1-0
call :compile v1.0.3705 net-1.0 /d:NET_1_0
goto :EOF

:net-1-1
call :compile v1.1.4322 net-1.1 /d:NET_1_1
goto :EOF

:net-2-0
call :compile v2.0.50727 net-2.0 /d:NET_2_0 /r:lib\System.Data.SQLite.dll /nowarn:618
echo.
echo Copying dependencies to output directories...
for %%i in (Debug Release) do if exist bin\net-2.0\%%i copy lib\System.Data.SQLite.dll bin\net-2.0\%%i
goto :EOF

:compile
echo.
echo Compiling for Microsoft .NET Framework %1
set CSC_PATH=%NETFX_BASE_PATH%\%1\csc.exe
if not exist "%CSC_PATH%" (
    echo.
    echo WARNING! 
    echo Microsoft .NET Framework %1 does not appear installed on 
    echo this machine. Skipping target!
    goto :EOF
)
set BIN_OUT_DIR=bin\%2
for %%i in (Debug Release) do if not exist %BIN_OUT_DIR%\%%i md %BIN_OUT_DIR%\%%i
echo Compiling DEBUG configuration
echo.
set CSC_FILES=/recurse:src\Elmah\*.cs /res:src\Elmah\ErrorLog.css,Elmah.ErrorLog.css /res:src\Elmah\RemoteAccessError.htm,Elmah.RemoteAccessError.htm 
"%CSC_PATH%" /t:library /out:%BIN_OUT_DIR%\Debug\Elmah.dll   /nowarn:1591 /doc:%BIN_OUT_DIR%\Debug\Elmah.xml   /debug+                   %CSC_FILES% /d:TRACE;DEBUG %3 %4 %5 %6 %7 %8 %9
echo Compiling RELEASE configuration
echo.
"%CSC_PATH%" /t:library /out:%BIN_OUT_DIR%\Release\Elmah.dll /nowarn:1591 /doc:%BIN_OUT_DIR%\Release\Elmah.xml /debug:pdbonly /optimize+ %CSC_FILES% /d:TRACE       %3 %4 %5 %6 %7 %8 %9
goto :EOF

:help
echo Usage: %~n0 TARGET
echo.
echo TARGET
echo     is the target to build (all, 1.0, 1.1, or 2.0)
