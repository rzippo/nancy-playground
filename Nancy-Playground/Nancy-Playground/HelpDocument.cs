using System.Diagnostics.CodeAnalysis;

namespace Unipi.Nancy.Playground.Cli;

/// <summary>
/// The manual of the syntax, as the <c>manual</c> command and the interactive help print it.
/// </summary>
[ExcludeFromCodeCoverage]
public static class NancyPlaygroundDocs
{
    /// <summary>
    /// The manual itself, i.e. every section and the items it documents.
    /// </summary>
    public static HelpDocument HelpDocument = new()
    {
        Preamble = """
Here are the supported constructs by the MPPG syntax.
The goal is to support as many constructs as possible to run existing code, and to optionally extend the syntax where useful.

WARNING: the initial contents of this help page was written by AI, using the docs/syntax.md document as source.
Expect (and please report) oddities.
""",
        Sections =
        [
            new HelpSection
            {
                Name = "Interactive commands",
                Description = "Commands available only in interactive mode",
                Items = [
                    new HelpItem
                    {
                        Name = "Help",
                        Formats = ["!help [query]"],
                        Description = "Shows this help text, or a search result. Useful reference for the syntax to use in scripts.",
                        Tags = ["help", "manual", "documentation", "interactive", "command", "cli"]
                    },
                    new HelpItem
                    {
                        Name = "CLI Help",
                        Formats = ["!clihelp"],
                        Description = "Shows the CLI help text. Useful reference for commands and options of this app.",
                        Tags = ["cli", "help", "manual", "documentation"]
                    },
                    new HelpItem
                    {
                        Name = "Quit",
                        Formats = ["!quit", "!exit"],
                        Description = "Terminates the program.",
                        Tags = ["quit", "exit", "terminate"]
                    },
                    new HelpItem
                    {
                        Name = "Export",
                        Formats = ["!export <output-file>", "!save <output-file>"],
                        Description = "Exports the commands in the current interactive session to a .mppg file. A relative path is resolved against the export root, set with --export-root.",
                        Tags = ["export", "save", "file"]
                    },
                    new HelpItem
                    {
                        Name = "Convert",
                        Formats = ["!convert <output-file>"],
                        Description = "Converts the commands in the current interactive session to a Nancy C# program and saves it to a file. A relative path is resolved against the export root, set with --export-root.",
                        Tags = ["convert", "nancy", "csharp", "save", "file"]
                    },
                    new HelpItem
                    {
                        Name = "Load",
                        Formats = ["!load <input-file>", "!load [-h|--history] <input-file>"],
                        Description = "Loads and executes commands from a .mppg file into the current interactive session.",
                        LongDescription = """
Reads a .mppg file line by line and executes each line in the current interactive session.
Empty lines and lines starting with // are skipped.

Options:
- `-h` or `--history`: Adds all loaded lines to the command history for arrow key navigation.
""",
                        Tags = ["load", "file", "import", "execute", "history"]
                    },
                    new HelpItem
                    {
                        Name = "Clear",
                        Formats = ["!clear", "!clear [-h|--history]"],
                        Description = "Resets the current session by clearing all variables and executed lines.",
                        LongDescription = """
Clears all variables and statement history from the current session.
By default, the command history for arrow key navigation is preserved.

Options:
- `-h` or `--history`: Also clears the command history.
""",
                        Tags = ["clear", "reset", "session", "variables", "history"]
                    }
                ]
            },
            new HelpSection
            {
                Name = "Comments",
                Description = "Line and inline comments that are ignored by the interpreter.",
                Tags = ["comments", "syntax", "statement"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "Line comments",
                        Formats = ["// text", "% text", "# text", "> text"],
                        Description = "Lines that start with //, %, #, or > are comments and are ignored.",
                        LongDescription = """
Lines that start with any of these characters are treated as whole-line comments:
- `//`
- `%`
- `#`
- `>`

They are not parsed as expressions and do not affect execution.
""",
                        Examples = """
// This is a comment
% This is also a comment
# This is a comment as well
> This is a comment as well
"""
                    },
                    new HelpItem
                    {
                        Name = "Inline comments",
                        Formats = ["expression // text", "expression % text", "expression # text"],
                        Description = "Comments at the end of a statement, starting with //, % or #.",
                        LongDescription = """
Inline comments can appear after a statement. They must start with `//`, `%`, or `#`.
Inline comments starting with `>` are NOT supported.
""",
                        Examples = """
f := ... // This is a comment
g := ... % This is also a comment
h := ... # This is a comment as well
"""
                    }
                ],
            },

            new HelpSection
            {
                Name = "Types",
                Description = "Supported value kinds.",
                Tags = ["types", "kinds", "syntax", "statement"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "Function and scalar values",
                        Formats = ["function", "scalar"],
                        Description = "The syntax supports function values (curves) and scalar values.",
                        LongDescription = """
- Functions (also called curves in MPPG) represent piecewise-defined curves, service curves, arrival curves, etc.
- Scalars are numeric values (rationals, ±infinity).
""",
                        Tags = ["types", "variables"]
                    }
                ],
            },

            new HelpSection
            {
                Name = "Variable declaration",
                Description = "Naming functions and scalars.",
                Tags = ["variables", "declaration", "assignment", "names", "syntax"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "Assignment",
                        Formats = ["name := expression"],
                        Description = "Assigns a function or scalar expression to a variable name.",
                        LongDescription = """
Variables can store both scalar and function values. Once assigned, they can be reused in later expressions.
""",
                        Examples = """
f := ratency(1, 2)
g := f * f
x := 3/2
"""
                    }
                ]
            },

            new HelpSection
            {
                Name = "Function constructors: known shapes",
                Description = "Built-in function constructors with common shapes.",
                Tags = ["functions", "constructors", "service-curves", "arrival-curves", "shapes", "builtins", "syntax"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "ratency",
                        Formats = ["ratency(a, b)"],
                        Description = "Rate-latency service function with rate a ≥ 0 and latency b ≥ 0.",
                        LongDescription = """
Constructs a rate-latency service curve:
- Parameter `a`: rate (slope), must be ≥ 0
- Parameter `b`: latency (horizontal shift), must be ≥ 0
""",
                        Tags = ["ratency", "service-curve", "curve", "rate-latency"]
                    },
                    new HelpItem
                    {
                        Name = "bucket",
                        Formats = ["bucket(a, b)"],
                        Description = "Leaky bucket arrival function with slope a ≥ 0 and constant b ≥ 0.",
                        LongDescription = """
Constructs a leaky bucket arrival curve:
- `a` is the sustained arrival rate (slope)
- `b` is the burst size (vertical offset)
""",
                        Tags = ["bucket", "arrival-curve", "curve", "leaky-bucket", "sigma-rho"]
                    },
                    new HelpItem
                    {
                        Name = "affine",
                        Formats = ["affine(a, b)"],
                        Description = "Affine function with slope a and constant b. Right-continuous at x = 0.",
                        LongDescription = """
Constructs an affine function f(x) = a·x + b.
The function is right-continuous at 0: f(0+) = f(0).
""",
                        Tags = ["affine", "linear", "curve"]
                    },
                    new HelpItem
                    {
                        Name = "step",
                        Formats = ["step(o, h)"],
                        Description = "Step function with step at time o and height h.",
                        Tags = ["step", "step-function", "curve"]
                    },
                    new HelpItem
                    {
                        Name = "stair",
                        Formats = ["stair(o, l, h)"],
                        Description = "Staircase function with first step at time o, length l, and step height h.",
                        Tags = ["stair", "staircase", "piecewise", "curve"]
                    },
                    new HelpItem
                    {
                        Name = "delay",
                        Formats = ["delay(o)"],
                        Description = "Burst-delay function that occurs at time o.",
                        Tags = ["delay", "burst-delay", "curve"]
                    },
                    new HelpItem
                    {
                        Name = "zero",
                        Formats = ["zero"],
                        Description = "Zero function: f(x) = 0 for x ≥ 0.",
                        Tags = ["zero", "zero-function", "constant", "curve"]
                    },
                    new HelpItem
                    {
                        Name = "epsilon",
                        Formats = ["epsilon"],
                        Description = "Epsilon function: f(x) = +\\infty for x ≥ 0.",
                        Tags = ["epsilon", "infinity", "constant", "curve"]
                    }
                ]
            },

            new HelpSection
            {
                Name = "Function constructors: arbitrary shapes",
                Description = "Ultimately affine and ultimately pseudo-periodic functions built from segments.",
                Tags = ["constructors", "segments", "uaf", "upp", "piecewise", "pseudo-periodic", "syntax"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "Segments",
                        Formats = ["[(x1, y1)s(x2, y2)] and variants"],
                        Description = "Primitive building blocks for arbitrary piecewise functions.",
                        LongDescription = """
Segments describe intervals in the (x, y) plane. Variants control inclusion of endpoints and how the slope is given.

Supported segment forms:
- `[(x, y)]` — a spot at (x, y). (Not implemented)
- `[(x1, y1)slope(x2, y2)]` — closed on the right; slope explicitly given.
- `[(x1, y1)slope(x2, y2)[` — right endpoint excluded.
- `](x1, y1)slope(x2, y2)]` — left endpoint excluded, right included.
- `[(x1, y1)(x2, y2)[` — slope is automatically computed from endpoints.

x and y can be any rational number or ±infinity. The end value of a segment is used for consistency checks, even if it is right-open.
""",
                        Examples = """
[(0, -3)1(1, -2)[
[(1, -2)2(7, 10)[
[(7, 10)0(+inf, 10)[
""",
                        Tags = ["segments", "piecewise", "intervals", "uaf", "upp", "syntax"]
                    },
                    new HelpItem
                    {
                        Name = "Ultimately Affine functions",
                        Formats = ["uaf(SEGMENT+)"],
                        Description = "Ultimately affine function built from one or more segments.",
                        LongDescription = """
Syntax:
- `uaf(SEGMENT+)`

At least one segment is required. The last segment must extend to +∞.

Example of a valid ultimately affine function:
uaf( [(0,-3)1(1,-2)[ [(1,-2)2(7,10)[ [(7,10)0(+inf,10)[ )
""",
                        Examples = """
uaf( [(0,-3)1(1,-2)[ [(1,-2)2(7,10)[ [(7,10)0(+inf,10)[ )
""",
                        Tags = ["uaf", "ultimately-affine", "segments", "piecewise", "curve"]
                    },
                    new HelpItem
                    {
                        Name = "Ultimately Pseudo-Periodic functions",
                        Formats = ["upp([finiteSegments,] period(periodicSegments) [, incr[, period]])"],
                        Description = "Ultimately pseudo-periodic function with a finite part and a repeating pseudo-periodic part.",
                        LongDescription = """
Syntax:
- `upp([SEGMENT*], period(SEGMENT*) [, incr[, period]])`

Meaning:
- First segment list (optional): finite prefix (non-periodic part).
- `period(...)`: mandatory; describes one pseudo-period.
- `incr` (optional): increment per period.
- Final `period` (optional): purely informational period length.

Examples:
1)
upp( period( [(0, 0) 0 (2, 0)[ [(2, 0) 1 (7, 5)] ](7, 5) 0 (12, 5)[ ))

2)
upp( [(0, +Infinity) 0 (6, +Infinity)],
     period (](6, 0) 0 (10.5, 0)[ [(10.5, +Infinity) 0 (18, +Infinity)]),
     0,
     12)
""",
                        Examples = """
upp( period( [(0, 0) 0 (2, 0)[ [(2, 0) 1 (7, 5)] ](7, 5) 0 (12, 5)[ ))

upp( [(0, +Infinity) 0 (6, +Infinity)],
     period (](6, 0) 0 (10.5, 0)[ [(10.5, +Infinity) 0 (18, +Infinity)]),
     0,
     12)
""",
                        Tags = ["upp", "ultimately-pseudo-periodic", "periodic", "segments", "curve"]
                    }
                ]
            },

            new HelpSection
            {
                Name = "Scalar values",
                Description = "Number syntax and allowed literals.",
                Tags = ["scalars", "numbers", "syntax"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "Number syntax",
                        Formats = ["integer", "decimal", "rational", "±inf", "±infinity"],
                        Description = "Scalars are rationals plus ±infinity.",
                        LongDescription = """
Supported numeric literals:
- Integers: `0`, `1`, `-3`
- Decimals: `0.25`, `3.14`, `-0.5`
- Rationals: `3/2`, `1/4`
- Positive infinity: `+inf`, `+infinity`
- Negative infinity: `-inf`, `-infinity`

Decimals are converted to exact rational values internally, avoiding floating-point precision issues.
""",
                        Examples = string.Empty,
                        Tags = ["scalars", "numbers", "rational", "decimal", "infinity"]
                    }
                ]
            },

            new HelpSection
            {
                Name = "Function-returning operations",
                Description = "Operations that take functions (and possibly scalars) and return functions.",
                Tags = ["functions", "operations", "syntax"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "Min/max",
                        Formats = ["f1 /\\ f2", "f1 \\/ f2"],
                        Description = "Minimum or maximum of two functions.",
                        LongDescription = """
- `f1 /\\ f2`: minimum of f1 and f2.
- `f1 \\/ f2`: maximum of f1 and f2.
""",
                        Tags = ["min", "max", "operation", "pointwise"]
                    },
                    new HelpItem
                    {
                        Name = "Addition and subtraction (functions)",
                        Formats = ["f1 + f2", "f1 - f2"],
                        Description = "Sum or difference of two functions.",
                        Tags = ["addition", "subtraction", "operation", "pointwise"]
                    },
                    new HelpItem
                    {
                        Name = "(min,+) convolution",
                        Formats = ["f1 * f2", "f1 *_ f2"],
                        Description = "(min,+) convolution of f1 and f2.",
                        LongDescription = """
Both `*` and `*_` denote the (min,+) convolution. They are aliases.
""",
                        Tags = ["convolution", "min-plus", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "(max,+) convolution",
                        Formats = ["f1 *^ f2"],
                        Description = "(max,+) convolution of f1 and f2.",
                        Tags = ["convolution", "max-plus", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "(min,+) deconvolution",
                        Formats = ["f1 / f2", "f1 /_ f2"],
                        Description = "(min,+) deconvolution of f1 by f2.",
                        Tags = ["deconvolution", "min-plus", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "(max,+) deconvolution",
                        Formats = ["f1 /^ f2"],
                        Description = "(max,+) deconvolution of f1 by f2.",
                        Tags = ["deconvolution", "max-plus", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Subadditive closure",
                        Formats = ["star(f)", "subaddclosure(f)"],
                        Description = "Subadditive closure of f.",
                        LongDescription = """
- `star(f)`: subadditive closure of f. Always available.
- `subaddclosure(f)`: synonym for subadditive closure. Requires syntax version 1.2 or later.
""",
                        Tags = ["closure", "subadditive", "star", "subaddclosure", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Superadditive closure",
                        Formats = ["superaddclosure(f)"],
                        Description = "Superadditive closure of f.",
                        LongDescription = """
- `superaddclosure(f)`: superadditive closure of f. Requires syntax version 1.2 or later.
""",
                        Tags = ["closure", "superadditive", "superaddclosure", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Horizontal shift",
                        Formats = ["hShift(f, n)", "hshift(f, n)"],
                        Description = "Function identical to f but shifted horizontally by n.",
                        LongDescription = """
- Positive n: shift to the right.
- Negative n: shift to the left.

Both `hShift` and `hshift` are accepted spellings.
""",
                        Tags = ["shift", "horizontal", "traslation", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Vertical shift",
                        Formats = ["vShift(f, n)", "vshift(f, n)"],
                        Description = "Function identical to f but shifted vertically by n.",
                        Tags = ["shift", "vertical", "traslation", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Pseudo-inverse (lower and upper)",
                        Formats = ["inv(f)", "low_inv(f)", "up_inv(f)"],
                        Description = "Lower and upper pseudo-inverses of f.",
                        LongDescription = """
- `inv(f)` and `low_inv(f)`: lower pseudo-inverse.
- `up_inv(f)`: upper pseudo-inverse.
""",
                        Tags = ["pseudo-inverse", "inverse", "lower", "upper", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Upper non-decreasing closure",
                        Formats = ["upclosure(f)", "upnondec(f)", "upnondecclosure(f)", "nnupclosure(f)", "nnupnondec(f)", "nnupnondecclosure(f)"],
                        Description = "Upper non-decreasing closure (optionally non-negative).",
                        LongDescription = """
- `upclosure(f)`: upper non-decreasing closure of f.
- `nnupclosure(f)`: non-negative upper non-decreasing closure of f.

`upnondec(f)`/`upnondecclosure(f)` and `nnupnondec(f)`/`nnupnondecclosure(f)` are the same two operations, under the explicit spelling of syntax version 1.4 or later.
""",
                        Tags = ["closure", "non-decreasing", "upper", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Lower non-decreasing closure",
                        Formats = ["lowclosure(f)", "lownondec(f)", "lownondecclosure(f)", "nnlowclosure(f)", "nnlownondec(f)", "nnlownondecclosure(f)"],
                        Description = "Lower non-decreasing closure (optionally non-negative).",
                        LongDescription = """
- `lowclosure(f)`: lower non-decreasing closure of f.
- `nnlowclosure(f)`: non-negative lower non-decreasing closure of f.

`lowclosure` and `nnlowclosure` require syntax version 1.2 or later.
`lownondec(f)`/`lownondecclosure(f)` and `nnlownondec(f)`/`nnlownondecclosure(f)` are the same two operations, under the explicit spelling of syntax version 1.4 or later.
""",
                        Tags = ["closure", "non-decreasing", "lower", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Upper non-increasing closure",
                        Formats = ["upnoninc(f)", "upnonincclosure(f)"],
                        Description = "Upper non-increasing closure of f.",
                        LongDescription = """
- `upnoninc(f)`/`upnonincclosure(f)`: the least wide-sense non-increasing curve g ≥ f.

Requires syntax version 1.4 or later.
""",
                        Tags = ["closure", "non-increasing", "upper", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Lower non-increasing closure",
                        Formats = ["lownoninc(f)", "lownonincclosure(f)"],
                        Description = "Lower non-increasing closure of f.",
                        LongDescription = """
- `lownoninc(f)`/`lownonincclosure(f)`: the greatest wide-sense non-increasing curve g ≤ f.

Requires syntax version 1.4 or later.
""",
                        Tags = ["closure", "non-increasing", "lower", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Floor and ceiling (functions)",
                        Formats = ["floor(f)", "ceil(f)"],
                        Description = "Floor and ceiling of a function, applied to its values.",
                        LongDescription = """
- `floor(f)`: the function g such that g(x) = ⌊f(x)⌋.
- `ceil(f)`: the function g such that g(x) = ⌈f(x)⌉.

Both take a function or a scalar, and return the same kind: `floor(f)` is a function, `floor(3/2)` is a scalar.
They require syntax version 1.3 or later.
""",
                        Tags = ["floor", "ceiling", "rounding", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Composition",
                        Formats = ["f comp g"],
                        Description = "Composition of functions: (f ∘ g)(x) = f(g(x)).",
                        Tags = ["composition", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Left/right projections",
                        Formats = ["left-ext(f)", "right-ext(f)"],
                        Description = "Left- and right-continuous projections of f.",
                        LongDescription = """
- `left-ext(f)`: g(x) = f(x⁻)
- `right-ext(f)`: g(x) = f(x⁺)
""",
                        Tags = ["extensions", "left-continuous", "right-continuous", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Scaling by a scalar",
                        Formats = ["scalar * f", "f * scalar", "f / scalar"],
                        Description = "Multiply or divide a function by a scalar.",
                        Tags = ["scaling", "multiplication", "division", "operation"]
                    }
                ]
            },

            new HelpSection
            {
                Name = "Scalar-returning operations on functions",
                Description = "Operations that take functions (or functions and scalars) and return a scalar.",
                Tags = ["functions", "scalars", "operations", "evaluation", "deviation", "syntax"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "Evaluation",
                        Formats = ["f(x)", "f(x+)", "f(x-)", "f(x~+)", "f(x~-)"],
                        Description = "Evaluates function f at or around a point x.",
                        LongDescription = """
- `f(x)`: value of f at x.
- `f(x+)` / `f(x~+)`: right-limit of f at x.
- `f(x-)` / `f(x~-)`: left-limit of f at x.

Both `f(x+)`/`f(x-)` and `f(x~+)`/`f(x~-)` are supported.
""",
                        Tags = ["functions", "evaluation", "limits", "right-limit", "left-limit", "scalars", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Horizontal deviation",
                        Formats = ["hDev(f, g)", "hdev(f, g)"],
                        Description = "Horizontal deviation between f and g.",
                        Tags = ["functions", "deviation", "horizontal", "hDev", "metrics", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Vertical deviation",
                        Formats = ["vDev(f, g)", "vdev(f, g)"],
                        Description = "Vertical deviation between f and g.",
                        Tags = ["functions", "deviation", "vertical", "vDev", "metrics", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Z-deviation",
                        Formats = ["zDev(f, g)", "zdev(f, g)"],
                        Description = "Z-deviation between f and g. Used for delay bounds with negative service curves.",
                        LongDescription = """
- `zDev(f, g)` and `zdev(f, g)`: computes $z(f, g) = \inf\{t \ge 0 \mid f \otimes g (t) \ge 0\}$.

Requires syntax version 1.2 or later.
""",
                        Tags = ["functions", "deviation", "z", "zDev", "metrics", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Max backlog period length",
                        Formats = ["maxBacklogPeriod(f, g)"],
                        Description = "Max backlog period length between f and g. (Not implemented)",
                        Tags = ["functions", "backlog", "period", "metrics", "not-implemented"]
                    }
                ]
            },

            new HelpSection
            {
                Name = "Scalar operations",
                Description = "Operations between scalars returning scalars.",
                Tags = ["scalars", "operations", "arithmetic", "min", "max", "syntax"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "Min/max",
                        Formats = ["v1 /\\ v2", "v1 \\/ v2"],
                        Description = "Minimum or maximum of two scalar values.",
                        LongDescription = """
- `v1 /\\ v2`: minimum of v1 and v2.
- `v1 \\/ v2`: maximum of v1 and v2.
""",
                        Tags = ["scalars", "min", "max", "comparison", "operations", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Arithmetic",
                        Formats = ["v1 + v2", "v1 - v2", "v1 * v2", "v1 / v2", "v1 div v2", "v1 mod v2"],
                        Description = "Standard scalar arithmetic operations.",
                        LongDescription = """
- `v1 + v2`: addition
- `v1 - v2`: subtraction
- `v1 * v2`: multiplication
- `v1 / v2`: division
- `v1 div v2`: division (same semantics for this syntax).
  It requires syntax version 1.1 or later.
  It is a legacy RTaW operator, which RTaW's own 1.5.0 removed: kept so older scripts run.
- `v1 mod v2`: remainder of the division, which takes the sign of v1, e.g. `-7/2 mod 3` is -1/2.
  It binds like the other product operators, so `1 + x mod y` is `1 + (x mod y)`.
  It requires syntax version 1.3 or later.
""",
                        Tags = ["scalars", "arithmetic", "addition", "multiplication", "division", "remainder", "modulo", "operations", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Absolute value and power",
                        Formats = ["abs(v)", "pow(v, n)"],
                        Description = "Absolute value of a scalar, and a scalar raised to an integer power.",
                        LongDescription = """
- `abs(v)`: |v|, e.g. `abs(-7/2)` is 7/2.
- `pow(v, n)`: v raised to n, e.g. `pow(2, 10)` is 1024 and `pow(2, -2)` is 1/4.
  n must be an integer, and a non-integer exponent is rejected.

They require syntax version 1.3 or later.
""",
                        Tags = ["scalars", "absolute", "power", "exponent", "operations", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Gcd and lcm",
                        Formats = ["gcd(v1, v2)", "lcm(v1, v2)"],
                        Description = "Greatest common divisor and least common multiple.",
                        LongDescription = """
- `gcd(v1, v2)`: greatest common divisor, e.g. `gcd(12, 18)` is 6.
- `lcm(v1, v2)`: least common multiple, e.g. `lcm(4, 6)` is 12.

Both work on rationals, not only on integers: `gcd(1/2, 1/3)` is 1/6.
They require syntax version 1.3 or later.
""",
                        Tags = ["scalars", "gcd", "lcm", "divisor", "multiple", "operations", "operation"]
                    },
                    new HelpItem
                    {
                        Name = "Floor and ceiling (scalars)",
                        Formats = ["floor(v)", "ceil(v)"],
                        Description = "Largest integer not above, or smallest integer not below, a scalar value.",
                        LongDescription = """
- `floor(v)`: ⌊v⌋, e.g. `floor(7/2)` is 3 and `floor(-7/2)` is -4.
- `ceil(v)`: ⌈v⌉, e.g. `ceil(7/2)` is 4 and `ceil(-7/2)` is -3.

The same names applied to a function round its values instead, see "Floor and ceiling (functions)".
They require syntax version 1.3 or later.
""",
                        Tags = ["scalars", "floor", "ceiling", "rounding", "operations", "operation"]
                    }
                ]
            },

            new HelpSection
            {
                Name = "Output",
                Description = "Rules for console output of expressions, variables and assertions.",
                Tags = ["output", "printing", "console", "variables", "display"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "Expression output",
                        Formats = ["expression"],
                        Description = "Any expression not assigned to a variable prints its value.",
                        LongDescription = """
- If an expression is not on the right side of `:=`, it is evaluated and its result is printed.
- If it is a function, the function is printed in its uaf/upp definition format.
""",
                        Tags = ["output", "expressions", "printing", "console"]
                    },
                    new HelpItem
                    {
                        Name = "Assignment output",
                        Formats = ["name := expression"],
                        Description = "Assignments print the variable name, not the value.",
                        LongDescription = """
- `f := ratency(1, 2)` will print `f`.
- The function value is stored and can be printed by evaluating `f` later.
""",
                        Tags = ["output", "assignment", "variables", "console"]
                    },
                    new HelpItem
                    {
                        Name = "Printing a variable",
                        Formats = ["name"],
                        Description = "Typing the name of a variable prints its content.",
                        LongDescription = """
- If the variable holds a function, its value is printed as a `uaf(...)` or `upp(...)` definition, regardless of the original constructor.
- If the variable holds a scalar, the scalar value is printed.
""",
                        Tags = ["output", "variables", "printing", "console"]
                    },
                    new HelpItem
                    {
                        Name = "Printing the expression of a variable",
                        Formats = ["printExpression(f)"],
                        Description = "Prints the expression of f, rather than its canonical uaf/upp form.",
                        LongDescription = """
Useful to inspect the original expression a variable was defined with, instead of its normalized representation.
It requires syntax version 1.1 or later.
""",
                        Tags = ["printExpression", "expression", "debugging", "output", "syntax"]
                    },
                    new HelpItem
                    {
                        Name = "Testing a property",
                        Formats = ["assert( exp1 OP exp2 )"],
                        Description = "Assertions tests for equality or inequality.",
                        LongDescription = "Assertions tests for equality or inequality. See assert command for more details."
                    }
                ]
            },

            new HelpSection
            {
                Name = "Plots",
                Description = "Plotting functions using plot(f1, f2, ..., args), as an image or as TikZ code.",
                Tags = ["plots", "graph", "visualization", "plot", "plotTikz", "tikz", "functions", "syntax"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "plot",
                        Formats = ["plot(f1, f2, ..., args)"],
                        Description = "Plots one or more function variables with optional configuration arguments. (partially)",
                        LongDescription = """
General form:
- `plot(f1, f2, ..., args)`

Notes:
- Functions must be variables (not inline expressions).
- Args and function names can appear in any order.
- Args can be numbers, intervals, or strings (possibly composed via sums).
- `gui` applies per plot, while --no-gui applies to the whole run and overrides it. The image is written either way.

Supported args:
- `main`: graph title.
- `title`: alias for `main`.
- `xlim=[min, max]`: x-axis range.
- `ylim=[min, max]`: y-axis range.
- `xlab="text"`: label for x-axis.
- `ylab="text"`: label for y-axis.
- `out="file.png"`: save to PNG file. The .png extension is enforced, so it can be omitted.
- `grid="no"`: disable grid. (Not implemented)
- `bg="no"`: white background instead of grey. (Not implemented)
- `gui="no"`: custom flag to skip showing the plot in a GUI window. Default: "yes".
""",
                        Examples = """
plot(f1)
plot(f1, f2)
plot(service2, service1, xlim=[-0.3, 15], ylim=[-0.3, 15])
plot(f1, main="f1 for J=" +J +"Jitter", xlim=[-0.5, 5], xlab="time", ylab="packets", out="image.png")
plot(xlim=[-0.3, 15], ylim=[-0.3, 15], service2, service1)
""",
                        Tags = ["plots", "plot", "graph", "visualization", "xlim", "ylim", "xlab", "ylab", "gui", "out"]
                    },

                    new HelpItem
                    {
                        Name = "plotTikz",
                        Formats = ["plotTikz(f1, f2, ..., args)"],
                        Description = "Plots one or more function variables as TikZ code, to be compiled with LaTeX, instead of an image.",
                        LongDescription = """
General form:
- `plotTikz(f1, f2, ..., args)`

It requires syntax version 1.1 or later.

The code is printed to the console, unless `out` is used to write it to file.
The arguments are the same as `plot`, with these differences:
- `main`, `title`: graph title. (Not supported by Nancy.Plots.Tikz)
- `xlab`, `ylab`: labels for the axes. (Nancy.Plots.Tikz always uses "time" and "data")
- `out="file.tikz"`: save the TikZ code to file. The .tikz extension is enforced, so it can be omitted, while .tex is also accepted.
- `gui="no"`: no effect, as no GUI is used.
""",
                        Examples = """
plotTikz(f1)
plotTikz(f1, f2)
plotTikz(service2, service1, xlim=[-0.3, 15], ylim=[-0.3, 15])
plotTikz(f1, main="f1 for J=" +J +"Jitter", xlab="time", ylab="packets", out="plot.tikz")
""",
                        Tags = ["plots", "plotTikz", "tikz", "latex", "graph", "visualization", "xlim", "ylim", "xlab", "ylab", "out"]
                    }
                ]
            },

            new HelpSection
            {
                Name = "Asserts",
                Description = "Relational checks between functions and/or scalars.",
                Tags = ["assert", "assertion", "checks", "relations", "constraints", "syntax"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "assert",
                        Formats = ["assert(f OP g)"],
                        Description = "Tests a relation between two expressions; prints true or an error message.",
                        LongDescription = """
f and g can be variable names or expressions, and can evaluate to either functions or scalars.
For =, !=, <= and >=, the relation must hold for all t: f(t) OP g(t), or f(t) OP c for function vs scalar c.
< and > are also supported, from syntax version 1.1 on.
For functions, a curve's ordering is partial: f < g means f(t) <= g(t) for all t and f is not the same curve as g, not f(t) < g(t) everywhere.

If the assertion holds, prints `true`, otherwise `false`.
""",
                        Examples = """
assert(f <= g)
assert(h != zero)
assert(f < g)
""",
                        Tags = ["assert", "assertion", "comparison", "constraints", "checks", "relations"]
                    },
                    new HelpItem
                    {
                        Name = "Property asserts",
                        Formats = ["assert(f is X)", "assert(f is not X)"],
                        Description = "Tests one property of one expression, rather than a relation between two; prints true or an error message.",
                        LongDescription = """
Requires syntax version 1.4 or later.
X is one of the property names below, and `is not` negates the check.

These apply to a function only:
- `subadditive`, `superadditive`
- `concave`, `convex`
- `nondecreasing`, `increasing`
- `plain`, `ultimatelyplain`
- `ultimatelyaffine` (`ua`), `ultimatelyconstant` (`uc`)
- `continuous`, `continuousexceptorigin`
- `leftcontinuous`, `rightcontinuous`
- `passingthroughorigin`, `nonnegative`
- `ultimatelyfinite`, `ultimatelyinfinite` (`ui`), `ultimatelyplusinfinite`, `ultimatelyminusinfinite`

These apply to either a function or a scalar: `finite`, `zero`, `plusinfinite`, `minusinfinite`.
For a function they mean finite/zero/infinite everywhere, for a scalar not-infinite, zero, or infinite.

`integer` applies to a scalar only: true where it has no fractional part.

Using a property with the wrong kind of operand is an error, not a silent `false`.

If the assertion holds, prints `true`, otherwise `false`.
""",
                        Examples = """
assert(f is subadditive)
assert(f is not concave)
assert(x is integer)
""",
                        Tags = ["assert", "assertion", "property", "predicate", "is", "subadditive", "concave", "convex", "ultimatelyaffine", "ua", "integer", "syntax"]
                    }
                ]
            },

            new HelpSection
            {
                Name = "Syntax version",
                Description = "The #!syntax version directive, and what each version gates.",
                Tags = ["syntax version", "versioning", "directive", "gating", "compatibility"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "Syntax version",
                        Formats = ["#!syntax version X.Y"],
                        Description = "Selects the syntax version used for the program, defaulting to the latest.",
                        LongDescription = """
The directive is applied only as the first line of the program, and only once.

In interactive mode the same rule holds for the session.
Use `!clear` to start a new session, and with it select a new version.

A keyword only acts as one from the version that introduced it.
Declaring a version keeps a program working as later versions add keywords:
`floor := 3` is an assignment under version 1.2, and the floor operator from 1.3 on.
A program that uses one of these names without declaring a version is told which name it is, and which directive keeps it.

The `<` and `>` operators of `assert` are version-gated too, from 1.1 on.

Versions and the keywords they introduce:
- 1.1: `printExpression`, `plotTikz`, `div`, `<` and `>` as `assert` operators
- 1.2: `subaddclosure`, `superaddclosure`, `lowclosure`, `nnlowclosure`, `zDev`, `zdev`
- 1.3: `floor`, `ceil`, `abs`, `pow`, `mod`, `gcd`, `lcm`
- 1.4: `upnoninc`, `upnonincclosure`, `lownoninc`, `lownonincclosure`, `upnondec`, `upnondecclosure`, `lownondec`, `lownondecclosure`, `nnupnondec`, `nnupnondecclosure`, `nnlownondec`, `nnlownondecclosure`, property asserts
""",
                        Examples = """
#!syntax version 1.2
floor := 3
""",
                        Tags = ["syntax version", "version", "directive", "gating", "compatibility", "keywords"]
                    }
                ]
            }
        ]
    };
}

/// <summary>
/// A manual, made of the sections it documents.
/// </summary>
[ExcludeFromCodeCoverage]
public class HelpDocument
{
    /// <summary>
    /// The text shown before the sections.
    /// </summary>
    public string Preamble { get; init; } = string.Empty;
    /// <summary>
    /// The sections, in the order they are shown.
    /// </summary>
    public required List<HelpSection> Sections { get; init; }
}

/// <summary>
/// One section of the manual, i.e. a group of related items.
/// </summary>
[ExcludeFromCodeCoverage]
public record class HelpSection
{
    /// <summary>
    /// The name of the section.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// What the section covers.
    /// </summary>
    public string Description { get; init; } = string.Empty;
    /// <summary>
    /// The items the section documents.
    /// </summary>
    public required List<HelpItem> Items { get; init; }
    /// <summary>
    /// The words a search matches the section by, beyond its name.
    /// </summary>
    public List<string> Tags { get; init; } = [];
}

/// <summary>
/// One item of the manual, i.e. one operator, constructor or command.
/// </summary>
[ExcludeFromCodeCoverage]
public record class HelpItem
{
    /// <summary>
    /// The name of the item.
    /// </summary>
    public required string Name { get; init; }
    /// <summary>
    /// The ways it can be written.
    /// </summary>
    public required List<string> Formats { get; init; }
    /// <summary>
    /// What it does, in one line.
    /// </summary>
    public required string Description { get; init; }
    /// <summary>
    /// What it does at length, shown when the item alone is asked for.
    /// </summary>
    public string LongDescription { get; init; } = string.Empty;
    /// <summary>
    /// Examples of it in use.
    /// </summary>
    public string Examples { get; init; } = string.Empty;
    /// <summary>
    /// The words a search matches the item by, beyond its name.
    /// </summary>
    public List<string> Tags { get; init; } = [];
}