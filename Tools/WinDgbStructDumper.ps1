function Add-StructToFile {

    param([string]$Path, [string]$Content)

    try { Add-Content -Path $Path -Value $Content -ErrorAction Stop }
    catch {
        Start-Sleep -Milliseconds 700
        try { Add-Content -Path $Path -Value $Content -ErrorAction Stop }
        catch { throw $_ }
    }
}

$destinationFile = 'C:\Repositories\C#\ManagedStrings\src\Temp\Heap32.cs'
$destinationFileForceSequential = 'C:\Repositories\C#\ManagedStrings\src\Temp\HeapForceSequential32.cs'
$content = Get-Content -Path 'C:\Repositories\C#\ManagedStrings\src\Temp\Heap32.txt' -Raw
$allTextLines = $content.Split("`n")
$heapNameRegex = [regex]::new('(?<=####)(\w*)(?=####)')

$structIndex = 0
for ($i = 0; $i -lt $allTextLines.Length; $i++) {
    if ($i -ge $allTextLines.Length) { break }

    $currentLine = $allTextLines[$i].Trim()
    if ([string]::IsNullOrWhiteSpace($currentLine)) { continue }
    
    if ($heapNameRegex.IsMatch($currentLine)) {
        $currentStructName = $heapNameRegex.Match($currentLine).Value

        $structIndex = $i + 1
        $buffer = [System.Text.StringBuilder]::new()
        do {
            if ($structIndex -ge $allTextLines.Length) { break }
    
            $currentStructLine = $allTextLines[$structIndex]
            if (!$currentStructLine -or [string]::IsNullOrWhiteSpace($currentStructLine)) { break }

            if (![string]::IsNullOrWhiteSpace($currentStructLine)) {
                [void]$buffer.AppendLine($currentStructLine)
                $structIndex++
            }

        } while (![string]::IsNullOrWhiteSpace($currentStructLine))

        $parserSplat = @{
            StructName = $currentStructName
            WinDbgRepresentation = $buffer.ToString()
            StructAccessModifier = 'internal'
            FieldAccessModifier = 'internal'
            Is32Bit = [switch]::Present
        }

        $structText = & C:\Repositories\PowerShell\UnmanagedStructParser3.ps1 @parserSplat
        $structText = [string]::Join('', $structText, "`n")
        Add-StructToFile -Path $destinationFile -Content $structText

        $structText = & C:\Repositories\PowerShell\UnmanagedStructParser3.ps1 @parserSplat -ForceLayoutSequential
        $structText = [string]::Join('', $structText, "`n")
        Add-StructToFile -Path $destinationFileForceSequential -Content $structText

        $i = $structIndex

        continue
    }
    
    throw "Something went wrong at line '$($i + 1)'. Line heah: $currentLine."
}