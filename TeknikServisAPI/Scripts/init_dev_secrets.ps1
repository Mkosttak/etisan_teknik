param(
    [string]$ConnectionString,
    [string]$HangfireConnectionString,
    [string]$JwtSecret,
    [string]$AdminSecret,
    [string]$AdminPassword
)

if (-not $ConnectionString) {
    $ConnectionString = Read-Host "Bağlantı dizesi (DefaultConnection)"
}
if (-not $HangfireConnectionString) {
    $HangfireConnectionString = Read-Host "Hangfire bağlantı dizesi (boş geçilebilir)"
}
if (-not $JwtSecret) {
    $JwtSecret = Read-Host "JWT gizli anahtarı (SecretKey)"
}
if (-not $AdminSecret) {
    $AdminSecret = Read-Host "AdminBootstrap gizli anahtarı (Secret)"
}
if (-not $AdminPassword) {
    $AdminPassword = Read-Host "AdminBootstrap parolası (Password)"
}

$projectPath = Join-Path $PSScriptRoot ".."
Push-Location $projectPath
try {
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" $ConnectionString
    if ($HangfireConnectionString) {
        dotnet user-secrets set "ConnectionStrings:HangfireConnection" $HangfireConnectionString
    }
    dotnet user-secrets set "JwtSettings:SecretKey" $JwtSecret
    dotnet user-secrets set "AdminBootstrap:Secret" $AdminSecret
    dotnet user-secrets set "AdminBootstrap:Password" $AdminPassword
    Write-Host "User secrets, TeknikServisAPI için güncellendi."
}
finally {
    Pop-Location
}
