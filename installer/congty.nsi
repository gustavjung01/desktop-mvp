Unicode true
RequestExecutionLevel user

!include "MUI2.nsh"
!include "FileFunc.nsh"
!include "LogicLib.nsh"

!ifndef APP_VERSION
  !error "APP_VERSION is required"
!endif
!ifndef STAGING_DIR
  !error "STAGING_DIR is required"
!endif
!ifndef OUTPUT_DIR
  !error "OUTPUT_DIR is required"
!endif
!ifndef ICON_PATH
  !error "ICON_PATH is required"
!endif

!define PRODUCT_NAME "CONGTY"
!define PRODUCT_PUBLISHER "Hưng Phát"
!define PRODUCT_EXE "CongTy.Desktop.exe"
!define PRODUCT_KEY "Software\CongTy\Desktop"
!define UNINSTALL_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\CONGTY"

Name "${PRODUCT_NAME}"
OutFile "${OUTPUT_DIR}\CONGTY-Setup-${APP_VERSION}.exe"
InstallDir "$LOCALAPPDATA\Programs\CONGTY"
InstallDirRegKey HKCU "${PRODUCT_KEY}" "InstallDir"
Icon "${ICON_PATH}"
UninstallIcon "${ICON_PATH}"
SetOverwrite on
ShowInstDetails show
ShowUninstDetails show

!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_LANGUAGE "Vietnamese"

Var WaitPid

Function .onInit
    ${GetParameters} $R0
    ${GetOptions} $R0 "/WAITPID=" $WaitPid
FunctionEnd

Section "CONGTY" SEC01
    SetRegView 64
    ${If} $WaitPid != ""
        DetailPrint "Đang chờ CONGTY đóng..."
        nsExec::ExecToLog 'powershell.exe -NoProfile -WindowStyle Hidden -Command "Wait-Process -Id $WaitPid -ErrorAction SilentlyContinue"'
    ${EndIf}

    SetOutPath "$INSTDIR"
    File /r "${STAGING_DIR}\*.*"

    WriteUninstaller "$INSTDIR\Uninstall.exe"

    CreateDirectory "$SMPROGRAMS\CONGTY"
    CreateShortCut "$SMPROGRAMS\CONGTY\CONGTY.lnk" "$INSTDIR\${PRODUCT_EXE}" "" "$INSTDIR\${PRODUCT_EXE}" 0
    CreateShortCut "$DESKTOP\CONGTY.lnk" "$INSTDIR\${PRODUCT_EXE}" "" "$INSTDIR\${PRODUCT_EXE}" 0

    WriteRegStr HKCU "${PRODUCT_KEY}" "InstallDir" "$INSTDIR"
    WriteRegStr HKCU "${PRODUCT_KEY}" "Version" "${APP_VERSION}"

    WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayName" "${PRODUCT_NAME}"
    WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayVersion" "${APP_VERSION}"
    WriteRegStr HKCU "${UNINSTALL_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
    WriteRegStr HKCU "${UNINSTALL_KEY}" "InstallLocation" "$INSTDIR"
    WriteRegStr HKCU "${UNINSTALL_KEY}" "DisplayIcon" "$INSTDIR\${PRODUCT_EXE}"
    WriteRegStr HKCU "${UNINSTALL_KEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
    WriteRegStr HKCU "${UNINSTALL_KEY}" "QuietUninstallString" '"$INSTDIR\Uninstall.exe" /S'
    WriteRegDWORD HKCU "${UNINSTALL_KEY}" "NoModify" 1
    WriteRegDWORD HKCU "${UNINSTALL_KEY}" "NoRepair" 1

    ${If} $WaitPid != ""
        Exec '"$INSTDIR\${PRODUCT_EXE}"'
    ${EndIf}
SectionEnd

Section "Uninstall"
    SetRegView 64
    Delete "$DESKTOP\CONGTY.lnk"
    Delete "$SMPROGRAMS\CONGTY\CONGTY.lnk"
    RMDir "$SMPROGRAMS\CONGTY"

    DeleteRegKey HKCU "${PRODUCT_KEY}"
    DeleteRegKey HKCU "${UNINSTALL_KEY}"

    RMDir /r "$INSTDIR"
SectionEnd
