using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Tests;

/// <summary>
/// The messages of the parser, rewritten into sentences addressed to whoever wrote the script.
/// What no pattern recognises keeps what ANTLR said, which is what these pin as much as the rewrites.
/// </summary>
public class SyntaxErrorMessagesTests
{
    private static SyntaxErrorInfo FirstError(string programText)
    {
        var program = Program.FromText(programText);
        Assert.NotEmpty(program.Errors);
        return program.Errors[0];
    }

    [Theory]
    // a name never declared, wherever an expression is what the syntax expects
    [InlineData("g := f + 1")]
    [InlineData("g := f(2)")]
    [InlineData("f")]
    [InlineData("assert( f = 1 )")]
    [InlineData("g := 1 + f")]
    public void UnknownVariableIsNamed(string programText)
    {
        var error = FirstError(programText);

        Assert.Equal("'f' is not a declared variable", error.Message);
    }

    [Fact]
    public void UnknownVariableKeepsWhatAntlrSaid()
    {
        var error = FirstError("g := f + 1");

        Assert.Equal("'f' is not a declared variable", error.Message);
        Assert.Equal("no viable alternative at input 'f'", error.AntlrMessage);
    }

    [Theory]
    // the keyword is the offending token, and the assignment follows it
    [InlineData("div := 3", "div")]
    [InlineData("comp := 3", "comp")]
    // the parser read past the keyword and stopped at the assignment
    [InlineData("star := 3", "star")]
    [InlineData("inv := 3", "inv")]
    public void KeywordUsedAsNameIsNamed(string programText, string keyword)
    {
        var error = FirstError(programText);

        Assert.Equal($"'{keyword}' is a keyword, so it cannot be a name", error.Message);
    }

    /// <summary>
    /// The message says that it is a keyword, the hint says since when, so that the two do not repeat each other.
    /// </summary>
    [Fact]
    public void VersionedKeywordUsedAsNameKeepsItsHint()
    {
        var error = FirstError("#!syntax version 1.3\nfloor := 3");

        Assert.Equal("'floor' is a keyword, so it cannot be a name", error.Message);
        Assert.Equal(
            "'floor' is a keyword from version 1.3 on: to keep using it as a name, "
                + "use '#!syntax version 1.2', or earlier, before any other statement.",
            error.Hint);
    }

    [Theory]
    // the word opens the statement, or stands where an expression does
    [InlineData("#!syntax version 1.0\na := 1\nprintExpression(a)", "printExpression", "1.0", "1.1")]
    [InlineData("#!syntax version 1.2\nx := abs(2)", "abs", "1.2", "1.3")]
    // an infix operation, which reads as a name after a statement that was read whole
    [InlineData("#!syntax version 1.2\nx := 5 mod 2", "mod", "1.2", "1.3")]
    // a call whose arity is right, which is reported inside the argument list
    [InlineData("#!syntax version 1.2\nx := pow(2, 3)", "pow", "1.2", "1.3")]
    public void OperationOfALaterVersionIsNamed(string programText, string keyword, string inForce, string introducedIn)
    {
        var error = FirstError(programText);

        Assert.Equal($"'{keyword}' is not an operation of syntax version {inForce}", error.Message);
        Assert.Equal(
            $"'{keyword}' is an operation from version {introducedIn} on: to use it, "
                + $"declare '#!syntax version {introducedIn}', or later, before any other statement.",
            error.Hint);
    }

    /// <summary>
    /// A name the program declares is its own, whatever it spells, so the error is about the use and not about the version.
    /// </summary>
    [Fact]
    public void NameDeclaredUnderAnOlderVersionIsNotClaimed()
    {
        var error = FirstError("#!syntax version 1.2\nfloor := 3\nx := floor(2)");

        Assert.NotEqual("operation of a later syntax version", error.RewrittenBy);
        Assert.Equal("'floor' is a number, and only a function can be sampled", error.Message);
    }

    /// <summary>
    /// A name that spells nothing of a later version stays an unknown variable, whatever the declared version is.
    /// </summary>
    [Fact]
    public void UnknownNameUnderAnOlderVersionIsNotClaimed()
    {
        var error = FirstError("#!syntax version 1.2\ny := nosuch + 1");

        Assert.Equal("'nosuch' is not a declared variable", error.Message);
    }

    /// <summary>
    /// A keyword that is wrong where it stands, but is not being named, is not claimed: in <c>( floor comp (C / 2) )</c> under 1.3 the error lands on <c>comp</c>, which is the operator it spells and is used correctly, the mistake being the <c>floor</c> before it.
    /// </summary>
    [Fact]
    public void KeywordThatIsNotBeingNamedIsNotClaimed()
    {
        var program = Program.FromText("#!syntax version 1.3\nC := bucket(2, 5)\nA := ( floor comp (C / 2) ) * 4");

        var error = Assert.Single(program.Errors);
        Assert.NotEqual("keyword used as a name", error.RewrittenBy);
        Assert.DoesNotContain("is a keyword, so it cannot be a name", error.Message);
        // the hint still points at the keyword that is the cause, which the message is not about
        Assert.Contains("'floor' is a keyword from version 1.3 on", error.Hint);
    }

    /// <summary>
    /// A name of the syntax used where it belongs is no mistake, so nothing is reported at all.
    /// </summary>
    [Theory]
    [InlineData("x := 1\ny := x + zero")]
    // the plot options are contextual keywords, so they are names as well
    [InlineData("f := bucket(2, 5)\nout := f")]
    public void KeywordInItsPlaceIsNoError(string programText)
    {
        Assert.Empty(Program.FromText(programText).Errors);
    }

    [Theory]
    // a bracket left open, where the parser reaches the end of the line looking for it
    [InlineData("f := bucket(2, 5", "a ')' is missing at the end of the line")]
    [InlineData("f := bucket(2, 5\ng := 1", "a ')' is missing at the end of the line")]
    [InlineData("f := uaf( [(0,0)1(1,1)[ ", "a ')' is missing at the end of the line")]
    // a separator left out, where the parser stops at the token that should have followed it
    [InlineData("f := bucket(2 5)", "a ',' is missing before '5'")]
    public void MissingTokenIsNamed(string programText, string expected)
    {
        var error = FirstError(programText);

        Assert.Equal(expected, error.Message);
    }

    /// <summary>
    /// A token too many comes the same way as one too few, with no exception to tell them apart, so it must not be read as something left out.
    /// </summary>
    [Fact]
    public void ExtraneousTokenIsNotClaimedAsMissing()
    {
        var error = FirstError("f := bucket(2, 5))");

        Assert.DoesNotContain("is missing", error.Message);
    }

    /// <summary>
    /// A declared name is not the cause, so whatever the error is, it is not about an unknown one.
    /// </summary>
    [Fact]
    public void DeclaredVariableIsNotClaimed()
    {
        var error = FirstError("f := bucket(2, 5)\ng := f ]");

        Assert.DoesNotContain("is not a declared variable", error.Message);
    }

    /// <summary>
    /// The argument of a plot names an option as well as a function, so a name that is neither is told which of the two it failed to be, by the equals sign that says how it was written.
    /// </summary>
    [Theory]
    [InlineData("f := bucket(2, 5)\nplot(f, nosuch=\"x\")", "'nosuch' is not an option of a plot")]
    [InlineData("plot(nosuch)", "'nosuch' is neither a declared function nor an option of a plot")]
    public void NameAPlotCannotTakeIsNamed(string programText, string expected)
    {
        var error = FirstError(programText);

        Assert.Equal(expected, error.Message);
    }

    [Theory]
    // a constructor, and a function operation, closed too early or carried on too long
    [InlineData("f := bucket(2)", "'bucket' takes 2 arguments")]
    [InlineData("f := stair(1, 2)", "'stair' takes 3 arguments")]
    [InlineData("f := stair(1)", "'stair' takes 3 arguments")]
    [InlineData("f := bucket(2, 5)\ng := hShift(f)", "'hShift' takes 2 arguments")]
    [InlineData("f := bucket(2, 5, 7)", "'bucket' takes 2 arguments")]
    [InlineData("f := delay(1, 2)", "'delay' takes 1 argument")]
    public void WrongNumberOfArgumentsIsNamed(string programText, string expected)
    {
        var error = FirstError(programText);

        Assert.Equal(expected, error.Message);
    }

    /// <summary>
    /// A scalar operation fails further out, in the expression, so nothing there says which call it was: what was expected is still named, the whole start set of an expression being what it is.
    /// </summary>
    [Theory]
    [InlineData("x := pow(2)", "'pow' takes 2 arguments")]
    [InlineData("x := abs(2, 3)", "'abs' takes 1 argument")]
    public void WrongNumberOfScalarArgumentsIsNamed(string programText, string expected)
    {
        var error = FirstError(programText);

        Assert.Equal(expected, error.Message);
    }

    /// <summary>
    /// What a construct needs is said in its own terms, where the rule being parsed says which construct it is.
    /// </summary>
    [Theory]
    [InlineData("f := bucket(2, 5)\nplot(f, xlim=[1,])", "the interval is missing its right extreme")]
    [InlineData("f := bucket(2, 5)\nplot(f, xlim=[,2])", "the interval is missing its left extreme")]
    [InlineData("f := bucket(2, 5)\nassert(f)", "'assert' takes a comparison between two expressions, or a property check with 'is'")]
    [InlineData("f := bucket(2, 5)\nassert(f is)", "'assert' takes a comparison between two expressions, or a property check with 'is'")]
    [InlineData("x := 1\nplot(x)", "'x' is a number, and 'plot' takes functions")]
    [InlineData("x := 1\ny := x(3)", "'x' is a number, and only a function can be sampled")]
    [InlineData("f := bucket(2, 5)\n)", "a statement cannot start with ')'")]
    [InlineData("f := bucket(2, 5)\nplot(f, out=)", "'out' needs a value")]
    [InlineData("f := bucket(2, 5)\ng := f(3, 4)", "'f' is sampled at one point, so it takes one argument")]
    [InlineData("f := bucket(2, 5)\ng := ((f + 1) (f - 1))", "an operator is missing between the two expressions")]
    [InlineData("assert(1, 2)", "unexpected ',', a comparison or 'is' was expected instead")]
    public void WhatTheConstructNeedsIsNamed(string programText, string expected)
    {
        var error = FirstError(programText);

        Assert.Equal(expected, error.Message);
    }

    /// <summary>
    /// A set of forty-odd tokens is named after the construct it opens, and a set of two or three is spelled, which is where listing still reads.
    /// </summary>
    [Theory]
    // the start set of an expression, which is what most of the grammar expects
    [InlineData("x := ]", "unexpected ']', an expression was expected instead")]
    // the same, reached by a token the parser dropped rather than one it could not use
    [InlineData("x := * 2", "unexpected '*', an expression was expected instead")]
    // and a set small enough to say
    public void WhatWasExpectedIsNamedOrSpelled(string programText, string expected)
    {
        var error = FirstError(programText);

        Assert.Equal(expected, error.Message);
    }

    [Theory]
    [InlineData("x := 1\ny := x ÷ 2", "'÷' is not a supported character")]
    [InlineData("x := 1 @ 2", "'@' is not a supported character")]
    [InlineData("f := bucket(2, @)", "'@' is not a supported character")]
    // the lexer stops at the quote that opens a string it never finds the end of
    [InlineData("f := bucket(2, 5)\nplot(f, out=\"x)", "a string is not closed")]
    public void CharacterTheLexerCannotReadIsNamed(string programText, string expected)
    {
        var error = FirstError(programText);

        Assert.Equal(SyntaxErrorInfo.ErrorType.Lexer, error.Type);
        Assert.Equal(expected, error.Message);
    }

    /// <summary>
    /// Where the line says what the character was being written as, the message says that, and the character it could not read becomes the detail behind it.
    /// </summary>
    [Theory]
    // the character is the whole of what was being named
    [InlineData("@ := 1")]
    // and where it is one character of a name, wherever in it that character stands
    [InlineData("ab@ := 1")]
    [InlineData("@ab := 1")]
    [InlineData("a@b := 1")]
    [InlineData("my@var := 1")]
    public void CharacterWrittenAsANameIsNamedAsOne(string programText)
    {
        var error = FirstError(programText);

        Assert.Equal("'@' is not a valid name", error.Message);
        Assert.Equal("'@' is not a supported character.", error.Hint);
    }

    /// <summary>
    /// A ':=' inside a string is text, not an assignment, so a character before it is not a name being written.
    /// </summary>
    /// <remarks>
    /// The line is read rather than its tokens, the lexer having stopped before making any, so the quotes are what say which ':=' is one.
    /// </remarks>
    [Theory]
    // the only ':=' of the line is inside a string, so the character stands before no assignment
    [InlineData("f := bucket(2, 5)\nplot(f, out=@ + \"a := b\")")]
    // and where there is an assignment, what stands before it is not a name, so the character is not part of one
    [InlineData("f(2) @ := 3")]
    public void CharacterThatIsNotInANameIsNotNamedAsOne(string programText)
    {
        var error = FirstError(programText);

        Assert.Equal("'@' is not a supported character", error.Message);
        Assert.Null(error.Hint);
    }

    /// <summary>
    /// A lexer reports its errors with no offending symbol, passing zero, so the character comes from the input at the position of the error rather than from what it was given.
    /// </summary>
    [Fact]
    public void CharacterTheLexerCannotReadIsTheOneInTheInput()
    {
        var error = FirstError("x := 1 @ 2");

        Assert.Equal("@", error.OffendingText);
    }

    /// <summary>
    /// A line parsed on its own is anchored at the end of the input, where a program is anchored at the end of the line, so the two report what follows a statement the same way.
    /// </summary>
    [Fact]
    public void SomethingAfterTheStatementReadsTheSameOnASingleLine()
    {
        var statement = Assert.Throws<Exceptions.SyntaxErrorException>(() => Statement.FromLine("x := 1 2"));

        Assert.Equal("unexpected '2' after the end of the statement", statement.Error?.Message);
        Assert.Equal("unexpected '2' after the end of the statement", FirstError("x := 1 2").Message);
    }

    /// <summary>
    /// With nothing before it on the line, no statement was read, so nothing can follow one: the error keeps its message rather than say that it does.
    /// </summary>
    [Fact]
    public void TokenOpeningALineIsNotAfterAStatement()
    {
        var statement = Assert.Throws<Exceptions.SyntaxErrorException>(() => Statement.FromLine(")"));

        Assert.DoesNotContain("after the end of the statement", statement.Error?.Message);
    }

    [Theory]
    // a bracket too many, reported as an extraneous token
    [InlineData("f := bucket(2, 5))", "unexpected ')' after the end of the statement")]
    // and a second statement on the same line, reported as a mismatched one
    [InlineData("x := 1 2", "unexpected '2' after the end of the statement")]
    [InlineData("x := 1 = 2", "unexpected '=' after the end of the statement")]
    public void SomethingAfterTheStatementIsNamed(string programText, string expected)
    {
        var error = FirstError(programText);

        Assert.Equal(expected, error.Message);
    }

    [Theory]
    // an operand still to come
    [InlineData("g := 1 +")]
    [InlineData("f := bucket(2, 5)\ng := f *")]
    // and a bracket still to close, which the parser reports the same way, from the expression
    [InlineData("g := (1 + 2")]
    public void IncompleteExpressionIsNamed(string programText)
    {
        var error = FirstError(programText);

        Assert.Equal("the expression is incomplete", error.Message);
    }

    /// <summary>
    /// What no matcher recognises keeps what the error carries, which is what the fallback means.
    /// It is pinned by reporting an error with the rewriting turned off, there being fewer and fewer errors that nothing claims.
    /// </summary>
    [Fact]
    public void WhatIsNotRecognisedKeepsItsMessage()
    {
        var error = Assert.IsAssignableFrom<ParseError>(FirstError("g := f + 1").Source);

        var kept = SyntaxErrorInfo.From(error, rewrite: false);

        Assert.Null(kept.RewrittenBy);
        Assert.Equal(error.DefaultMessage, kept.Message);
    }

    /// <summary>
    /// A line that opens with a name and an equals sign is an assignment written with the wrong operator, which is one of the few mistypings sure enough to name.
    /// </summary>
    [Theory]
    [InlineData("T4 = 60", "an assignment to 'T4' is written with ':=', not with '='")]
    [InlineData("f := bucket(2, 5)\ng = f + 1", "an assignment to 'g' is written with ':=', not with '='")]
    // where the lines after it give the parser somewhere else to go, it stops at the '=' rather than at the name
    [InlineData("T4 = 60\nA := stair(0, T4, 12)", "an assignment to 'T4' is written with ':=', not with '='")]
    public void AssignmentWrittenWithAnEqualsIsNamed(string programText, string expected)
    {
        var error = FirstError(programText);

        Assert.Equal(expected, error.Message);
    }

    /// <summary>
    /// An equals sign after a name is a comparison inside an assertion and an option inside a plot, so neither is read as an assignment.
    /// </summary>
    [Theory]
    [InlineData("f := bucket(2, 5)\nassert(nosuch = 1)", "'nosuch' is not a declared variable")]
    [InlineData("f := bucket(2, 5)\nplot(f, nosuch=\"x\")", "'nosuch' is not an option of a plot")]
    public void AnEqualsElsewhereIsNotAnAssignment(string programText, string expected)
    {
        var error = FirstError(programText);

        Assert.Equal(expected, error.Message);
    }

    /// <summary>
    /// A bracket left out is reported wherever the parser gives up, which is rarely where it was left out, so the count of the line is suggested whatever the message says.
    /// </summary>
    [Fact]
    public void UnbalancedBracketsAreSuggested()
    {
        var error = FirstError("f := bucket(2, 5)\ns := bucket(1, 1)\nb := ( f * s ) comp f) * ( s comp f)");

        Assert.Equal("The brackets of this line are not balanced: 2 opened and 3 closed.", error.Hint);
    }

    /// <summary>
    /// A segment says with a square bracket which of its ends it includes, so <c>](0, 1) 1 (1, 2)[</c> is written with two that do not match and round ones that do.
    /// </summary>
    [Fact]
    public void TheBracketsOfASegmentAreNotCounted()
    {
        Assert.Empty(Program.FromText("f := uaf([(0, 0)] ](0, 1) 1 (1, 2)[ [(1, 3)] ](1, 3) 0 (+inf, 3)[)").Errors);
    }

    /// <summary>
    /// A bracket inside a string is text, and one after a comment is not on the line at all.
    /// </summary>
    [Fact]
    public void BracketsInAStringOrACommentAreNotCounted()
    {
        var error = FirstError("f := bucket(2, 5)\nplot(f, out=\"a)b\") ]");

        Assert.DoesNotContain("are not balanced", error.Hint ?? string.Empty);
    }

    [Fact]
    public void EveryErrorCarriesWhatAntlrSaid()
    {
        var program = Program.FromText("g := f + 1\nx := 1 = 2\ny := 1 ÷ 2");

        Assert.NotEmpty(program.Errors);
        Assert.All(program.Errors, error => Assert.False(string.IsNullOrEmpty(error.AntlrMessage)));
    }
}
