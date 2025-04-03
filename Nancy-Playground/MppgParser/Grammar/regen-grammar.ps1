Push-Location $PSScriptRoot
try {
    if(Get-Command antlr4 -ErrorAction SilentlyContinue)
    {
        antlr4 -Dlanguage=CSharp -o ./ -package Unipi.MppgParser.Grammar -visitor -no-listener -lib ./ ./Mppg.g4
    }
    else
    {
        Write-Host "ANTLR is not installed. Follow the instructions at https://github.com/antlr/antlr4/blob/master/doc/getting-started.md"
    }    
}
finally {
    Pop-Location
}
