namespace Unipi.Nancy.Playground.Cli;

public static class NancyPlaygroundDocs
{
    public static HelpDocument HelpDocument = new()
    {
        Preamble = """
Here are the supported constructs by the MPPG syntax.
The goal is to support as many constructs as possible to run existing code, and to optionally extend the syntax where useful.

WARNING: the initial contents of this help page was written by AI, using the syntax.md document as source.
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
                        Format = "!help [query]",
                        Description = "Shows this help text, or a search result. Useful reference for the syntax to use in scripts.",
                        Tags = ["help", "manual", "documentation"]
                    },
                    new HelpItem
                    {
                        Name = "CLI Help",
                        Format = "!clihelp",
                        Description = "Shows the CLI help text. Useful reference for commands and options of this app.",
                        Tags = ["cli", "help", "manual", "documentation"]
                    },
                    new HelpItem
                    {
                        Name = "Quit",
                        Format = "!quit | !exit",
                        Description = "Terminates the program.",
                        Tags = ["quit", "exit", "terminate"]
                    },
                    new HelpItem
                    {
                        Name = "Export",
                        Format = "!export <output-file>",
                        Description = "Exports the current program to a .mppg file.",
                        Tags = ["export", "save", "file"]
                    },
                    new HelpItem
                    {
                        Name = "Convert",
                        Format = "!convert <output-file>",
                        Description = "Converts the current program to a Nancy C# program and saves it to a file.",
                        Tags = ["convert", "nancy", "csharp", "file"]
                    }
                ]
            },
            new HelpSection
            {
                Name = "Comments",
                Description = "Line and inline comments that are ignored by the interpreter.",
                Tags = ["comments"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "Line comments",
                        Format = "// text | % text | # text | > text",
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
                        Format = "expression // text | expression % text | expression # text",
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
                Tags = ["types", "kinds"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "Function and scalar values",
                        Format = "function | scalar",
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
                Tags = ["variables", "declaration", "assignment", "names"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "Assignment",
                        Format = "name := expression",
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
                Tags = ["functions", "constructors", "service-curves", "arrival-curves", "shapes", "builtins"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "ratency",
                        Format = "ratency(a, b)",
                        Description = "Rate-latency service function with rate a ≥ 0 and latency b ≥ 0.",
                        LongDescription = """
Constructs a rate-latency service curve:
- Parameter `a`: rate (slope), must be ≥ 0
- Parameter `b`: latency (horizontal shift), must be ≥ 0
""",
                        Tags = ["ratency", "service-curve", "rate-latency"]
                    },
                    new HelpItem
                    {
                        Name = "bucket",
                        Format = "bucket(a, b)",
                        Description = "Leaky bucket arrival function with slope a ≥ 0 and constant b ≥ 0.",
                        LongDescription = """
Constructs a leaky bucket arrival curve:
- `a` is the sustained arrival rate (slope)
- `b` is the burst size (vertical offset)
""",
                        Tags = ["bucket", "arrival-curve", "leaky-bucket", "sigma-rho"]
                    },
                    new HelpItem
                    {
                        Name = "affine",
                        Format = "affine(a, b)",
                        Description = "Affine function with slope a and constant b. Right-continuous at x = 0.",
                        LongDescription = """
Constructs an affine function f(x) = a·x + b.
The function is right-continuous at 0: f(0+) = f(0).
""",
                        Tags = ["affine", "linear"]
                    },
                    new HelpItem
                    {
                        Name = "step",
                        Format = "step(o, h)",
                        Description = "Step function with step at time o and height h.",
                        Tags = ["step", "step-function"]
                    },
                    new HelpItem
                    {
                        Name = "stair",
                        Format = "stair(o, l, h)",
                        Description = "Staircase function with first step at time o, length l, and step height h.",
                        Tags = ["stair", "staircase", "piecewise"]
                    },
                    new HelpItem
                    {
                        Name = "delay",
                        Format = "delay(o)",
                        Description = "Burst-delay function that occurs at time o.",
                        Tags = ["delay", "burst-delay"]
                    },
                    new HelpItem
                    {
                        Name = "zero",
                        Format = "zero",
                        Description = "Zero function: f(x) = 0 for x ≥ 0.",
                        Tags = ["zero", "zero-function", "constant"]
                    },
                    new HelpItem
                    {
                        Name = "epsilon",
                        Format = "epsilon",
                        Description = "Epsilon function: f(x) = +∞ for x ≥ 0.",
                        Tags = ["epsilon", "infinity", "constant"]
                    }
                ]
            },

            new HelpSection
            {
                Name = "Function constructors: arbitrary shapes",
                Description = "Ultimately affine and ultimately pseudo-periodic functions built from segments.",
                Tags = ["constructors", "segments", "uaf", "upp", "piecewise", "pseudo-periodic"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "Segments",
                        Format = "[(x1, y1)s(x2, y2)] and variants",
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
                        Format = "uaf(SEGMENT+)",
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
                        Tags = ["uaf", "ultimately-affine", "segments", "piecewise"]
                    },
                    new HelpItem
                    {
                        Name = "Ultimately Pseudo-Periodic functions",
                        Format = "upp([finiteSegments,] period(periodicSegments) [, incr[, period]])",
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
                        Tags = ["upp", "ultimately-pseudo-periodic", "periodic", "segments"]
                    }
                ]
            },

            new HelpSection
            {
                Name = "Scalar values",
                Description = "Number syntax and allowed literals.",
                Items =
                [
                    new HelpItem
                    {
                        Name = "Number syntax",
                        Format = "integer | rational | ±inf | ±infinity",
                        Description = "Scalars are rationals plus ±infinity.",
                        LongDescription = """
Supported numeric literals:
- Integers: `0`, `1`, `-3`
- Rationals: `3/2`
- Positive infinity: `+inf`, `+infinity`
- Negative infinity: `-inf`, `-infinity`

Implementation ignores any floating-point variant and focuses on rationals.
""",
                        Examples = """
0
1
-3
3/2
+inf
-inf
+infinity
-infinity
""",
                        Tags = ["scalars", "numbers", "rational", "infinity"]
                    }
                ]
            },

            new HelpSection
            {
                Name = "Function-returning operations",
                Description = "Operations that take functions (and possibly scalars) and return functions.",
                Tags = ["functions", "operations"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "Pointwise min/max",
                        Format = "f1 ∧ f2 | f1 ∨ f2",
                        Description = "Pointwise minimum or maximum of two functions.",
                        LongDescription = """
- `f1 ∧ f2`: pointwise minimum of f1 and f2.
- `f1 ∨ f2`: pointwise maximum of f1 and f2.
""",
                        Tags = ["min", "max"]
                    },
                    new HelpItem
                    {
                        Name = "Addition and subtraction (functions)",
                        Format = "f1 + f2 | f1 - f2",
                        Description = "Pointwise sum or difference of two functions.",
                        Tags = ["addition", "subtraction"]
                    },
                    new HelpItem
                    {
                        Name = "(min,+) convolution",
                        Format = "f1 * f2 | f1 *_ f2",
                        Description = "(min,+) convolution of f1 and f2.",
                        LongDescription = """
Both `*` and `*_` denote the (min,+) convolution. They are aliases.
""",
                        Tags = ["convolution", "min-plus"]
                    },
                    new HelpItem
                    {
                        Name = "(max,+) convolution",
                        Format = "f1 *^ f2",
                        Description = "(max,+) convolution of f1 and f2.",
                        Tags = ["convolution", "max-plus"]
                    },
                    new HelpItem
                    {
                        Name = "(min,+) deconvolution",
                        Format = "f1 / f2 | f1 /_ f2",
                        Description = "(min,+) deconvolution of f1 by f2.",
                        Tags = ["deconvolution", "min-plus"]
                    },
                    new HelpItem
                    {
                        Name = "(max,+) deconvolution",
                        Format = "f1 /^ f2",
                        Description = "(max,+) deconvolution of f1 by f2.",
                        Tags = ["deconvolution", "max-plus"]
                    },
                    new HelpItem
                    {
                        Name = "Subadditive closure",
                        Format = "star(f)",
                        Description = "Subadditive closure of f.",
                        Tags = ["closure", "subadditive", "star"]
                    },
                    new HelpItem
                    {
                        Name = "Horizontal shift",
                        Format = "hShift(f, n) | hshift(f, n)",
                        Description = "Function identical to f but shifted horizontally by n.",
                        LongDescription = """
- Positive n: shift to the right.
- Negative n: shift to the left.

Both `hShift` and `hshift` are accepted spellings.
""",
                        Tags = ["shift", "horizontal", "traslation"]
                    },
                    new HelpItem
                    {
                        Name = "Vertical shift",
                        Format = "vShift(f, n) | vshift(f, n)",
                        Description = "Function identical to f but shifted vertically by n.",
                        Tags = ["shift", "vertical", "traslation"]
                    },
                    new HelpItem
                    {
                        Name = "Pseudo-inverse (lower and upper)",
                        Format = "inv(f) | low_inv(f) | up_inv(f)",
                        Description = "Lower and upper pseudo-inverses of f.",
                        LongDescription = """
- `inv(f)` and `low_inv(f)`: lower pseudo-inverse.
- `up_inv(f)`: upper pseudo-inverse.
""",
                        Tags = ["pseudo-inverse", "inverse", "lower", "upper"]
                    },
                    new HelpItem
                    {
                        Name = "Upper closure",
                        Format = "upclosure(f) | nnupclosure(f, n)",
                        Description = "Upper non-decreasing closure (optionally non-negative).",
                        LongDescription = """
- `upclosure(f)`: upper non-decreasing closure of f.
- `nnupclosure(f, n)`: non-negative upper non-decreasing closure, parameterized by n.
""",
                        Tags = ["closure", "non-decreasing", "upper"]
                    },
                    new HelpItem
                    {
                        Name = "Composition",
                        Format = "f comp g",
                        Description = "Composition of functions: (f ∘ g)(x) = f(g(x)).",
                        Tags = ["composition"]
                    },
                    new HelpItem
                    {
                        Name = "Left/right extensions",
                        Format = "left-ext(f) | right-ext(f)",
                        Description = "Left- and right-continuous extensions of f.",
                        LongDescription = """
- `left-ext(f)`: g(x) = f(x⁻)
- `right-ext(f)`: g(x) = f(x⁺)
""",
                        Tags = ["extensions", "left-continuous", "right-continuous"]
                    },
                    new HelpItem
                    {
                        Name = "Scaling by a scalar",
                        Format = "scalar * f | f * scalar | f / scalar",
                        Description = "Multiply or divide a function by a scalar.",
                        Tags = ["scaling", "multiplication", "division"]
                    }
                ]
            },

            new HelpSection
            {
                Name = "Scalar-returning operations on functions",
                Description = "Operations that take functions (or functions and scalars) and return a scalar.",
                Tags = ["functions", "scalars", "operations", "evaluation", "deviation"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "Evaluation",
                        Format = "f(x) | f(x+) | f(x-) | f(x~+) | f(x~-)",
                        Description = "Evaluates function f at or around a point x.",
                        LongDescription = """
- `f(x)`: value of f at x.
- `f(x+)` / `f(x~+)`: right-limit of f at x.
- `f(x-)` / `f(x~-)`: left-limit of f at x.

Both `f(x+)`/`f(x-)` and `f(x~+)`/`f(x~-)` are supported.
""",
                        Tags = ["functions", "evaluation", "limits", "right-limit", "left-limit", "scalars"]
                    },
                    new HelpItem
                    {
                        Name = "Horizontal deviation",
                        Format = "hDev(f, g) | hdev(f, g)",
                        Description = "Horizontal deviation between f and g.",
                        Tags = ["functions", "deviation", "horizontal", "hDev", "metrics"]
                    },
                    new HelpItem
                    {
                        Name = "Vertical deviation",
                        Format = "vDev(f, g) | vdev(f, g)",
                        Description = "Vertical deviation between f and g.",
                        Tags = ["functions", "deviation", "vertical", "vDev", "metrics"]
                    },
                    new HelpItem
                    {
                        Name = "Max backlog period length",
                        Format = "maxBacklogPeriod(f, g)",
                        Description = "Max backlog period length between f and g. (Not implemented)",
                        Tags = ["functions", "backlog", "period", "metrics", "not-implemented"]
                    }
                ]
            },

            new HelpSection
            {
                Name = "Scalar operations",
                Description = "Operations between scalars returning scalars.",
                Tags = ["scalars", "operations", "arithmetic", "min", "max"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "Min/max",
                        Format = "v1 /\\ v2 | v1 \\/ v2",
                        Description = "Minimum or maximum of two scalar values.",
                        LongDescription = """
- `v1 /\\ v2`: minimum of v1 and v2.
- `v1 \\/ v2`: maximum of v1 and v2.
""",
                        Tags = ["scalars", "min", "max", "comparison", "operations"]
                    },
                    new HelpItem
                    {
                        Name = "Arithmetic",
                        Format = "v1 + v2 | v1 - v2 | v1 * v2 | v1 ÷ v2 | v1 div v2",
                        Description = "Standard scalar arithmetic operations.",
                        LongDescription = """
- `v1 + v2`: addition
- `v1 - v2`: subtraction
- `v1 * v2`: multiplication
- `v1 ÷ v2`: division
- `v1 div v2`: division (same semantics for this syntax)
""",
                        Tags = ["scalars", "arithmetic", "addition", "multiplication", "division", "operations"]
                    }
                ]
            },

            new HelpSection
            {
                Name = "Output",
                Description = "Rules for console output of expressions and variables.",
                Tags = ["output", "printing", "console", "variables", "display"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "Expression output",
                        Format = "expression",
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
                        Format = "name := expression",
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
                        Format = "name",
                        Description = "Typing the name of a variable prints its content.",
                        LongDescription = """
- If the variable holds a function, its value is printed as a `uaf(...)` or `upp(...)` definition, regardless of the original constructor.
- If the variable holds a scalar, the scalar value is printed.
""",
                        Tags = ["output", "variables", "printing", "console"]
                    }
                ]
            },

            new HelpSection
            {
                Name = "Plots",
                Description = "Plotting functions using plot(f1, f2, ..., args).",
                Tags = ["plots", "graph", "visualization", "plot", "functions"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "plot",
                        Format = "plot(f1, f2, ..., args)",
                        Description = "Plots one or more function variables with optional configuration arguments. (partially)",
                        LongDescription = """
General form:
- `plot(f1, f2, ..., args)`

Notes:
- Functions must be variables (not inline expressions).
- Args and function names can appear in any order.
- Args can be numbers, intervals, or strings (possibly composed via sums).
- `gui` behaves differently depending on rendering backend.

Supported args:
- `main`: graph title.
- `title`: alias for `main`.
- `xlim=[min, max]`: x-axis range.
- `ylim=[min, max]`: y-axis range.
- `xlab="text"`: label for x-axis.
- `ylab="text"`: label for y-axis.
- `out="file.png"`: save to PNG file.
- `grid="no"`: disable grid. (Not implemented)
- `bg="no"`: white background instead of grey. (Not implemented)
- `gui="no"`: custom flag to enable/skip GUI rendering.
""",
                        Examples = """
plot(f1)
plot(f1, f2)
plot(service2, service1, xlim=[-0.3, 15], ylim=[-0.3, 15])
plot(f1, main="f1 for J=" +J +"Jitter", xlim=[-0.5, 5], xlab="time", ylab="packets", out="image.png")
plot(xlim=[-0.3, 15], ylim=[-0.3, 15], service2, service1)
""",
                        Tags = ["plots", "plot", "graph", "visualization", "xlim", "ylim", "xlab", "ylab", "gui", "out"]
                    }
                ]
            },

            new HelpSection
            {
                Name = "Asserts",
                Description = "Relational checks between functions and/or scalars.",
                Tags = ["assert", "assertion", "checks", "relations", "constraints"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "assert",
                        Format = "assert(f OP g)",
                        Description = "Tests a relation between two expressions; prints true or an error message.",
                        LongDescription = """
General form:
- `assert(f OP g)`

Where:
- f and g can be variable names or expressions
- They can evaluate to either functions or scalars

Semantics:
- For two functions: relation must hold for all t: f(t) OP g(t).
- For function vs scalar c: relation must hold for all t: f(t) OP c.

Supported operators:
- `=`
- `!=`
- `<=`
- `>=`

If the assertion holds, prints `true`.
Otherwise, prints `assertion failed` with an explanation.

If the syntax is unsupported or too complex, prints `-1`.
""",
                        Examples = """
assert(f <= g)
assert(h != zero)
""",
                        Tags = ["assert", "assertion", "comparison", "constraints", "checks", "relations"]
                    }
                ]
            },

            new HelpSection
            {
                Name = "New shiny syntax",
                Description = "Extra helper constructs beyond the original syntax.",
                Tags = ["syntax", "extras", "helpers", "extensions", "printExpression"],
                Items =
                [
                    new HelpItem
                    {
                        Name = "printExpression",
                        Format = "printExpression(f)",
                        Description = "Prints out the expression of f, rather than its canonical uaf/upp form.",
                        LongDescription = """
Useful to inspect the original expression used to define a function variable, instead of its normalized representation.
""",
                        Tags = ["printExpression", "expression", "debugging", "output", "syntax"]
                    }
                ]
            }
        ]
    };
}

public class HelpDocument
{
    public string Preamble { get; init; } = string.Empty;
    public required List<HelpSection> Sections { get; init; }
}

public record class HelpSection
{
    public required string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public required List<HelpItem> Items { get; init; }
    public List<string> Tags { get; init; } = [];
}

public record class HelpItem
{
    public required string Name { get; init; }
    public required string Format { get; init; }
    public required string Description { get; init; }
    public string LongDescription { get; init; } = string.Empty;
    public string Examples { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = [];
}