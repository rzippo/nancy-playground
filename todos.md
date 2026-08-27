- [ ] Filter the interactive autocomplete by the session syntax version: `InteractiveCommand.Keywords.cs`
  lists every keyword, so a 1.0 session suggests keywords that version does not have
- [ ] Integrate `plotTikz` with [`tikz-to-pdf`](https://github.com/rzippo/tikz-to-pdf)?
- [ ] Give `MppgClassic` a message aimed at users where a plot would go, or render plots in it
- [ ] Add the print commands for the notations an expression can take, i.e. the Unicode and LaTeX forms
- [ ] Point a failing call at the token the message names, rather than at where the parse stopped
- [ ] Cover `--on-error Continue` with a golden case
- [ ] Reflow the comments that wrap a sentence over several lines
- [ ] Share the plot formatter construction between `run` and `interactive`
- [ ] Bump `Unipi.Nancy` and `Unipi.Nancy.Expressions` when the coming releases land

## Documentation

- [ ] Describe the CLI itself: the commands, the run modes, the output modes, and what `convert` produces
- [ ] Say where a contributor starts, a line of the readme being the only way in today
- [ ] Cite the papers behind the operations of the syntax in `syntax.md`
- [ ] Describe `bucket` as rate then burst, rather than as slope and constant

## Convert: emit a Roslyn syntax tree

The two string building visitors would become a single visitor emitting a `CompilationUnitSyntax`, parameterised on whether the converted program uses `Unipi.Nancy.Expressions`.

- [ ] Write the emission layer against the whole syntax, i.e. `plotTikz`, the syntax version
  directives, the v1.2 closures, `zDev` and the v1.3 scalar operators
- [ ] Use the syntax tree to drop the parentheses and casts that the string visitors emit defensively

## Operators of Nancy still missing from the syntax

Compared against `Unipi.Nancy` 1.3.6 and `Unipi.Nancy.Expressions` 1.0.7, and due again against the versions in use.

- [ ] Curve predicates, to be used with `assert`: `Dominance`, `EquivalentAfter`,
  `EquivalentExceptOrigin`, the `IsContinuousAt` and `IsNonDecreasingOverInterval` family, and the curve properties (`IsSubAdditive`, `IsConcave`, ...), none of which can be queried today.
  Needs design first: `assert` compares two expressions, so a predicate is a new kind of assertion.

Considered and left out:

- `ToNonNegative`, `WithZeroOrigin`, `DelayBy`, `ForwardBy`: already expressible with simple constructs.
- Curve-to-scalar queries, i.e. `MaxValue`, `MinValue`, `SupValue`, `InfValue`, the matching `*Arg`,
  and `TimeAt`: niche.
- `maxBacklogPeriod`, listed as not implemented in `syntax.md`: `Curve` has no such method, so it
  needs work in Nancy before it can be a syntax question.
