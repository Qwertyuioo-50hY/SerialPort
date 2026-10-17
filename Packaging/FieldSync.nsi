Unicode true
!include "MUI2.nsh"
!include "x64.nsh"
!include "WinVer.nsh"
!include "WordFunc.nsh"

Name "FieldSync"
OutFile "${OUTPUT}"
InstallDir "$LOCALAPPDATA\Programs\FieldSync"
InstallDirRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\FieldSync" "InstallLocation"
RequestExecutionLevel user
SetCompressor /SOLID lzma
BrandingText "FieldSync EMT / EIT / ECT"
Icon "..\UI\Assets\AppIcon.ico"
UninstallIcon "..\UI\Assets\AppIcon.ico"
VIProductVersion "${VERSION}.0"
VIAddVersionKey "ProductName" "FieldSync"
VIAddVersionKey "FileDescription" "FieldSync ${ARCH} installer"
VIAddVersionKey "FileVersion" "${VERSION}"
VIAddVersionKey "LegalCopyright" "FieldSync"
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "SimpChinese"
!insertmacro MUI_LANGUAGE "English"

Function .onInit
  ${IfNot} ${AtLeastBuild} 19045
    MessageBox MB_ICONSTOP "FieldSync requires Windows 10 22H2 / Windows 11."
    Abort
  ${EndIf}
!if "${ARCH}" == "arm64"
  ${IfNot} ${IsNativeARM64}
!else
  ${IfNot} ${IsNativeAMD64}
!endif
    MessageBox MB_ICONSTOP "Please download the installer matching your Windows CPU architecture (${ARCH})."
    Abort
  ${EndIf}
  SetRegView 64
  SetShellVarContext current
FunctionEnd

Section "FieldSync"
  ReadRegStr $1 HKLM "Software\Microsoft\VisualStudio\14.0\VC\Runtimes\${ARCH}" "Version"
  StrCpy $2 $1 1
  ${If} $2 == "v"
    StrCpy $1 $1 "" 1
  ${EndIf}
  ${VersionCompare} $1 "${VCVERSION}" $0
  ${If} $0 == 2
  ${OrIf} $1 == ""
    InitPluginsDir
    SetOutPath "$PLUGINSDIR"
    File /oname=vc_redist.exe "${VCREDIST}"
    ClearErrors
    ExecShellWait "runas" "$PLUGINSDIR\vc_redist.exe" "/install /quiet /norestart" SW_HIDE $0
    ${If} ${Errors}
      MessageBox MB_ICONSTOP "Microsoft Visual C++ Runtime installation was not authorized."
      Abort
    ${EndIf}
    ${If} $0 == 3010
      SetRebootFlag true
    ${ElseIf} $0 != 0
    ${AndIf} $0 != 1638
      MessageBox MB_ICONSTOP "Microsoft Visual C++ Runtime installation failed (exit code $0)."
      Abort
    ${EndIf}
  ${EndIf}
  SetOutPath "$INSTDIR"
  File /r /x "tests" /x "__pycache__" "${PAYLOAD}\*.*"
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  CreateDirectory "$SMPROGRAMS\FieldSync"
  CreateShortcut "$SMPROGRAMS\FieldSync\FieldSync.lnk" "$INSTDIR\Tomography.exe"
  CreateShortcut "$DESKTOP\FieldSync.lnk" "$INSTDIR\Tomography.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\FieldSync" "DisplayName" "FieldSync"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\FieldSync" "DisplayVersion" "${VERSION}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\FieldSync" "DisplayIcon" "$INSTDIR\Tomography.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\FieldSync" "UninstallString" '$\"$INSTDIR\Uninstall.exe$\"'
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\FieldSync" "InstallLocation" "$INSTDIR"
SectionEnd

Section "Uninstall"
  SetRegView 64
  SetShellVarContext current
  !include "${UNINSTALL_FILES}"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"
  Delete "$DESKTOP\FieldSync.lnk"
  Delete "$SMPROGRAMS\FieldSync\FieldSync.lnk"
  RMDir "$SMPROGRAMS\FieldSync"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\FieldSync"
SectionEnd
