# 일차가 끝날 때마다 실행: Study 문서를 복사·가림하고, 진행 현황 README를 저장소 첫 화면에 올린 뒤 커밋·push한다.
# 사용 예: .\publish.ps1 -Message "D001 B01 완료"
param(
    [Parameter(Mandatory = $true)][string]$Message,
    [string]$StudySource = 'D:\박종부\00.Source\03.OCR리팩토링\OmniSight\.claude\worktrees\omnisight-study-plan-4bc7dd\Study'
)

# git은 경고도 stderr로 내보낸다. Windows PowerShell 5.1에서 'Stop'이면 경고만으로 멈추므로
# 실패 여부는 $LASTEXITCODE로 판단한다.
$ErrorActionPreference = 'Continue'
Set-Location -Path $PSScriptRoot

if (-not (Test-Path $StudySource)) {
    throw "Study source not found: $StudySource"
}

# Study 폴더를 그대로 맞춘다 (원본에서 지운 파일은 여기서도 지운다)
robocopy $StudySource (Join-Path $PSScriptRoot 'Study') /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) {
    throw "robocopy failed: exit code $LASTEXITCODE"
}

# 비공개 이름 가리기. 스크립트가 없거나 실패하면 올리지 않는다.
$sanitizer = Join-Path (Split-Path $PSScriptRoot -Parent) 'publish-sanitize.ps1'
if (-not (Test-Path $sanitizer)) {
    throw "Sanitizer not found: $sanitizer"
}
& $sanitizer -StudyPath (Join-Path $PSScriptRoot 'Study')

# 진행 현황 대시보드(Study/README.md)를 저장소 첫 화면 README로 복사한다. 상대 링크 앞에 Study/를 붙인다.
$utf8Bom = New-Object System.Text.UTF8Encoding($true)
$dashboard = [System.IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Study\README.md'), $utf8Bom)
$dashboard = [regex]::Replace($dashboard, '\]\((?!https?:|#)', '](Study/')
[System.IO.File]::WriteAllText((Join-Path $PSScriptRoot 'README.md'), $dashboard, $utf8Bom)

# 공개 전 마지막 검사: 금지 단어("이"+"직")가 있으면 올리지 않는다.
$banned = ([char]0xC774).ToString() + ([char]0xC9C1).ToString()
$hits = Get-ChildItem -Path (Join-Path $PSScriptRoot 'Study'), (Join-Path $PSScriptRoot 'README.md') -Recurse -File -Include *.md |
    Select-String -SimpleMatch -Pattern $banned
if ($hits -or $Message.Contains($banned)) {
    throw "금지 단어가 남아 있어 올리지 않습니다: $($hits | Select-Object -First 3)"
}

git add -A
git diff --cached --quiet
if ($LASTEXITCODE -eq 0) {
    Write-Host 'Nothing to publish.'
    exit 0
}

git status --short
git commit -q -m $Message
if ($LASTEXITCODE -ne 0) { throw 'git commit failed' }
git push origin main
if ($LASTEXITCODE -ne 0) { throw 'git push failed' }
Write-Host 'Published.'
