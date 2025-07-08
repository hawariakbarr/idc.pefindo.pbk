# set up exclusions and output file
$Excludes = 'bin','obj'
$Out = 'projectstructure.txt'
# reset file
".\" | Out-File $Out

function Write-Tree {
    param(
        [string]$Path,
        [string]$Indent
    )
    Get-ChildItem -LiteralPath $Path | Where-Object { 
        # skip excluded dirs
        -not ($_.PSIsContainer -and $Excludes -contains $_.Name)
    } | ForEach-Object {
        if ($_.PSIsContainer) {
            # print folder
            "$Indent├── $($_.Name)" | Out-File $Out -Append
            # recurse with extra indent
            Write-Tree $_.FullName ("$Indent│   ")
        }
        else {
            # print file
            "$Indent└── $($_.Name)" | Out-File $Out -Append
        }
    }
}

# kick it off
Write-Tree (Get-Location).Path ""
