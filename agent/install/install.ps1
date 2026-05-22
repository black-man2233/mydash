# MyDash Agent installer for Windows (x64)
# Usage: irm https://mydash.example.com/agent/install.ps1 | iex -- --hub <URL> --token <TOKEN> --name <NAME>
param(
    [Parameter(Mandatory)] [string] $Hub,
    [Parameter(Mandatory)] [string] $Token,
    [string] $Name    = $env:COMPUTERNAME,
    [string] $Version = "latest"
)

$InstallDir = "$env:ProgramFiles\MyDash"
$BinaryUrl  = "https://github.com/user/mydash/releases/$Version/download/mydash-agent-win-x64.exe"

Write-Host "[install] Downloading MyDash Agent..."
New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
Invoke-WebRequest -Uri $BinaryUrl -OutFile "$InstallDir\mydash-agent.exe" -UseBasicParsing

Write-Host "[install] Registering Windows Service..."
$svc = Get-Service -Name "mydash-agent" -ErrorAction SilentlyContinue
if ($svc) {
    Stop-Service "mydash-agent" -ErrorAction SilentlyContinue
    sc.exe delete "mydash-agent" | Out-Null
}

[System.Environment]::SetEnvironmentVariable("MYDASH_HUB",   $Hub,   "Machine")
[System.Environment]::SetEnvironmentVariable("MYDASH_TOKEN", $Token, "Machine")
[System.Environment]::SetEnvironmentVariable("MYDASH_NAME",  $Name,  "Machine")

sc.exe create "mydash-agent" `
    binpath= "`"$InstallDir\mydash-agent.exe`"" `
    start= auto `
    displayname= "MyDash Agent"

sc.exe start "mydash-agent"

Write-Host "[install] Done! Service started."
Write-Host "          Check status: sc.exe query mydash-agent"
