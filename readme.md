# Nancy-Playground

This project is meant to replicate RTaW's _Network calculus interpreter_ interface and features so that

 - People used to that interface can easily migrate to a Nancy implementation
 - Code shared in that form can be run and studied (profile, debug, etc.) locally, using Nancy

# Syntax name and file extension

The names for the tool and its syntax change often between the playground website, its online documentation, the downloadable user manual and available examples.
This is a problem since I will need to refer to the syntax many times.

Therefore, I hereby declare _MPPG_ as the name of the syntax and `.mppg` as the corresponding file extension.
The acronym stands for _Min-Plus PlayGround_.

> I am not aware of a better or more fitting idea, let me know if you do.

See the [MPPG Syntax](/syntax.md) docs for a summary of the supported constructs.

The file extension can be used to associate it with the programs created in this project.

# Requirements

`Nancy-Playground` is a .NET 9.0 application, written in C# 12. 
Both SDK and runtime for .NET are cross-platform, and can be downloaded from [here](https://dotnet.microsoft.com/en-us/download).

The MPPG grammar and its parser is written using `ANTLR`.
The parser code is already in the repository [here](./Nancy-Playground/MppgParser/Grammar/), so there is no need to build it again.
You will need it if you make changes to the grammar, e.g. to add a new operator.
The [`regen-grammar.ps1`](./Nancy-Playground/MppgParser/Grammar/regen-grammar.ps1) is designed to download and run `ANTLR` locally, with the right arguments.

You will need [Powershell](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell?view=powershell-7.5) to run the script and [Java 11 or later](https://adoptium.net/temurin/releases/) to run `ANTLR`.
