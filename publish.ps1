# Kata를 하나 통과할 때마다 실행: Study 문서를 복사하고 커밋한 뒤 GitHub에 push한다.
# 사용 예: .\publish.ps1 -Message "기초 트랙 B03 완료"
param(
    [Parameter(Mandatory = $true)][string]$Message,
    [string]$StudySource = 'D:\박종부\00.Source\03.OCR리팩토링\OmniSight\.claude\worktrees\omnisight-study-plan-4bc7dd\Study'
)

$ErrorActionPreference = 'Stop'
Set-Location -Path $PSScriptRoot

if (-not (Test-Path $StudySource)) {
    throw "Study source not found: $StudySource"
}

# Study 폴더를 그대로 맞춘다 (원본에서 지운 파일은 여기서도 지운다)
robocopy $StudySource (Join-Path $PSScriptRoot 'Study') /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) {
    throw "robocopy failed: exit code $LASTEXITCODE"
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
