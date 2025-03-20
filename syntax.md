# MPPG Syntax

Here are the supported constructs by the _MPPG_ syntax.

I aim to support as many constructs as possible, to run all existing code, but also extend it when useful.

> The extensions should be made optional, if possible.

This syntax is re-organized, compared to the source material [[1]](https://www.realtimeatwork.com/minplus-quickref-syntax/)[[2]](https://www.realtimeatwork.com/minplus-console/RTaW-MinplusConsole-UserManual.pdf), to better guide implementation.

## Comments ✅

Lines that start with `//` are comments and are ignored.

```
// This is a comment
```

## Types ✅

The syntax supports _function_ and _scalar_ values.

> _Function_ is how MPPG names _curves_.

## Variable declaration ✅

Functions and scalars can be given a name and recalled later using the `:=` syntax.

```
f := ...
```

## Function constructors

### Known functions

Here is the information in a markdown table format:

| Expression | Description | Implemented |
|----|----|----|
| ratency(a,b) | Constructs a rate-latency service function with rate $a \geq 0$ and latency $b \geq 0$. | ✅ |
| bucket(a,b)| Constructs a leaky bucket arrival function with slope $a \geq 0$ and constant $b \geq 0$. | ✅ |
| affine(a,b)| Constructs an affine function with slope $a$ and constant $b$. The function is right-continuous at $x=0$, i.e., $f(0+)=f(0)$. | ✅ |
| step(o,h) | Constructs a step function with the step occurring at time $o$ and height $h$. | ❌ |
| stair(o,l,h)| Constructs a staircase function with the first step at time $o$, length $l$, and height $h$. | ✅ |
| delay(o) | Constructs a burst-delay function that occurs at time $o$. | ✅ |
| zero | Constructs a function that has zero as its value everywhere: $f(x)=0$ for $x \geq 0$. | ✅ |
| epsilon | Constructs the "epsilon" function: $f(x)=+\infty$ for $x \geq 0$. | ❌ |

### Arbitrarily-shaped functions ❌

A more general syntax is available to define functions that do not fit in the shapes above.
One can use `uaf` to define Ultimately Affine functions, and `upp` to define Ultimately Pseudo-Periodic functions.

Both are built using _segments_.

#### Segments ❌

> Despite the name, the sound more like _elements_ of Nancy.

| Expression | Description |
| ---------- | ----------- |
| [(x, y)] | A _spot_ in $(x, y)$ |
| [(x1, y1)slope(x1, y1)] | A segment from $(x1, y1)$ to $(x2, y2)$, with the given slope. The right spot is included. |
| [(x1, y1)slope(x1, y1)[ | A segment from $(x1, y1)$ to $(x2, y2)$, with the given slope. The right spot is not included. |
| ](x1, y1)slope(x1, y1)] | A segment from $(x1, y1)$ to $(x2, y2)$, with the given slope. The left spot is not included, but right one is. |
| [(x1, y1)(x1, y1)[ | A segment from $(x1, y1)$ to $(x2, y2)$. The slope is automatically computed. |

The docs claim "x and y could be any number, or +inf, +infinity, -inf,
-infinity", which opens to _a lot_ of edge cases and uncertainty.

The end value of a segment is used for consistency checks, even for a right-open segment.

#### Ultimately Affine functions ❌

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

#### Ultimately Pseudo-Periodic functions ❌

```
upp([SEGMENT*,] period(SEGMENT*) [, incr[,period]])
```

> Construct an ultimately pseudo-periodic function.
> The * means optional. "period" is a mandatory field. 
> First segment list is the finite part. 
> The second part is the pseudo-periodic part. 
> The increment is optional. 
> The period is purely informational.

## Scalar values ✅

### Number syntax

Numbers are rationals. 

> There is an implementation using floats, we will ignore that

Examples of valid syntax:
```
0
1
-3
3/2
+inf
-inf
+infinity
-infinity
```

## Function-returning operations

These operations return a _function_.

| Expression | Description | Implemented |
|----|----|----|
| f1 ∧ f2 | Minimum of $f_1$ and $f_2$. | ❌ |
| f1 ∨ f2 | Maximum of $f_1$ and $f_2$. | ❌ |
| f1 + f2 | Sum of $f_1$ and $f_2$. | ✅ |
| f1 - f2 | Subtraction of $f_2$ from $f_1$. | ✅ |
| f1 * f2 | (min,+) convolution of $f_1$ and $f_2$. | ✅ |
| f1 *_ f2 | (min,+) convolution of $f_1$ and $f_2$. | ✅ |
| f1 *^ f2 | (max,+) convolution of $f_1$ and $f_2$. | ✅ |
| f1 / f2 | (min,+) deconvolution of  $f_1$ and $f_2$. | ✅ |
| f1 /_ f2 | (min,+) deconvolution of  $f_1$ and $f_2$. | ✅ |
| f1 /^ f2 | (max,+) deconvolution of  $f_1$ and $f_2$. | ✅ |
| star(f) | Subadditive closure of $f$. | ❌ |
| hShift(f, n) | Compute the function identical to $f$ but horizontally shifted by $n$. | ❌ |
| vShift(f,n ) | Compute the function which is identical to $f$ but vertically shifted by $n$. | ❌ |
| inv(f) | Compute the _lower_ pseudo-inverse of $f$. | ❌ |
| low_inv(f) | Compute the _lower_ pseudo-inverse of $f$. | ❌ |
| up_inv(f) | Compute the _upper_ pseudo-inverse of $f$. | ❌ |
| upclosure(f) | Compute the "non-decreasing" closure of $f$. _WHICH one??_ | ❌ |
| nnupclosure(f,n ) | Compute the "non-negative non-decreasing" closure of $f$. _WHICH one??_ | ❌ |
| f comp g | Compute the composition of $f$ and $g$, i.e. $f(g(x))$ | ✅ |
| left-ext(f) | Defined as the function $g$ such that for all $x$, $g(x) = f(x^-)$. _So it is a left-continuous "extension"?_ | ✅ |
| right-ext(f) | Defined as the function $g$ such that for all $x$, $g(x) = f(x^+)$. _So it is a right-continuous "extension"?_ | ✅ |
| scalar * f | Function multiplication by a scalar value. | ✅ |
| f * scalar | Function multiplication by a scalar value. | ✅ |
| f / scalar | Function division by a scalar value. | ✅ |

## Scalar-returning operations

These operations work on functions, but return scalars.

| Expression | Description | Implemented |
|----|----|----|
| f(x) | Value of f at x | ❌ |
| f(x+) | Value of f at the right of x | ❌ |
| f(x-) | Value of f at the left of x | ❌ |
| hDev(f, g) | Horizontal deviation between $f$ and $g$. | ✅ |
| vDev(f, g) | Vertical deviation between $f$ and $g$. | ✅ |
| maxBacklogPeriod(f, g) | Max backlog period length between $f$ and $g$. | ❌ |

## Operations _between_ scalars

These operations work between scalars, and return scalars.

| Expression | Description | Implemented |
|----|----|----|
| v1 /\ v2 | Minimum of v1 and v2. | ❌ |
| v1 \/ v2 | Maximum of v1 and v2. | ❌ |
| v1 + v2 | Sum of v1 and v2. | ✅ |
| v1 - v2 | Substraction of v1 and v2. | ❌ |
| v1 * v2 | Multiplication of v1 and v2. | ✅ |
| v1 ÷ v2 | Division of v1 and v2. | ❌ |
| v1 div v2 | Division of v1 by v2. | ❌ |


## Output ✅

Any operation that does not assign to a variable, prints its value to the console.

An assignment operation prints the name of the assigned variable to the console.

By typing the name of a variable, one can have its content printed to the console.

The value of a _function_ variable is its definition as `uaf` or `upp`, regardless of the constructor used.

## Plots ❌

`plot(f1, ..., args)`

Plot a graph displaying the functions `f1, f2, ...` 
`args` contains parameters for the drawing. Valid `args` are:
- `main` : the graph title
- `xlim` : range for x-axis
- `ylim` : range for y-axis
- `xlab` : label for x axis
- `ylab` : label for y axis
- `out` : name of png file to save plot to
- `grid ="no"` : remove grid from plot
- `bg ="no"` : use white background instead of default grey

Notes: 
- functions must be variables, they cannot be expressions (e.g., sum of two functions);
- args can be numbers, intervals, string, or string with sum
of numbers, variables and strings for labels.

### Examples
- `plot(f1)`
- `plot(f1, f2)`
- `plot(service2,service1,xlim=[-0.3,15],ylim=[-0.3,15])`
- `plot(f1, main="f1 for J=" +J +"Jitter", xlim=[-0.5, 5], xlab="time", ylab="packets", out = "image.png")`

## New shiny syntax

| Expression | Description | Implemented |
| ---- | ---- | ---- |
| printExpression(f) | Prints out the _expression_ of f. | ✅ |
