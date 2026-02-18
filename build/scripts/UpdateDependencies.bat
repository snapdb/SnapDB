::*******************************************************************************************************
::  UpdateDependencies.bat - Gbtc
::
::  Copyright © 2013, Grid Protection Alliance.  All Rights Reserved.
::
::  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
::  the NOTICE file distributed with this work for additional information regarding copyright ownership.
::  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
::  not use this file except in compliance with the License. You may obtain a copy of the License at:
::
::      http://www.opensource.org/licenses/eclipse-1.0.php
::
::  Unless agreed to in writing, the subject software distributed under the License is distributed on an
::  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
::  License for the specific language governing permissions and limitations.
::
::  Code Modification History:
::  -----------------------------------------------------------------------------------------------------
::  08/16/2015 - Stephen C. Wills
::       Generated original version of source code.
::
::*******************************************************************************************************

@ECHO OFF

SETLOCAL

SET pwd=%CD%
IF "%git%" == "" SET git=%PROGRAMFILES(X86)%\Git\cmd\git.exe

SET defaulttarget=%LOCALAPPDATA%\Temp\SnapDB
IF "%remote%" == "" SET remote=git@github.com:snapdb/SnapDB.git
IF "%target%" == "" SET target=%defaulttarget%

ECHO.
ECHO Entering working directory...
IF EXIST "%target%" IF NOT EXIST "%target%"\.git RMDIR /S /Q "%target%"
IF NOT EXIST "%target%" MKDIR "%target%"
CD /D %target%

IF NOT EXIST .git GOTO CloneRepository
IF NOT "%target%" == "%defaulttarget%" GOTO UpdateDependencies
GOTO UpdateRepository

:CloneRepository
ECHO.
ECHO Getting latest version...
"%git%" clone "%remote%" .
GOTO UpdateDependencies

:UpdateRepository
ECHO.
ECHO Updating to latest version...
"%git%" fetch
"%git%" reset --hard origin/main
"%git%" clean -f -d -x
GOTO UpdateDependencies

:UpdateDependencies

REM Gemstone package versions are controlled by the Gemstone.Common.csproj file, we set the
REM Directory.Build.props file to reference the package version parsed from the previously
REM updated Gemstone.Common.csproj file. All projects in the solution use the GemstoneVersion
REM MSBuild property defined in Directory.Build.props to stay in sync:
ECHO.
ECHO Updating Gemstone package versions...
SET scriptdir=%~dp0
SET gemstonecsproj=%scriptdir%..\..\..\gemstone\common\src\Gemstone\Gemstone.Common.csproj
SET targetprops=%target%\src\Directory.Build.props
PowerShell -ExecutionPolicy Bypass -File "%scriptdir%update-gemstone-packages.ps1" -SourceCsprojPath "%gemstonecsproj%" -TargetPropsPath "%targetprops%"

:CommitChanges
ECHO.
ECHO Committing updates to local repository...
"%git%" add .
"%git%" commit -m "Updated Gemstone package versions."
IF NOT "%donotpush%" == "" GOTO Finish

:PushChanges
ECHO.
ECHO Pushing changes to remote repository...
"%git%" push
CD /D %pwd%

:Finish
ECHO.
ECHO Update complete