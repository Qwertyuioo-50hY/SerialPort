param([Parameter(Mandatory)][int]$AppPid)
$ErrorActionPreference = 'Stop'
$env:WINAPP_UI_WORKFLOW_ID = 'tomography-settings-smoke'
$artifact = Join-Path $PSScriptRoot 'obj'
New-Item -ItemType Directory -Force -Path $artifact | Out-Null
$results = [Collections.Generic.List[object]]::new()
function UI([string[]]$Arguments) {
    $output = & winapp ui @Arguments --on local -a $AppPid
    if ($LASTEXITCODE -ne 0) { throw ($output -join "`n") }
    $output
}
function Check([string]$Name, [scriptblock]$Action) {
    try { & $Action | Out-Null; $results.Add(@{ name=$Name; status='PASS' }) }
    catch { $results.Add(@{ name=$Name; status='FAIL'; detail="$_" }) }
}
try {
    Check 'Startup and project navigation' {
        UI @('wait-for','CreateProject','-t','3000')
        UI @('wait-for','ChooseProjectDirectory','-t','3000')
        UI @('wait-for','NewAiConversation','-t','3000')
        $tree = UI @('inspect','--json','--depth','8') | ConvertFrom-Json
        function Check-Nodes($nodes) {
            foreach ($node in $nodes) {
                if ($node.automationId -in @('SearchProjectDirectory','ProjectDirectory')) { throw 'Removed navigation controls are still present.' }
                Check-Nodes $node.children
            }
        }
        foreach ($window in $tree.windows) { Check-Nodes $window.elements }
    }
    Check 'Settings AI category' {
        UI @('invoke','SettingsNavigation')
        UI @('wait-for','SettingsSearch','-t','3000')
        UI @('invoke','SettingsAI')
        UI @('wait-for','AiBaseUrl','-t','3000')
        UI @('screenshot','--output',(Join-Path $artifact 'settings-ai.png'))
    }
    foreach ($category in @('SettingsGeneral','SettingsAppearance','SettingsShortcuts','SettingsLanguageRegion')) {
        Check $category {
            UI @('invoke',$category)
            UI @('wait-for','暂无可配置项','-t','2000')
        }
    }
    Check 'Search category' {
        UI @('click','SettingsSearch')
        UI @('send-keys','ctrl+a','--via','send-input')
        UI @('send-keys','AI','--verbatim','--via','send-input')
        UI @('wait-for','AiBaseUrl','-t','2000')
        UI @('send-keys','ctrl+a','--via','send-input')
        UI @('send-keys','zzzz','--verbatim','--via','send-input')
        UI @('wait-for','未找到设置分类','-t','2000')
        UI @('send-keys','ctrl+a','--via','send-input')
        UI @('send-keys','backspace','--via','send-input')
        UI @('invoke','SettingsAI')
    }
    Check 'Create project dialog' {
        UI @('invoke','CreateProject')
        UI @('wait-for','ProjectName','-t','2000')
        UI @('screenshot','--output',(Join-Path $artifact 'new-project.png'))
        UI @('invoke','取消')
        UI @('wait-for','ProjectName','--gone','-t','2000')
    }
    Check 'AI page remains available' {
        UI @('invoke','AiNavigation')
        UI @('wait-for','ChatInput','-t','2000')
        UI @('screenshot','--output',(Join-Path $artifact 'chat.png'))
    }
}
finally {
    $results | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $artifact 'results.json')
    $results | ConvertTo-Json -Depth 4
    winapp ui yield --on local | Out-Null
}
if ($results.status -contains 'FAIL') { exit 1 }
