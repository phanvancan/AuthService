param(
    [string]$RootPath = "e:\toolCode\AuthService"
)

$ErrorActionPreference = "Stop"

function Wait-Port {
    param(
        [int]$Port,
        [int]$TimeoutSeconds = 60
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $ok = Test-NetConnection -ComputerName localhost -Port $Port -InformationLevel Quiet -WarningAction SilentlyContinue
        if ($ok) { return $true }
        Start-Sleep -Milliseconds 500
    }

    return $false
}

function Get-HeaderValue {
    param(
        [string[]]$Headers,
        [string]$Name
    )

    $pattern = "(?i)^" + [regex]::Escape($Name) + ":"
    $line = $Headers | Select-String -Pattern $pattern
    if ($line) {
        return $line.ToString().Substring($Name.Length + 1).Trim()
    }

    return ""
}

$authProcess = $null
$app1Process = $null
$app2Process = $null

$cookieFile = Join-Path $RootPath "sso-cookies-regression.txt"
if (Test-Path $cookieFile) {
    Remove-Item $cookieFile -Force
}

try {
    Write-Host "[1/6] Starting services..."

    $authProcess = Start-Process dotnet -ArgumentList "run" -WorkingDirectory (Join-Path $RootPath "AuthService") -PassThru -WindowStyle Hidden
    $app1Process = Start-Process dotnet -ArgumentList "run --launch-profile https" -WorkingDirectory (Join-Path $RootPath "Application1") -PassThru -WindowStyle Hidden
    $app2Process = Start-Process dotnet -ArgumentList "run --launch-profile https" -WorkingDirectory (Join-Path $RootPath "Application2") -PassThru -WindowStyle Hidden

    Write-Host "[2/6] Waiting for ports 7250 / 7002 / 7281..."
    if (-not (Wait-Port -Port 7250)) { throw "AuthService did not start on 7250." }
    if (-not (Wait-Port -Port 7002)) { throw "Application1 did not start on 7002." }
    if (-not (Wait-Port -Port 7281)) { throw "Application2 did not start on 7281." }

    Write-Host "[3/6] Testing initial redirect from App1..."
    $h1 = curl.exe -k -s -D - -o NUL "https://localhost:7002/dashboard"
    $step1Redirect = Get-HeaderValue -Headers $h1 -Name "Location"
    $step1Pass = $step1Redirect.Contains("https://localhost:7250/sso/login")

    Write-Host "[4/6] Performing SSO login + cookie set..."
    $returnUrlEncoded = [uri]::EscapeDataString("https://localhost:7002/dashboard")
    $body = "username=admin&password=Admin@123&returnUrl=$returnUrlEncoded&app=Application1"
    $hLogin = curl.exe -k -s -L -D - -o NUL -c $cookieFile -b $cookieFile -H "Content-Type: application/x-www-form-urlencoded" -d $body "https://localhost:7250/sso/login"
    $loginSetAccess = ($hLogin | Select-String -Pattern "(?i)^Set-Cookie:\s*access_token=").Count -gt 0
    $loginSetRefresh = ($hLogin | Select-String -Pattern "(?i)^Set-Cookie:\s*refresh_token=").Count -gt 0

    Write-Host "[5/6] Testing refresh-token-cookie endpoint..."
    $hRefresh = curl.exe -k -s -D - -o NUL -c $cookieFile -b $cookieFile -X POST "https://localhost:7250/api/auth/refresh-token-cookie"
    $refreshStatus = ($hRefresh | Select-String -Pattern "^HTTP/" | Select-Object -Last 1).ToString()
    $refreshSetAccess = ($hRefresh | Select-String -Pattern "(?i)^Set-Cookie:\s*access_token=").Count -gt 0
    $refreshSetRefresh = ($hRefresh | Select-String -Pattern "(?i)^Set-Cookie:\s*refresh_token=").Count -gt 0

    Write-Host "[6/6] Testing App2 access before/after logout..."
    $hApp2Before = curl.exe -k -s -D - -o NUL -c $cookieFile -b $cookieFile "https://localhost:7281/dashboard"
    $app2BeforeStatus = ($hApp2Before | Select-String -Pattern "^HTTP/" | Select-Object -Last 1).ToString()

    $logoutReturnUrl = [uri]::EscapeDataString("https://localhost:7002/")
    $hLogout = curl.exe -k -s -L -D - -o NUL -c $cookieFile -b $cookieFile "https://localhost:7250/sso/logout?returnUrl=$logoutReturnUrl"
    $logoutClearAccess = ($hLogout | Select-String -Pattern "(?i)^Set-Cookie:\s*access_token=.*expires=").Count -gt 0
    $logoutClearRefresh = ($hLogout | Select-String -Pattern "(?i)^Set-Cookie:\s*refresh_token=.*expires=").Count -gt 0

    $hApp2After = curl.exe -k -s -D - -o NUL -c $cookieFile -b $cookieFile "https://localhost:7281/dashboard"
    $app2AfterStatus = ($hApp2After | Select-String -Pattern "^HTTP/" | Select-Object -Last 1).ToString()
    $app2AfterLocation = Get-HeaderValue -Headers $hApp2After -Name "Location"
    $app2AfterRedirectAuth = $app2AfterLocation.Contains("https://localhost:7250/sso/login")

    $results = [ordered]@{
        "Step1 Redirect To Auth" = $step1Pass
        "Login Set access_token" = $loginSetAccess
        "Login Set refresh_token" = $loginSetRefresh
        "Refresh Endpoint 200" = ($refreshStatus -like "*200 OK")
        "Refresh Set access_token" = $refreshSetAccess
        "Refresh Set refresh_token" = $refreshSetRefresh
        "App2 Before Logout 200" = ($app2BeforeStatus -like "*200 OK")
        "Logout Clears access_token" = $logoutClearAccess
        "Logout Clears refresh_token" = $logoutClearRefresh
        "App2 After Logout Redirect" = ($app2AfterStatus -like "*302 Found")
        "Redirect Target Is Auth" = $app2AfterRedirectAuth
    }

    Write-Host ""
    Write-Host "=== SSO REGRESSION RESULT ==="
    foreach ($item in $results.GetEnumerator()) {
        $status = if ($item.Value) { "PASS" } else { "FAIL" }
        Write-Host ("{0,-32}: {1}" -f $item.Key, $status)
    }

    $allPass = -not ($results.Values -contains $false)
    Write-Host ""
    if ($allPass) {
        Write-Host "OVERALL: PASS"
        exit 0
    }

    Write-Host "OVERALL: FAIL"
    exit 1
}
finally {
    foreach ($proc in @($authProcess, $app1Process, $app2Process)) {
        if ($null -ne $proc) {
            try {
                if (-not $proc.HasExited) {
                    Stop-Process -Id $proc.Id -Force
                }
            }
            catch {
            }
        }
    }
}
