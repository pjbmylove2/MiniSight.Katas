# Kata를 하나 통과할 때마다 실행: Study 문서를 복사하고 커밋한 뒤 GitHub에 push한다.
# 사용 예: .\publish.ps1 -Message "기초 트랙 B03 완료"
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
