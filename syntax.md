# MPPG Syntax

Here are the supported constructs by the _MPPG_ syntax.

I aim to support as many constructs as possible, to run all existing code, but also extend it when useful.

> The extensions should be made optional, if possible.

This syntax is re-organized, compared to the source material [[1]](https://www.realtimeatwork.com/minplus-quickref-syntax/)[[2]](https://www.realtimeatwork.com/minplus-console/RTaW-MinplusConsole-UserManual.pdf), to better guide implementation.

Spaces and tabs are ignored.
A newline terminates a statement, so there is one statement per line.

## Comments ✅

### Line comments

Lines that start with `//`, `%`, `#` or `>` are comments and are ignored.

```
// This is a comment
% This is also a comment
# This is a comment as well
> This is a comment as well
```

> Fun thing, `%`, `#` and `>` are not documented.
> They are used heavily, for example, in the PhD Thesis of Guidolin--Pina.

### Inline comments

Statements may end with comments that start with `//`, `%` or `#`.
Inline comments cannot start with `>`.

```
f := ... // This is a comment
g := ... % This is also a comment
h := ... # This is a comment as well
```

## Types ✅

The syntax supports _function_ and _scalar_ values.

> _Function_ is how MPPG names _curves_.

## Variable declaration ✅

Functions and scalars can be given a name and recalled later using the `:=` syntax.

```
f := ...
```

Variable references must point to variables that were already declared in the current script or interactive session.
Forward references and unknown names are rejected by the parser instead of being guessed as either scalars or functions.

## Function constructors

### Known functions

Here is the information in a markdown table format:

| Expression | Description | Implemented |
|----|----|----|
| ratency(a,b) | Constructs a rate-latency service function with rate $a \geq 0$ and latency $b \geq 0$. | ✅ |
| bucket(a,b)| Constructs a leaky bucket arrival function with slope $a \geq 0$ and constant $b \geq 0$. | ✅ |
| affine(a,b)| Constructs an affine function with slope $a$ and constant $b$. The function is right-continuous at $x=0$, i.e., $f(0+)=f(0)$. | ✅ |
| step(o,h) | Constructs a step function with the step occurring at time $o$ and height $h$. The step is left-continuous. | ✅ |
| stair(o,l,h)| Constructs a staircase function with the first step at time $o$, length $l$, and height $h$. It is left continuous. | ✅ |
| delay(o) | Constructs a burst-delay function that occurs at time $o$. | ✅ |
| zero | Constructs a function that has zero as its value everywhere: $f(x)=0$ for $x \geq 0$. | ✅ |
| epsilon | Constructs the "epsilon" function: $f(x)=+\infty$ for $x \geq 0$. | ✅ |

### Arbitrarily-shaped functions ✅

A more general syntax is available to define functions that do not fit in the shapes above.
One can use `uaf` to define Ultimately Affine functions, and `upp` to define Ultimately Pseudo-Periodic functions.

Both are built using _segments_.

#### Segments 🟨

> Despite the name, they sound more like _elements_ of Nancy.

| Expression | Description | Implemented |
|----|----|----|
| [(x, y)] | A _spot_ in $(x, y)$ | ✅ |
| [(x1, y1)slope(x1, y1)] | A segment from $(x1, y1)$ to $(x2, y2)$, with the given slope. The right spot is included. | ✅ |
| [(x1, y1)slope(x1, y1)[ | A segment from $(x1, y1)$ to $(x2, y2)$, with the given slope. The right spot is not included. | ✅ |
| ](x1, y1)slope(x1, y1)] | A segment from $(x1, y1)$ to $(x2, y2)$, with the given slope. The left spot is not included, but right one is. | ✅ |
| [(x1, y1)(x1, y1)[ | A segment from $(x1, y1)$ to $(x2, y2)$. The slope is automatically computed. | ✅ |

The docs claim "x and y could be any number, or +inf, +infinity, -inf,
-infinity", which opens to _a lot_ of edge cases and uncertainty.

The end value of a segment is used for consistency checks, even for a right-open segment.

#### Ultimately Affine functions ✅

```
uaf(SEGMENT+) 
```

At least one segment is required, and the last segment must extend to $+\infty$.
Example:

```markdown
uaf( [(0,-3)1(1,-2)[ [(1,-2)2(7,10)[ [(7,10)0(+inf,10)[ )
```

> The following does not work and I don't know why:
> ```
> uaf( [(0,-3)1(1,-2)[ [(1,-2)2(7,10)[ [(7,10)1(+inf,+inf)[ )
> ```

#### Ultimately Pseudo-Periodic functions ✅

```
upp([SEGMENT*,] period(SEGMENT*) [, incr[,period]])
```

> Construct an ultimately pseudo-periodic function.
> The * means optional. "period" is a mandatory field. 
> First segment list is the finite part. 
> The second part is the pseudo-periodic part. 
> Both `incr` and `period` are optional.

`incr` is authoritative: giving a height other than the one the periodic part implies produces a different curve.
`period` is informational, and a length other than the one the periodic part implies is ignored.

Unlike the endpoints and slopes of the segments, which are full [number expressions](#number-syntax), `incr` and `period` are literals: an integer, a decimal, an infinity, or a fraction of two of these.

##### Examples

```
upp( period( [(0, 0) 0 (2, 0)[ [(2, 0) 1 (7, 5)] ](7, 5) 0 (12, 5)[ ))
```

```
upp( [(0, +Infinity) 0 (6, +Infinity)], period (](6, 0) 0 (10.5, 0)[ [(10.5, +Infinity) 0 (18, +Infinity)]), 0, 12)
```

```
upp( period( [(0, 0)] ](0, 0) 0 (1, 0)[ ), 1/2, 1)
```

## Scalar values ✅

### Number syntax

Numbers are rationals. 

> There is an implementation using floats, we will ignore that

They can be written in 4 notations:
    - as integers (`0`, `1`, `-3`)
    - as rationals (`3/2`, `-2/3`)
    - as decimals (`0.25`)
    - $\pm\infty$ (`+inf`, `-inf`, `+infinity`, `-infinity`)

## Function-returning operations

These operations return a _function_.

| Expression | Description | Implemented |
|----|----|----|
| f1 ∧ f2 | Minimum of $f_1$ and $f_2$. | ✅ |
| f1 ∨ f2 | Maximum of $f_1$ and $f_2$. | ✅ |
| f1 + f2 | Sum of $f_1$ and $f_2$. | ✅ |
| f1 - f2 | Subtraction of $f_2$ from $f_1$. | ✅ |
| f1 * f2 | (min,+) convolution of $f_1$ and $f_2$. | ✅ |
| f1 *_ f2 | (min,+) convolution of $f_1$ and $f_2$. | ✅ |
| f1 *^ f2 | (max,+) convolution of $f_1$ and $f_2$. | ✅ |
| f1 / f2 | (min,+) deconvolution of  $f_1$ and $f_2$. | ✅ |
| f1 /_ f2 | (min,+) deconvolution of  $f_1$ and $f_2$. | ✅ |
| f1 /^ f2 | (max,+) deconvolution of  $f_1$ and $f_2$. | ✅ |
| star(f) | Subadditive closure of $f$. | ✅ |
| subaddclosure(f) | Subadditive closure of $f$. Requires syntax version 1.2 or later. | ✅ |
| superaddclosure(f) | Superadditive closure of $f$. Requires syntax version 1.2 or later. | ✅ |
| hShift(f, n) | Compute the function identical to $f$ but horizontally shifted by $n$. | ✅ |
| hshift(f, n) | Compute the function identical to $f$ but horizontally shifted by $n$. | ✅ |
| vShift(f, n) | Compute the function which is identical to $f$ but vertically shifted by $n$. | ✅ |
| vshift(f, n) | Compute the function which is identical to $f$ but vertically shifted by $n$. | ✅ |
| inv(f) | Compute the _lower_ pseudo-inverse of $f$. | ✅ |
| low_inv(f) | Compute the _lower_ pseudo-inverse of $f$. | ✅ |
| up_inv(f) | Compute the _upper_ pseudo-inverse of $f$. | ✅ |
| upclosure(f) | Compute the _upper_ non-decreasing closure of $f$. | ✅ |
| nnupclosure(f,n ) | Compute the non-negative _upper_ non-decreasing closure of $f$. | ✅ |
| lowclosure(f) | Compute the _lower_ non-decreasing closure of $f$. | ✅ |
| nnlowclosure(f) | Compute the non-negative _lower_ non-decreasing closure of $f$. | ✅ |
| floor(f) | Compute the function $g$ such that $g(x) = \lfloor f(x) \rfloor$. Requires syntax version 1.3 or later. | ✅ |
| ceil(f) | Compute the function $g$ such that $g(x) = \lceil f(x) \rceil$. Requires syntax version 1.3 or later. | ✅ |
| f comp g | Compute the composition of $f$ and $g$, i.e. $f(g(x))$ | ✅ |
| left-ext(f) | Left-continuous projection, i.e., the function $g$ such that for all $x$, $g(x) = f(x^-)$. | ✅ |
| right-ext(f) | Right-continuous projection, i.e., the function $g$ such that for all $x$, $g(x) = f(x^+)$. | ✅ |
| scalar * f | Function multiplication by a scalar value. | ✅ |
| f * scalar | Function multiplication by a scalar value. | ✅ |
| f / scalar | Function division by a scalar value. | ✅ |

> `hShift` and `hshift` are both fine, like `vShift` and `vshift`.
> Fun thing: this is not documented, but used heavily in the PhD Thesis of Guidolin--Pina.

### Function operator precedence

Operators are parsed by the type of the expression they return.
Mixed scalar/function operators therefore parse as function expressions when their result is a function.
For example, `x + f`, `f(x) * g`, and `f comp x` are function expressions, while `f(x)` and `f(x) + x` are scalar expressions.

The supported precedence order is:

1. Function sampling and unary operators.
2. Product/composition operators: `*`, `/`, `*_`, `*^`, `/_`, `/^`, and `comp`, evaluated left-to-right.
3. Sum/min/max operators: `+`, `-`, `/\`, and `\/`, evaluated left-to-right.

Thus `f comp g * x` is parsed as `(f comp g) * x`, and `f * x comp g` is parsed as `(f * x) comp g`.

#### Scalar operands of the mixed operators

The scalar side of `+`, `-`, `/\` and `\/` is a whole product of scalars, so `f + 1/2` shifts by one half and `f + x * y` shifts by the product.
It stops short of the sum operators, so `f - x + y` is `(f - x) + y`, not `f - (x + y)`.

The scalar side of `*`, `/` and `comp` is one value at a time, and a chain of them folds left to right, exactly as it does between scalars.
So `f / 1/2` is `(f / 1) / 2`, the same grouping that `a / 1/2` has when `a` is a scalar.

Some mixed scalar/function forms are Nancy extensions beyond the subset that RTaW computes successfully.
For example, `f(x) comp g` is accepted as a mixed scalar/function composition and returns a function.
For syntax that RTaW computes successfully, the implementation is expected to match RTaW behavior.

##### Division edge cases and divergence from RTaW

RTaW groups divisions differently based on the type of the dividend:
- a divisor that *starts with a number* is read as one whole value, e.g. it reads `f / 1/2` as `f / (1/2)`
- a divisor that instead starts with a variable, sampled value, etc. is folded left, e.g. it reads `f / x/y` as `(f / x) / y`.

`nancy-playground` instead will always fold left. 
This is internally coherent but may lead to unexpected different results w.r.t. RTaW.
For this reason, a `WARNING` recommending explicit parentheses will be printed whenever an expression like `f / 1 / 2` is used. 

## Scalar-returning operations

These operations work on functions, but return scalars.

| Expression | Description | Implemented |
|----|----|----|
| f(x) | Value of f at x | ✅ |
| f(x+) | Value of f at the right of x | ✅ |
| f(x-) | Value of f at the left of x | ✅ |
| f(x~+) | Value of f at the right of x | ✅ |
| f(x~-) | Value of f at the left of x | ✅ |
| hDev(f, g) | Horizontal deviation between $f$ and $g$. | ✅ |
| hdev(f, g) | Horizontal deviation between $f$ and $g$. | ✅ |
| vDev(f, g) | Vertical deviation between $f$ and $g$. | ✅ |
| vdev(f, g) | Vertical deviation between $f$ and $g$. | ✅ |
| zDev(f, g) | Z-deviation between $f$ and $g$, for delay bounds with negative service curves. | ✅ |
| maxBacklogPeriod(f, g) | Max backlog period length between $f$ and $g$. | ❌ |

> The [syntax quick reference](https://www.realtimeatwork.com/minplus-quickref-syntax/), 
> mentions the syntax `f(x+)` and `f(x-)` that do not work in the [online playground](https://www.realtimeatwork.com/minplus-playground).
> It is instead `f(x~+)` and `f(x~-)`.
> `nancy-playground`, for good measure, supports both.

> `hDev` and `hdev` are both fine, like `vDev` and `vdev`.
> Fun thing: this is not documented, but used heavily in the PhD Thesis of Guidolin--Pina.

## Operations _between_ scalars

These operations work between scalars, and return scalars.

| Expression | Description | Implemented |
|----|----|----|
| v1 /\ v2 | Minimum of v1 and v2. | ✅ |
| v1 \/ v2 | Maximum of v1 and v2. | ✅ |
| v1 + v2 | Sum of v1 and v2. | ✅ |
| v1 - v2 | Substraction of v1 and v2. | ✅ |
| v1 * v2 | Multiplication of v1 and v2. | ✅ |
| v1 ÷ v2 | Division of v1 and v2. | ✅ |
| v1 div v2 | Division of v1 by v2. | ✅ |
| v1 mod v2 | Remainder of v1 divided by v2, which takes the sign of v1. Requires syntax version 1.3 or later. | ✅ |
| floor(v) | Largest integer not above $v$, i.e. $\lfloor v \rfloor$. Requires syntax version 1.3 or later. | ✅ |
| ceil(v) | Smallest integer not below $v$, i.e. $\lceil v \rceil$. Requires syntax version 1.3 or later. | ✅ |
| abs(v) | Absolute value of $v$. Requires syntax version 1.3 or later. | ✅ |
| pow(v, n) | $v$ raised to $n$, which must be an integer. Requires syntax version 1.3 or later. | ✅ |
| gcd(v1, v2) | Greatest common divisor of $v_1$ and $v_2$. Requires syntax version 1.3 or later. | ✅ |
| lcm(v1, v2) | Least common multiple of $v_1$ and $v_2$. Requires syntax version 1.3 or later. | ✅ |

> `floor` and `ceil` return the kind of their argument: `floor(f)` is a function, `floor(3/2)` is a scalar.
> Which one it is is decided by the argument, so `f * floor(2)` scales `f` by 2, while `f * floor(g)` is a
> convolution.


## Output ✅

Any operation that does not assign to a variable, prints its value to the console.

An assignment operation prints the name of the assigned variable to the console.

By typing the name of a variable, one can have its content printed to the console.

The value of a _function_ variable is its definition as `uaf` or `upp`, regardless of the constructor used.

## Plots 🟨

> Limited support. Can be parsed completely, but not all options actually affect the output.

`plot(f1, ..., args)`

Plot a graph displaying the functions `f1, f2, ...` 
`args` contains parameters for the drawing. 
Valid `args` are the following.

| Arg | Description | Implemented |
|----|----|----|
| `main` | The graph title. | ✅ |
| `title` | Custom option, alias for `main`. | ✅ |
| `xlim` | Range for x-axis. | ✅ |
| `ylim` | Range for y-axis. | ✅ |
| `xlab` | Label for x axis. | ✅ |
| `ylab` | Label for y axis. | ✅ |
| `out` | Name of png file to save plot to. The `.png` extension is enforced, so it can be omitted. | ✅ |
| `grid ="no"` | Remove grid from plot. | ❌ |
| `bg ="no"` | Use white background instead of default grey. | ❌ |
| `gui ="no"` | Custom option, skips showing the plot in a GUI window. Default: `"yes"`. | ✅ |

Notes: 
- functions must be variables, they cannot be expressions (e.g., sum of two functions);
- args can be numbers, intervals, string, or string with sum
of numbers, variables and strings for labels
- the bounds of the `xlim` and `ylim` intervals are literals, not expressions: an integer, a decimal, an infinity, or a fraction of two of these.
  Fractions, as in `xlim=[1/3, 10]`, are a `nancy-playground` addition: RTaW takes a variable there but not a fraction
- *not documented*: args and function names can appear in any order
- the `gui` option applies per plot, while the `--no-gui` option of the command line applies to the
whole run, overriding it. The image is written either way, and its path printed. `plotTikz` renders
code rather than an image, so neither has any effect there.

### Examples
- `plot(f1)`
- `plot(f1, f2)`
- `plot(service2,service1,xlim=[-0.3,15],ylim=[-0.3,15])`
- `plot(f1, main="f1 for J=" +J +"Jitter", xlim=[-0.5, 5], xlab="time", ylab="packets", out = "image.png")`
- `plot(xlim=[-0.3,15], ylim=[-0.3,15], service2, service1)`

## TikZ plots ✅

> Custom addition, requires syntax version 1.1 or later.

`plotTikz(f1, ..., args)`

Plot the functions `f1, f2, ...` as [TikZ](https://tikz.dev/) code, to be compiled with LaTeX, instead of an image.
The code is printed to the console, unless the `out` option is used to write it to file.

The `args` are the same as `plot`, with these differences.

| Arg | Description | Implemented |
|----|----|----|
| `main`, `title` | The graph title. Not supported by Nancy.Plots.Tikz. | ❌ |
| `xlab`, `ylab` | Labels for the axes. Nancy.Plots.Tikz always uses _time_ and _data_. | ❌ |
| `out` | Name of the file to save the TikZ code to. The `.tikz` extension is enforced, so it can be omitted, while `.tex` is also accepted. | ✅ |
| `gui ="no"` | No effect, as no GUI is used. | ❌ |

### Examples
- `plotTikz(f1)`
- `plotTikz(f1, f2)`
- `plotTikz(service2,service1,xlim=[-0.3,15],ylim=[-0.3,15])`
- `plotTikz(f1, main="f1 for J=" +J +"Jitter", xlab="time", ylab="packets", out = "plot.tikz")`

## Asserts

> This feature is not mentioned on the publicly available documentation.
> The behavior is therefore not well specified, I poked around.

The general form is `assert( f OP g )`, which tests relation `OP` between `f` and `g`.
If successful, outputs `true`. 
Otherwise, it outputs `assertion failed` followed by an explanation.

> If the assertion syntax is not supported, or "too complex to be understood", it outputs `-1`

Both sides can be either variable names, or expressions, which can evaluate to both function or number.
When comparing two functions, the relation is true if $f(t) OP g(t)$ is true for all $t$.
When comparing a function and a number, the relation is true if $f(t) OP g$ for all $t$ (i.e., as if $g$ was a constant function of that value).

The operators supported are `=`, `!=`, `<=`, and `>=`.

There seems _not_ to be any support for complex logic like `and`, `or` and `not`, or strict inequalities `<` and `>`.

> Note: some of the above restrictions may not be matched by `nancy-playground`, as detecting "too complex" expressions may be harder than just computing them.

## New shiny syntax

| Expression | Description | Implemented |
| ---- | ---- | ---- |
| printExpression(f) | Prints out the _expression_ of f. | ✅ |
| plotTikz(f1, ..., args) | Plots the functions as TikZ code, see [TikZ plots](#tikz-plots-). | ✅ |

### Syntax version

`#!syntax version X.Y`

Selects the syntax version used for the program, defaulting to the latest.
It is applied only as the first line of the program, and only once: any later directive is reported as a duplicate and ignored.

In interactive mode the same rule holds for the session, so that an exported session behaves the same when run again.
Use `!clear` to start a new session, and with it select a new version.

A keyword only acts as one from the version that introduced it: `lowclosure := 3` is an assignment under
`#!syntax version 1.0`, and the closure operator from 1.2 on.
Declaring a version therefore keeps a program working as later versions add keywords.

| Version | Keywords introduced |
| ---- | ---- |
| 1.1 | `printExpression`, `plotTikz` |
| 1.2 | `subaddclosure`, `superaddclosure`, `lowclosure`, `nnlowclosure` |
| 1.3 | `floor`, `ceil`, `abs`, `pow`, `mod`, `gcd`, `lcm` |

Scripts that name a variable `floor`, as the ones that spell the floor function
`right-ext(stair(1, 1, 1))` do, therefore need `#!syntax version 1.2` from 1.3 on.
A program that uses one of these names without declaring a version is told which name it is and which
directive keeps it.
