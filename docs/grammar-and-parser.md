# The grammar and the parser

MPPG is parsed by ANTLR, from the grammar in `MppgParser/Grammar/Mppg.g4`.
This document explains why that grammar is shaped the way it is, and which of its choices are load-bearing.
It is meant for whoever is about to change it, since several of the rules below look like clutter until the reason for them is known.

For adding a construct to the language, see [Extending the syntax](/docs/extending-the-syntax.md).
For what happens when a script fails to parse, see [Error messages](/docs/error-messages.md).

## Names are known while parsing

In MPPG a variable is declared by assigning to it, and it can only be used after that.
The parser keeps a table of the names declared so far, together with the kind of each one, and fills it as it reads the script.

This is why there is no separate pass to find the declarations first.
A prepass would be a second place deciding what a name means, and the two could disagree.
Instead the parser knows, at the point a name is met, whether it is a curve, a number, or nothing yet.

The interactive mode parses one line at a time, and each line is a parse of its own.
The table would be empty every time, so it is seeded from the session before the line is read.
That is why `Statement.FromLine` and the expression entry points take a `State`.

## Numbers and curves are told apart by the grammar

The kind of an expression is decided while parsing, not afterwards.
The grammar has one set of rules for expressions that produce a curve and another for those that produce a number, and an operation belongs to the set matching what it returns.

Some operators take one of each, such as a curve scaled by a number.
These are parsed through the function-expression rules, and the visitors resolve them once they see which side evaluated to what.

There is a rule that follows from this, and it is worth stating plainly.
A branch handling two numbers inside a function-operator visitor means the expression was routed through the wrong rules.
The fix belongs in the grammar, not in the visitor:
a visitor that quietly handles the wrong case hides the misparse instead of correcting it.

## Semantic predicates, and where they go

A semantic predicate is a condition written into a rule, which ANTLR evaluates while deciding what to match.
MPPG uses them for one thing:
the ambiguity between a name that holds a number and a name that holds a curve, which the grammar alone cannot tell apart.
They are kept few and aimed at that, because every predicate is a piece of the language that lives in C# rather than in the grammar.

Their placement inside a rule follows two different rules, one for the parser and one for the lexer.

In a parser rule, a predicate steers the choice of alternative only if prediction reaches it before any action or token reference.
The ANTLR reference calls such a predicate *visible*.
A predicate placed later is still evaluated, but by then the alternative has been chosen, so it throws `FailedPredicateException` instead of choosing.

In a lexer rule, a predicate may sit anywhere.
The reference recommends the end as the most efficient place, and warns that one may be evaluated several times while a single token is matched.
The only hard requirement is that a predicate comes before any lexer action.

So `FLOOR : 'floor' {IsVersion1_3OrLater()}?;` has its predicate at the end by that advice, not by necessity.
A false predicate prunes the rule wherever it sits, so under an earlier syntax version `floor` falls through and is lexed as an identifier.

## The order of alternatives is not cosmetic

ANTLR commits to the first alternative that matches, so reordering them changes what parses.

The clearest case is bracketed expressions.
A pure-number bracket is meant to fall through to the number rules, and a predicate on the function-bracket alternative is what sends it there.
Reordering those two alternatives makes `(f + g)` dead-end inside the number rules instead.

For the same reason `functionEnclosedExpression` reaches for `numberEnclosedExpression` rather than for `numberExpression`.
The wider rule would match binary operators greedily and take tokens that belong to the operators of the function tier.

Before changing how anything is routed, write tests for both directions.
One for the case being refined, such as `(x + y)` standing in a function expression, and one for a genuinely mixed case such as `(f + x)`, which must still take the path it took before.

## Every parse goes through one place

Parsers are built by `MppgParsing.Create`, which removes the default error listeners and installs the ones that collect errors.
Building one by hand would put ANTLR's own messages on standard error, from a library that is also used by the tests and by the CLI.

That factory also decides what the parser does after an error.
Whole programs are parsed so that the statements after a bad one are still read, and `run --on-error Continue` reports all of them.
Every other entry point stops at the first error, since the earliest failure is the one worth reporting, rather than the point recovery happened to settle on.

ANTLR ships a strategy that stops at the first error by throwing, `BailErrorStrategy`, and it is not used here.
It throws before notifying the listeners, so the error never reaches the collector and there is nothing left to render.

## A new route needs the visitors to follow it

When a construct starts being routed through a different alternative, every visitor needs an override for the new context.
Usually it does nothing but hand the work to the child context, matching what the neighbouring overrides do.
Without it the visitor silently produces nothing for that construct, which is the failure mode described in [Extending the syntax](/docs/extending-the-syntax.md).
