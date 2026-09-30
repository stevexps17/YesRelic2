param([string]$GamePath = 'C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2')
$ErrorActionPreference = 'Stop'
$gameBin = Join-Path $GamePath 'data_sts2_windows_x86_64'
$compiler = Join-Path $env:ProgramFiles 'dotnet\sdk\6.0.410\Roslyn\bincore\csc.dll'
if (!(Test-Path -LiteralPath $compiler)) { throw 'Requires .NET SDK 6.0.410, or update the compiler path in build.ps1.' }
$dest = Join-Path $PSScriptRoot '..\YesRelic2.dll'
$refs = @('System.Private.CoreLib','System.Runtime','System.Collections','System.Linq','System.Console','System.Threading','System.Threading.Tasks','System.IO','System.IO.FileSystem','System.Security.Cryptography','System.Runtime.Extensions','System.Reflection','System.Text.Json','System.Memory','System.ObjectModel','GodotSharp','sts2','0Harmony')
$compilerArgs = @('-nologo','-target:library','-langversion:10','-nullable:disable','-nostdlib+','-deterministic+',('-out:'+$dest))
$compilerArgs += $refs | ForEach-Object { '-r:' + (Join-Path $gameBin ($_ + '.dll')) }
$compilerArgs += Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | ForEach-Object FullName
& dotnet $compiler @compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
Write-Output 'Built YesRelic2.dll successfully.'
