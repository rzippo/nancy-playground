- [ ] Let interactive mode specify the directory where exports should be saved
- [ ] Filter the interactive autocomplete by the session syntax version: `InteractiveCommand.Keywords.cs`
  lists every keyword, so a 1.0 session suggests keywords that version does not have
- [x] Add plot to TiKZ, as the `plotTikz` command
  - [ ] Integrate with [`tikz-to-pdf`](https://github.com/rzippo/tikz-to-pdf)?

## Operators of Nancy still missing from the syntax

Compared against `Unipi.Nancy` 1.3.6 and `Unipi.Nancy.Expressions` 1.0.7.

- [x] `floor(f)` and `ceil(f)`, from `Curve.Floor` and `Curve.Ceil`, and `floor(v)` and `ceil(v)` on
  scalars, added in syntax version 1.3.
  Scripts that spelled the floor function as `right-ext(stair(1, 1, 1))` and named it `floor`, as the
  `hal-04513292v1` test case did, now declare `#!syntax version 1.2` to keep that name.
- [x] Scalar operators `abs`, `pow`, `mod`, `gcd`, `lcm`, from `Expressions.AbsoluteValue`, `Pow`,
  `Remainder`, `GreatestCommonDivisor` and `LeastCommonMultiple`, added in syntax version 1.3.
  All are call forms, `mod(v1, v2)` included, rather than infix like `div`.
- [ ] Curve predicates, to be used with `assert`: `Dominance`, `EquivalentAfter`,
  `EquivalentExceptOrigin`, the `IsContinuousAt` and `IsNonDecreasingOverInterval` family, and the
  curve properties (`IsSubAdditive`, `IsConcave`, ...), none of which can be queried today.
  Needs design first: `assert` compares two expressions, so a predicate is a new kind of assertion.

Considered and left out:

- `ToNonNegative`, `WithZeroOrigin`, `DelayBy`, `ForwardBy`: already expressible with simple constructs.
- Curve-to-scalar queries, i.e. `MaxValue`, `MinValue`, `SupValue`, `InfValue`, the matching `*Arg`,
  and `TimeAt`: niche.
- `maxBacklogPeriod`, listed as not implemented in `syntax.md`: `Curve` has no such method, so it
  needs work in Nancy before it can be a syntax question.

`Unipi.Nancy.Expressions` 1.0.7 added `Floor` and `Ceil`, on both curves and rationals, so the syntax
maps to them and stays symbolic. The predicates still exist on `Curve` only: they need either an
addition there, to keep the syntax symbolic, or eager evaluation, which would make `printExpression`
and `convert` show a computed curve instead of the expression that produced it.
