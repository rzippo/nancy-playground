# The grammar and the parser

MPPG is parsed by ANTLR, from the grammar in `MppgParser/Grammar/Mppg.g4`.
This document explains why that grammar is shaped the way it is, and which of its choices cannot be changed without breaking something else.
It is meant for whoever is about to change it, since several of the rules below look like clutter until the reason for them is known.

For adding a construct to the language, see [Extending the syntax](/docs/extending-the-syntax.md).
For what happens when a script fails to parse, see [Error messages](/docs/error-messages.md).

What ANTLR itself does is described in *The Definitive ANTLR 4 Reference* [[1]](https://pragprog.com/titles/tpantlr2/the-definitive-antlr-4-reference/), by Terence Parr.
The section numbers below, such as §15.7, point into that book.

## Names are known while parsing

The parser knows whether a name holds a function or a number at the point it meets it.
It keeps a table of the names declared so far, with the kind of each one, and fills it as it reads the script.
This works because in MPPG a variable is declared by assigning to it, and can only be used after that.

There is no separate pass to find the declarations first.
That would be a second place deciding what a name means, and the two could disagree.

The interactive mode parses one line at a time, each as a parse of its own, so its table would start empty every time.
The session fills it in before the line is read.
That is the `State` passed to `Statement.FromLine` and to the expression entry points.

## Numbers and functions are told apart by the grammar

The kind of an expression is decided while parsing, not afterwards.
An operation belongs to the set of rules that matches what it returns, so `f * 2` is read by the function rules and `f(3) * 2` by the number ones.

Some operators take one of each, such as a function scaled by a number.
These are parsed through the function rules, and the kinds are settled later by the visitors, the classes that walk the parse tree (§7.3) and are described in [Extending the syntax](/docs/extending-the-syntax.md).

A branch handling two numbers, inside a visitor of the function operators, means the expression was routed through the wrong rules.
The fix belongs in the grammar, not in the visitor:
a visitor that quietly handles the wrong case hides the misparse instead of correcting it.

## Semantic predicates, and where they go

A semantic predicate is a condition written into a rule, which ANTLR evaluates while deciding what to match (chapter 11, and §15.7 for the fine print).
MPPG uses them for one thing:
the ambiguity between a name that holds a number and a name that holds a function, which the grammar alone cannot tell apart.
They are kept few and aimed at that, because a predicate puts part of the language in C# instead of in the grammar.

Their placement inside a rule follows two different rules, one for the parser and one for the lexer.

ANTLR chooses between the alternatives of a rule by looking ahead, a step it calls prediction (§2.2).
In a parser rule, a predicate steers that choice only if prediction reaches it before any action or token reference.
The book calls such a predicate *visible*, and says that prediction ignores the others as if they were not written (§15.7, *Finding Visible Predicates*).
A predicate placed later is still evaluated, but by then the alternative has been chosen, so it throws `FailedPredicateException` instead of choosing.

In a lexer rule, a predicate may sit anywhere (§15.7, *Predicates in Lexer Rules*).
The book puts them at the end, because a lexer rule is chosen only once the whole text of the token has been read, and it promises nothing about which position is the fastest.
It also warns that a predicate may be evaluated several times while a single token is matched.
The only hard requirement is that a predicate comes before any lexer action, since an action runs only after the rule has been chosen.

So `FLOOR : 'floor' {IsVersion1_3OrLater()}?;` has its predicate at the end by that advice, not by necessity.
A false predicate prunes the rule wherever it sits, so under an earlier syntax version `floor` falls through and is lexed as an identifier.

## The order of alternatives is not cosmetic

Where more than one alternative can match, ANTLR takes the one written first, so reordering them changes what parses (§15.7).

The clearest case is bracketed expressions.
The alternative for a bracket holding a function carries a predicate, so a bracket holding only numbers fails it and falls through to the number rules.
Reordering the two alternatives makes `(f + g)` dead-end inside the number rules instead.

For the same reason `functionEnclosedExpression` uses `numberEnclosedExpression` and not `numberExpression`.
The wider rule would match binary operators greedily and take tokens that belong to the operators of the function rules.

Before changing how anything is routed, write tests for both directions.
One for the case being refined, such as `(x + y)` inside a function expression.
One for a mixed case such as `(f + x)`, which has to keep the path it already took.

## Every parse goes through one place

Parsers are built by `MppgParsing.Create`, which removes the default error listeners and installs the ones that collect errors (§9.2).
Building one by hand would put ANTLR's own messages on standard error, from a library that is also used by the tests and by the CLI.

That factory also decides what the parser does after an error.
ANTLR can skip ahead and carry on, a step it calls recovery (§9.3), and that is what lets one parse report more than one error.
Whole programs are parsed that way, so the statements after a bad one are still read, and `run --on-error Continue` reports all of them.
Every other entry point stops at the first error, since recovery moves the reported position away from the failure that caused it.

ANTLR ships a strategy that stops at the first error by throwing, `BailErrorStrategy` (§9.5), and it is not used here.
It throws before notifying the listeners, so the error never reaches the collector and there is nothing left to render.

## A new route needs the visitors to follow it

When a construct starts being routed through a different alternative, every visitor needs an override for the new context, which is the class ANTLR generates for that alternative (§7.4).
Usually it does nothing but hand the work to the child context, matching what the neighbouring overrides do.
Without it the visitor silently produces nothing for that construct, which is the failure mode described in [Extending the syntax](/docs/extending-the-syntax.md).
