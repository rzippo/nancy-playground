#!/bin/sh
SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
psargs="$SCRIPT_DIR/nancy-playground.ps1 $*"
pwsh -Command "$psargs"
