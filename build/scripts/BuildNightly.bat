::*******************************************************************************************************
::  BuildNightly.bat - Gbtc
::
::  Tennessee Valley Authority, 2009
::  No copyright is claimed pursuant to 17 USC § 105.  All Other Rights Reserved.
::
::  This software is made freely available under the TVA Open Source Agreement (see below).
::
::  Code Modification History:
::  -----------------------------------------------------------------------------------------------------
::  10/20/2009 - Pinal C. Patel
::       Generated original version of source code.
::  09/14/2010 - Mihir Brahmbhatt
::		 Change Framework path from v3.5 to v4.0
::  10/03/2010 - Pinal C. Patel
::       Updated to use MSBuild 4.0.
::
::*******************************************************************************************************

@ECHO OFF

SetLocal

IF NOT "%1" == "" SET "logFlag=/fl /flp:logfile=%~1;verbosity=diagnostic;encoding=UTF-8;append=false"

ECHO BuildNightly: dotnet msbuild SnapDB.buildproj /p:ForceBuild=false %logflag%
dotnet msbuild SnapDB.buildproj /p:ForceBuild=false %logflag% 

SET "CHANGES_DETECTED=false"

IF EXIST "ChangesDetected.cmd" (
  CALL "ChangesDetected.cmd"
) ELSE (
  ECHO WARNING: ChangesDetected.cmd not found; assuming no changes.
)

IF /I "%CHANGES_DETECTED%"=="true" (
    ECHO Changes detected — publishing Gemstone.SnapDB package...
    CALL PowerShell -NoProfile -ExecutionPolicy ByPass -File ".\publish-packages.ps1" "C:\Users\buildbot\Projects\SnapDB"
)
