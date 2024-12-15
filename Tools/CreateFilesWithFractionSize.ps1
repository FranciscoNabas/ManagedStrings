<#
    Creates lots of files with a fraction of the size of the input file.
    Usefull to benchmark different file sizes.
#>

[CmdletBinding()]
param (
    [Parameter(
        Mandatory,
        Position = 0
    )]
    [ValidateNotNullOrEmpty()]
    [string]$OriginFile,

    [Parameter(
        Mandatory,
        Position = 1
    )]
    [ValidateNotNullOrEmpty()]
    [string]$Destination
)

if (![System.IO.Path]::Exists($OriginFile)) {
    throw [System.IO.FileNotFoundException]::new("Input file doesn't exist dumbass!")
}

if (![System.IO.Directory]::Exists($Destination)) {
    throw [System.IO.FileNotFoundException]::new("Destination directory doesn't exist dumbass!")
}

$originFileInfo = [System.IO.FileInfo]::new($OriginFile)
if (!$originFileInfo.Exists) {
    throw [System.IO.FileNotFoundException]::new("Origin file doesn't exist dumbass!")
}

$destinationInfo = [System.IO.Directory]::CreateDirectory($Destination)
$sizes = @(
    $originFileInfo.Length,
    ($originFileInfo.Length * 0.80),
    ($originFileInfo.Length * 0.60),
    ($originFileInfo.Length * 0.40),
    ($originFileInfo.Length * 0.30),
    ($originFileInfo.Length * 0.25),
    ($originFileInfo.Length * 0.20),
    ($originFileInfo.Length * 0.15),
    ($originFileInfo.Length * 0.10),
    ($originFileInfo.Length * 0.05),
    ($originFileInfo.Length * 0.025),
    ($originFileInfo.Length * 0.0125),
    ($originFileInfo.Length * 0.00625),
    ($originFileInfo.Length * 0.003125),
    ($originFileInfo.Length * 0.0015),
    ($originFileInfo.Length * 0.001),
    ($originFileInfo.Length * 0.0005)
)

$originStream = [System.IO.File]::Open($originFileInfo.FullName, 'Open', 'Read', 'Read')
$originReader = [System.IO.BinaryReader]::new($originStream)
try {
    foreach ($size in $sizes) {
        $destinationName = "$($originFileInfo.BaseName)-[$('0x{0:X}' -f ([int][Math]::Round(($size / 1Kb), 0)))-KB]$($originFileInfo.Extension)"
        $destinationFile = [System.IO.Path]::Combine($destinationInfo.FullName, $destinationName)
        $destinationStream = [System.IO.File]::Open($destinationFile, 'Create', 'ReadWrite', 'Read')
        $destinationWriter = [System.IO.BinaryWriter]::new($destinationStream)
        try {
            Write-Host "Writing '$('0x{0:X}' -f ([int][math]::Round($size, 0)))' bytes into '$destinationName'." -ForegroundColor DarkGreen
            $buffer = [byte[]]::new($size)
            [void]$originReader.Read($buffer, 0, $size)
            $destinationWriter.Write($buffer, 0, $size)

            $originStream.Position = 0
        }
        finally {
            $destinationWriter.Dispose()
            $destinationStream.Dispose()
        }
    }
}
finally {
    $originReader.Dispose()
    $originStream.Dispose()
}