# Error messages

When `nancy-playground` is given an invalid script as input, most of the time the failure happens during parsing, as ANTLR is unable to derive a coherent syntax tree for the script.
However, ANTLR errors are very low level, and specific for grammar-parsing:
useful for dev debugging, not so much for an end user making honest mistakes.

So, `nancy-playground` has a mechanism to capture, analyze and rewrite ANTLR errors into something more readable, based on *known* patterns.
This document explains how that works, and how to teach it a new mistake.

The rule the whole layer is built around is that **nothing is invented**.
A message is rewritten only where the tool recognises what the mistake was, and everything else keeps the wording of ANTLR as a safe fallback.
A confident explanation of the wrong mistake sends a reader looking in the wrong place, which is worse than an obscure message that at least points at the right token.

## The matcher, which is where a message comes from

A matcher is a small class that knows one kind of mistake:
whether an error is that mistake, and what to say when it is.
`f := stair(1)` makes ANTLR report `no viable alternative at input 'stair'`, and the matcher for argument counts turns it into `'stair' takes 3 arguments`, because it recognised a call written with too few of them.

Each one lives in a file of its own under `MppgParser/ErrorMatchers/` and answers three questions:

- **what it is called**, used in tests and `--verbose` prints to trace back rewrites to the matcher that did it;
- **whether it recognises an error**, which is a guard and nothing else;
- **what it writes about one**, which is asked only after the guard has said yes.

Recognising and writing are kept apart on purpose:
a test can ask every matcher whether it claims an error, without any of them writing a message.
The checks further down are built on that.

The matchers are held in a registry and tried in turn, and the first that recognises the error answers.
There is one registry for the errors of the parser, holding sixteen of them, and one for the errors of the lexer, holding three.
Between them they cover the mistakes met so far:
a keyword used as a variable name, an argument list too short or too long, an interval missing an extreme, a name a plot cannot take, an assignment written with `=`, and so on.

## What a matcher reads

Matchers do not work with the _text_ of an ANTLR error.
Instead, an error is first collected into an object holding what is known about it, with one class per kind of thing that can go wrong:

- a `LexerError` is about a character no token rule accepts, and knows the character and where it is;
- a `ParserError` is about a token the parser could not use, and knows the token and its neighbours, the rule being parsed, what would have fitted instead, and the variables declared so far;
- an `UnusableVersionDirectiveError` is a `#!syntax version` line this build cannot apply, which the grammar accepts and the tool refuses.

Keeping the kinds apart means a matcher never has to check what it was given:
one that reads a grammar rule cannot receive an error from the lexer.

These classes also answer questions about the error, each computed once and read by whoever needs it:
whether the line uses a keyword as a name, which call a token stands inside, whether the round brackets of the line are balanced.
When two matchers need the same check, it goes on the error object, not inside either of them.
That way neither has to know about the other.

## When no matcher recognises the error

Two things happen below the matchers, in order.

First, what the parser expected is put into words: `unexpected ']', an expression was expected instead`.
It says nothing about the mistake, only what could have stood there, so it runs after everything that might know more.
The sets it names are computed from the grammar itself, so an expression that gains a keyword does not quietly turn back into a list of forty-odd tokens.

Then, whatever is still unclaimed keeps its own wording:
ANTLR's for a parse error, the tool's own for a directive it cannot apply.
Even here the text is tidied rather than replaced.
In `no viable alternative at input '(floorcomp'` the quoted span comes from ANTLR clamping tokens together with no separator, so it is re-read from the source and shown as `'( floor comp'`, just as the user wrote it.

## Hints

Beside the message there may be a hint, and it answers a different question:
not *what did the parser stop at* but *what should you go and look at*.

When a keyword is used as a name, a hint is given about which version reserved that keyword and what to declare to keep using the name.
When a line has unbalanced round brackets, a hint is useful since the parser will most likely point to whenever it gave up, not to where the critical bracket is missing.

## Adding a matcher

A new file under `ErrorMatchers/` and a line in the registry is the whole of the mechanism.
The difficulty is in choosing what the matcher should claim, where two constraints apply.

**Two matchers recognising the same error is a defect, not a precedence to settle.**
The registry is read in order, but that order is meant to carry no information:
whichever came first would answer, and it is not necessarily the one that understands the mistake best.
When a new matcher overlaps an existing one, the fix is to sharpen the guards, usually by moving the shared question onto the error as a fact and having one read it positively and the other negatively.

**A mistake that admits more than one reading is better left unclaimed.**
`T4 60` could be a dropped `:=`, a dropped operator or a dropped comma, and nothing in the line tells which;
`T4 = 60` can be nothing but an assignment written with the wrong operator, and that one is worth naming.
What is left unclaimed still goes through the two stages below, so saying nothing costs little.

## What the tests hold

Each matcher has its messages pinned, and also the cases it must *not* claim.
The second half matters more:
a guard that is too loose steals errors that another matcher explains better.

The set as a whole is checked against a corpus of failing programs, kept for the purpose:

- at most one matcher recognises any given error, so the order of the registry does not matter;
- every matcher recognises something, so that none sits there unreachable because its guard is wrong or its case is no longer reported that way;
- every message reads as a fragment, since it is printed after `line 3:14` and neither opens with a capital nor closes with a period;
- every hint reads as a sentence, since it stands on its own rather than continuing the line above it.

A new kind of failing program belongs in the corpus even when nothing claims it yet, since these checks are about the whole set.

## Tracing a message

`--verbose` prints, under the message, what the parser said, the matcher that reworded it, the rule stack, and the tokens that would have fitted:

```
 - line 1:5 'f' is not a declared variable
   g := f + 1
        ^
   parser: no viable alternative at input 'f'
   reworded by: unknown variable
   rule: program < statementLine < statement < assignment < expression
   expected: '(', 'star', 'hShift', ...
```

This is what to read when a message is confusing, and what to include when reporting one.
