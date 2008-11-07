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
REM This is a batch file that can used to build ELMAH for Microsoft .NET 
REM Framework 1.x and 2.0. The build is created for only those versions
REM that are found to be installed in the expected locations (see below).
REM
REM To compile for Microsoft .NET Framework 1.0, you must have Microsoft
REM Visual Studio .NET 2002 installed in the standard path proposed by
REM by its installer.
REM
REM To compile for Microsoft .NET Framework 1.1, you must have Microsoft
REM Visual Studio .NET 2003 installed in the standard path proposed by
REM by its installer.
REM
REM To compile for Microsoft .NET Framework 2.0, 3.0 and 3.5, you only
REM need MSBUILD.EXE and which is expected to be located in the standard
REM installation directory.

setlocal
pushd "%~dp0"
set DEVENV70EXE=%ProgramFiles%\Microsoft Visual Studio .NET\Common7\IDE\devenv.com
set DEVENV71EXE=%ProgramFiles%\Microsoft Visual Studio .NET 2003\Common7\IDE\devenv.com
set MSBUILD20EXE=%windir%\Microsoft.NET\Framework\v2.0.50727\msbuild.exe
set MSBUILD35EXE=%windir%\Microsoft.NET\Framework\v3.5\msbuild.exe
for %%i in (debug release) do if exist "%DEVENV70EXE%"  "%DEVENV70EXE%"  2002\Elmah.sln /build %%i
for %%i in (debug release) do if exist "%DEVENV71EXE%"  "%DEVENV71EXE%"  2003\Elmah.sln /build %%i
for %%i in (debug release) do if exist "%MSBUILD20EXE%" "%MSBUILD20EXE%" 2005\Elmah.sln /p:Configuration=%%i
for %%i in (debug release) do if exist "%MSBUILD35EXE%" "%MSBUILD35EXE%" 2008\Elmah.sln /p:Configuration=%%i
popd
