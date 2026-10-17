param([Parameter(Mandatory)][int]$AppPid)
$ErrorActionPreference = 'Stop'
$env:WINAPP_UI_WORKFLOW_ID = 'fieldsync-home-smoke'
$artifact = Join-Path $PSScriptRoot 'obj'
New-Item -ItemType Directory -Force -Path $artifact | Out-Null
$results = [Collections.Generic.List[object]]::new()
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class HomeWindowProbe {
 [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr window);
 [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr window);
 [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr window,uint message,UIntPtr wParam,IntPtr lParam);
}
'@
function UI([string[]]$Arguments) {
    $output = & winapp ui @Arguments --on local -a $AppPid
    if ($LASTEXITCODE -ne 0) { throw ($output -join "`n") }
    $output
}
function Check([string]$Name, [scriptblock]$Action) {
    try { & $Action | Out-Null; $results.Add(@{ name=$Name; status='PASS' }) }
    catch { $results.Add(@{ name=$Name; status='FAIL'; detail="$_" }) }
}
function Restore-FromTray {
    # Deliver the same callback used by the Shell on tray activation.
    [HomeWindowProbe]::PostMessage($windowHandle,0x802B,[UIntPtr]43,[IntPtr]0x202) | Out-Null
    Start-Sleep -Milliseconds 350
    if (![HomeWindowProbe]::IsWindowVisible($windowHandle)) { throw 'Tray activation did not restore the window.' }
}
try {
    $windows = UI @('list-windows','--json') | ConvertFrom-Json
    $windowHandle = [IntPtr]($windows | Where-Object ownerHwnd -eq 0 | Select-Object -First 1).hwnd
    Check 'Home content and project dialog' {
        UI @('invoke','HomeNavigation')
        UI @('wait-for','HomeWelcome','-t','2000')
        UI @('wait-for','HomeRecentProjects','-t','2000')
        UI @('screenshot','--output',(Join-Path $artifact 'home-dark.png'))
        UI @('invoke','HomeCreateProject')
        UI @('wait-for','ProjectName','-t','2000')
        UI @('invoke','取消')
    }
    Check 'Light theme and logo' {
        UI @('invoke','ThemeButton')
        UI @('screenshot','--output',(Join-Path $artifact 'home-light.png'))
        UI @('invoke','ThemeButton')
    }
    Check 'Minimize keeps process alive' {
        UI @('invoke','DeviceMinimize')
        Start-Sleep -Milliseconds 350
        if (!(Get-Process -Id $AppPid -ErrorAction SilentlyContinue) -or ![HomeWindowProbe]::IsIconic($windowHandle)) { throw 'Minimize failed.' }
        Restore-FromTray
    }
    Check 'Close hides window and keeps process alive' {
        UI @('invoke','DeviceClose')
        Start-Sleep -Milliseconds 350
        if (!(Get-Process -Id $AppPid -ErrorAction SilentlyContinue) -or [HomeWindowProbe]::IsWindowVisible($windowHandle)) { throw 'Close did not hide the live window.' }
        Restore-FromTray
    }
    Check 'System close also runs in background' {
        [HomeWindowProbe]::PostMessage($windowHandle,0x10,[UIntPtr]0,[IntPtr]0) | Out-Null
        Start-Sleep -Milliseconds 350
        if ([HomeWindowProbe]::IsWindowVisible($windowHandle)) { throw 'System close was not intercepted.' }
        Restore-FromTray
    }
    Check 'Tray menu' {
        [HomeWindowProbe]::PostMessage($windowHandle,0x802B,[UIntPtr]43,[IntPtr]0x205) | Out-Null
        UI @('wait-for','打开 FieldSync','-t','2000')
        UI @('screenshot','--output',(Join-Path $artifact 'tray-menu.png'))
        UI @('send-keys','escape','--via','send-input')
    }
}
finally {
    $results | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $artifact 'home-results.json')
    $results | ConvertTo-Json -Depth 4
    winapp ui yield --on local | Out-Null
}
if ($results.status -contains 'FAIL') { exit 1 }
