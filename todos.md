- [ ] Let interactive mode specify the directory where exports should be saved
- [ ] Filter the interactive autocomplete by the session syntax version: `InteractiveCommand.Keywords.cs`
  lists every keyword, so a 1.0 session suggests keywords that version does not have
- [x] Add plot to TiKZ, as the `plotTikz` command
  - [ ] Integrate with [`tikz-to-pdf`](https://github.com/rzippo/tikz-to-pdf)?

## Operators of Nancy still missing from the syntax

Compared against `Unipi.Nancy` 1.3.6 and `Unipi.Nancy.Expressions` 1.0.6.

- [ ] `floor(f)` and `ceil(f)`, from `Curve.Floor` and `Curve.Ceil`.
  Writing these today is cumbersome: it takes `right-ext(stair(1, 1, 1)) comp (f / n)`, as in the
  `hal-04513292v1` test case.
- [ ] Scalar operators `abs`, `pow`, `mod`, `gcd`, `lcm`, from `Expressions.AbsoluteValue`, `Pow`,
  `Remainder`, `GreatestCommonDivisor` and `LeastCommonMultiple`.
  Scalars currently have only `+ - * / div /\ \/`.
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

Note that `Floor`, `Ceil` and the predicates exist on `Curve` but not in `Unipi.Nancy.Expressions`
1.0.6. They need either an addition there, to keep the syntax symbolic, or eager evaluation, which
would make `printExpression` and `convert` show a computed curve instead of the expression that
produced it.
