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

# Usage

`nancy-playground` can run either in _interactive_ mode, where each line is submitted by the user one by one, or in _scripted_ mode, where a `.mppg` script is provided.

Examples:
```
nancy-playground interactive
nancy-playground run ./Examples/hal-04513292v1.mppg
```

The interactive mode is the default, in case no argument is provided.

`--help` is also available.
```
USAGE:
    nancy-playground [OPTIONS] [COMMAND]

OPTIONS:
    -h, --help           Prints help information
    -o, --output-mode    How the output is formatted. Available options: ExplicitPrintsOnly, MppgClassic, NancyNew (default)
    -r, --run-mode       How the computations are performed. Available options are PerStatement (computes the result of each line as it comes), ExpressionsBased (computes only as needed, e.g. for plots and value prints). Default: PerStatement
    -e, --on-error       Specifies what to do when an error occurs. Available options: Stop (default), Continue
        --no-welcome     Mutes the welcome message
        --version        If used, the program prints out the version and immediately terminates

COMMANDS:
    run <file>        Runs a .mppg script
    interactive       Interactive mode, where the user can input MPPG lines one by one
    convert <file>
```

# Installation

## For users

`nancy-playground` is available as a [dotnet tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools).
Make sure `dotnet` is installed (see [here](https://dotnet.microsoft.com/en-us/download)), then run

```
dotnet tool install --global unipi.nancy.playground.cli
```

After it completes (and possibly after opening a new terminal) you should see the tool available as `nancy-playground`.

## For devs

`Nancy-Playground` is a .NET 9.0 application, written in C# 12. 
Both SDK and runtime for .NET are cross-platform, and can be downloaded from [here](https://dotnet.microsoft.com/en-us/download).

A PowerShell script is available to "compile and install" `nancy-playground-dev` to be run from a terminal, which currently support only Windows and Linux.
You will need [PowerShell](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell?view=powershell-7.5) to run it.

```
> ./install-nancy-playground.ps1
``` 

By default, this will build the program once and make it available to use.
The optional `-Dev` argument will, instead, make it so that the program is recompiled from the contents of this folder each time you run the program.

```
> ./install-nancy-playground.ps1 -Dev
``` 

The application can then be launched as `nancy-playground-dev`. 
The suffix `-dev` is there to avoid conflict with the published version.

### To work on the grammar

The MPPG grammar and its parser is written using `ANTLR`.
The `ANTLR` grammar is used to *build* the parser code, which you can find [here](./Nancy-Playground/MppgParser/Grammar/).
If you make any changes to the grammar, e.g. to add a new operator, you will need to build the parser code again.

To do this, run the [`regen-grammar.ps1`](./Nancy-Playground/MppgParser/Grammar/regen-grammar.ps1) script, which will download and run `ANTLR` locally, with the right arguments.

You will need [PowerShell](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell?view=powershell-7.5) to run the script and [Java 11 or later](https://adoptium.net/temurin/releases/) to run `ANTLR`.

# Academic attribution

This software is an academic product, just like papers are. If you build on someone else's scientific ideas, you will obviously cite their paper reporting these ideas. 
This is standard academic practice. Like it or not, citations are the academic currency. 

```
If you use the Nancy library, or any software including parts of it or derived from it, 
we would appreciate it if you could cite the original paper describing it:

R. Zippo, G. Stea, "Nancy: an efficient parallel Network Calculus library", 
SoftwareX, Volume 19, July 2022, DOI: 10.1016/j.softx.2022.101178
```

The MIT license allows you to use this software for almost any purpose. However, if you use or include this software or its code (in full or in part) in your own, the fact that you are doing so in full compliance to the license does not exempt you from following standard academic practices regarding attribution and citation. 
This means that it is still your duty to ensure that users of your software:

  1. know that it use or includes our work, and 
  
  2. they can cite the above paper for correct attribution (along with your own work, possibly). 

The above two requirements are met, for open source projects, if you report the statement above in the readme of any code that uses or includes ours. 
