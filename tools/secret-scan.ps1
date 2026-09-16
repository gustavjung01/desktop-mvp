$ErrorActionPreference = "Stop"

$patterns = @(
    'vcp_[A-Za-z0-9]{20,}',
    'gh[pousr]_[A-Za-z0-9]{20,}',
    'sb_secret_[A-Za-z0-9_-]{20,}',
    'os_v2_app_[A-Za-z0-9_-]{20,}',
    'cfat_[A-Za-z0-9_-]{20,}',
    'cfut_[A-Za-z0-9_-]{20,}',
    'HRKU-[A-Za-z0-9_-]{20,}',
    '-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----',
    '(?i)postgres(?:ql)?://[^:\s/]+:[^@\s/]+@'
)

$files = Get-ChildItem -Path . -Recurse -File |
    Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj|\.git)[\\/]' -and
        $_.FullName -notlike '*tools\secret-scan.ps1' -and
        $_.Extension -notin @('.png', '.jpg', '.jpeg', '.gif', '.ico', '.dll', '.exe', '.pdb')
    }

$findings = New-Object System.Collections.Generic.List[string]

foreach ($file in $files) {
    $content = Get-Content -LiteralPath $file.FullName -Raw -ErrorAction SilentlyContinue
    if ($null -eq $content) {
        continue
    }

    foreach ($pattern in $patterns) {
        if ($content -match $pattern) {
            $relative = [System.IO.Path]::GetRelativePath((Get-Location).Path, $file.FullName)
            $findings.Add("$relative matched secret pattern: $pattern")
        }
    }
}

if ($findings.Count -gt 0) {
    Write-Error ("Secret scan failed:`n" + ($findings -join "`n"))
}

Write-Host "Secret scan PASS"
