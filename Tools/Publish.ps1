[CmdletBinding()]
param (
    [Parameter()]
    [switch]$Compress
)

$currentDir = Push-Location -Path ([System.IO.Path]::Combine($PSScriptRoot, '..')) -PassThru

Remove-Item -Path ([System.IO.Path]::Combine($currentDir, 'out\Release\win-x64')) -Recurse -Force -ErrorAction SilentlyContinue
$outDirInfo = [System.IO.Directory]::CreateDirectory([System.IO.Path]::Combine($currentDir, 'out\Release\win-x64\Artifacts'))

$publishSplat = @{
    FilePath = 'dotnet'
    ArgumentList = "publish .\ManagedStrings.csproj -c release -r win-x64 -o $($outDirInfo.FullName) /p:PublishSingleFile=true /p:LinkDuringPublish=true /p:ShowLinkerSizeComparison=true /p:PublishTrimmed=true"
    NoNewWindow = [switch]::Present
    PassThru = [switch]::Present
}

$publishProcess = Start-Process @publishSplat

# For some reason using the '-Wait' switch on Start-Process hangs after the publish finishes.
$publishProcess.WaitForExit()

if ($publishProcess.ExitCode -ne 0) {
    Write-Error -Message "Publish process returned non-success exit code '$($publishProcess.ExitCode)'."
    Pop-Location
    
    return
}

Copy-Item -Path .\LICENSE -Destination $outDirInfo.FullName

if ($Compress) {
    Compress-Archive -Path "$($outDirInfo.FullName)\*" -DestinationPath "$currentDir\ManagedStrings.zip" -Force
    Remove-Item -Path "$currentDir\out" -Recurse -Force
}

Pop-Location