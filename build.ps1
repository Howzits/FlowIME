param([string]$NodePath = 'node')
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
$metadata = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'package.json') -Raw | ConvertFrom-Json
if ($metadata.publisher -cne 'flowime') { throw 'Publisher must be flowime.' }
$packager = Join-Path $PSScriptRoot 'node_modules\@vscode\vsce\vsce'
if (!(Test-Path -LiteralPath $packager)) { throw 'Run npm ci --ignore-scripts first.' }
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$bin = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $bin -Force | Out-Null
$exe = Join-Path $bin 'InputHelper.exe'
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'native') -Filter '*.cs' | ForEach-Object FullName)
& (Join-Path $framework 'csc.exe') /nologo /target:exe /optimize+ "/out:$exe" "/reference:$framework\WPF\UIAutomationClient.dll" "/reference:$framework\WPF\UIAutomationTypes.dll" @sources
if ($LASTEXITCODE -ne 0) { throw 'Helper compilation failed' }
& $exe --self-test
if ($LASTEXITCODE -ne 0) { throw 'Helper self-test failed' }
& $NodePath --test (Join-Path $PSScriptRoot 'test\extension.test.js') | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Extension tests failed' }
$destination = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$artifact = Join-Path $destination "FlowIME-$($metadata.version)-win32-x64.vsix"
& $NodePath $packager package --no-dependencies --target win32-x64 --out $artifact | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Official VSIX packaging failed' }
python (Join-Path $PSScriptRoot 'scripts\verify_package.py') --vsix $artifact
if ($LASTEXITCODE -ne 0) { throw 'VSIX verification failed' }
python -m unittest discover -s test -p '*_test.py'
if ($LASTEXITCODE -ne 0) { throw 'Release guard tests failed' }
$digest = (Get-FileHash -LiteralPath $artifact -Algorithm SHA256).Hash.ToLowerInvariant()
$checksum = Join-Path $destination 'SHA256SUMS.txt'
[IO.File]::WriteAllText($checksum, "$digest  $([IO.Path]::GetFileName($artifact))`n", [Text.Encoding]::ASCII)
Write-Host "Marketplace and GitHub Release use the same file: $artifact"
} finally { Pop-Location }
