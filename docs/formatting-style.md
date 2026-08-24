# MPPG Formatting Style

The formatting style used here: by the statement echo of `nancy-playground`, and by the MPPG rendering
of Nancy values.

| # | Construct | Prescription | Examples |
| ---- | ---- | ---- | ---- |
| 1 | Assignment | Spaced around `:=` | `f := ratency(1, 3)` |
| 2 | Constructor | Call: no space before or inside the parentheses, one space after each comma | `bucket(2, 1)`, `stair(1, 2, 3)`, `zero`, `epsilon` |
| 3 | Function operation | Call | `star(f)`, `hShift(f, 2)`, `left-ext(f)`, `nnupclosure(f)` |
| 4 | Scalar operation | Call | `abs(-x)`, `pow(x, 2)`, `gcd(x, y)` |
| 5 | Sampling | Call | `f(2)`, `f(2+)`, `f(2~-)` |
| 6 | Binary operator | Spaced | `f /\ g`, `f *^ g`, `f comp g`, `x div y`, `"a" + "b"` |
| 7 | Unary sign | Tight against its operand | `-f`, `+inf`, `-(x + y)` |
| 8 | Number | As written | `2`, `3/2`, `0.25`, `+inf` |
| 9 | Division | Spaced, being an operator, unless it is inside a rational literal, which is one value | `x / y`, `f / x`, `1/2`, `-3/2` |
| 10 | Grouping brackets | Tight | `(f + g) * x` |
| 11 | Compound operand | Parenthesized, making the grouping explicit | `f + (x * y)`, `(f * g) * h` |
| 12 | Endpoint | A pair, written like an argument list | `(0, -3)` |
| 13 | Spot | Brackets tight against the endpoint | `[(0, 0)]` |
| 14 | Segment | Brackets tight against the endpoints, the slope spaced between them | `[(0, 0) 1 (1, 1)]`, `](1, 1) 0 (+inf, 1)[`, `[(0, 0) (1, 1)[` |
| 15 | Sequence | Segments separated by one space | `uaf([(0, -3) 1 (1, -2)[ [(1, -2) 0 (+inf, -2)[)` |
| 16 | Pseudo-periodic function | Call, the period and the increment being arguments | `upp(period([(0, 0) 0 (2, 0)[), 1/2, 1)` |
| 17 | Assertion | Call around a spaced comparison | `assert(f * g = g * f)`, `assert(g(0) >= 0)` |
| 18 | Print | Call | `printExpression(f)` |
| 19 | Plot | Call, each option being a tight binding | `plot(f, g, out="p.png", xlim=[0, 10])`, `plotTikz(f)` |
| 20 | Comment | As written, one space after the statement it follows | `f := zero // as written` |
| 21 | Directive | As written | `#!syntax version 1.3` |

## Grouping

Expressions are parsed by the [precedence rules](/docs/syntax.md#function-operator-precedence) of the
syntax, and rule 11 then spells out the grouping that was read.
The parentheses are added when printing, not expected in the input: `f - x + y` comes back as
`(f - x) + y`, and `f + x * y` as `f + (x * y)`.

## Differences from the original

The [source material](https://www.realtimeatwork.com/minplus-console/RTaW-MinplusConsole-UserManual.pdf)
writes a segment with no spaces, as `[(x1,y1)slope(x2,y2)[`.
Rule 14 spaces it, so that an endpoint follows the same comma as any other pair of arguments.
