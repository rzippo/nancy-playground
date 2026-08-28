using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Utility;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Unipi.Nancy.Playground.MppgParser.Visitors.CodeGeneration;

internal static class NancyCodeTreeBuilder
{
    public static CompilationUnitSyntax ToCompilationUnit(
        Unipi.MppgParser.Grammar.MppgParser.ProgramContext context,
        MppgBaseVisitor<GeneratedCode> visitor,
        IEnumerable<string> packageDirectives,
        IEnumerable<string> usingNames)
    {
        var statementLineContexts = context.GetRuleContexts<Unipi.MppgParser.Grammar.MppgParser.StatementLineContext>();
        var entries = new List<GeneratedCodeEntry>
        {
            new GeneratedStatementEntry(ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    Member(IdentifierName("CultureInfo"), "CurrentCulture"),
                    Member(IdentifierName("CultureInfo"), "InvariantCulture"))))
        };

        foreach (var statementLineContext in statementLineContexts)
        {
            var statementEntries = statementLineContext.Accept(visitor).EntriesOrEmpty();
            if (statementEntries.Count == 0)
                continue;

            entries.Add(GeneratedBlankLineEntry.Instance);
            entries.AddRange(statementEntries);
        }

        return BuildCompilationUnit(entries, packageDirectives, usingNames);
    }

    public static GeneratedCode VisitStatementLine(
        Unipi.MppgParser.Grammar.MppgParser.StatementLineContext context,
        MppgBaseVisitor<GeneratedCode> visitor)
    {
        var statementContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.StatementContext>(0);
        var inlineCommentContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.InlineCommentContext>(0);

        if (statementContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.EmptyContext>(0) is not null)
            return GeneratedCode.Empty;

        if (statementContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.VersionDirectiveContext>(0) is not null)
            return GeneratedCode.Entries([new GeneratedCommentEntry(context.GetJoinedText())]);

        if (statementContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.CommentContext>(0) is not null)
        {
            var comment = statementContext.Accept(visitor).EntriesOrEmpty().OfType<GeneratedCommentEntry>().Single();
            if (inlineCommentContext is null)
                return GeneratedCode.Entries([comment]);

            return GeneratedCode.Entries([
                comment with { Text = $"{comment.Text} {inlineCommentContext.GetJoinedText()}" }
            ]);
        }

        var entries = new List<GeneratedCodeEntry>
        {
            new GeneratedCommentEntry($"code for: {MppgReformatVisitor.Reformat(context)}")
        };

        List<GeneratedCodeEntry> statementEntries;
        try
        {
            statementEntries = statementContext.Accept(visitor).EntriesOrEmpty().ToList();
        }
        catch (NotImplementedCodeGenerationException)
        {
            statementEntries = [];
        }

        if (statementEntries.Count == 0)
        {
            entries.Add(new GeneratedCommentEntry("NOT IMPLEMENTED"));
            return GeneratedCode.Entries(entries);
        }

        if (inlineCommentContext is not null)
            statementEntries = AppendTrailingComment(statementEntries, inlineCommentContext.GetJoinedText());

        entries.AddRange(statementEntries);
        return GeneratedCode.Entries(entries);
    }

    public static GeneratedCode VisitAssignment(
        Unipi.MppgParser.Grammar.MppgParser.AssignmentContext context,
        MppgBaseVisitor<GeneratedCode> visitor,
        HashSet<string> declaredVariables,
        Func<Unipi.MppgParser.Grammar.MppgParser.ExpressionContext, string> getDeclarationType)
    {
        var name = context.GetChild(0).GetText();
        var expressionContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.ExpressionContext>(0);
        var expression = expressionContext.Accept(visitor).SingleExpression();

        StatementSyntax statement;
        if (declaredVariables.Add(name))
        {
            statement = LocalDeclarationStatement(
                VariableDeclaration(IdentifierName(getDeclarationType(expressionContext)))
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator(Identifier(name))
                            .WithInitializer(EqualsValueClause(expression)))));
        }
        else
        {
            statement = ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(name),
                    expression));
        }

        return GeneratedCode.Entries([new GeneratedStatementEntry(statement)]);
    }

    public static GeneratedCode VisitExpressionCommand(
        Unipi.MppgParser.Grammar.MppgParser.ExpressionCommandContext context,
        MppgBaseVisitor<GeneratedCode> visitor,
        Func<ExpressionSyntax, ExpressionType, ExpressionSyntax> printedExpression)
    {
        var expressionContext = context.GetChild<Unipi.MppgParser.Grammar.MppgParser.ExpressionContext>(0);
        var expression = expressionContext.Accept(visitor).SingleExpression();

        return GeneratedCode.Entries([
            new GeneratedStatementEntry(ExpressionStatement(
                Invoke(
                    Member(IdentifierName("Console"), "WriteLine"),
                    printedExpression(expression, expressionContext.GetExpressionType()))))
        ]);
    }

    public static GeneratedCode VisitPrintExpressionCommand(
        Unipi.MppgParser.Grammar.MppgParser.PrintExpressionCommandContext context)
    {
        var variableName = context.GetChild(2).GetText();
        return GeneratedCode.Entries([
            new GeneratedStatementEntry(ExpressionStatement(
                Invoke(Member(IdentifierName("Console"), "WriteLine"), IdentifierName(variableName))))
        ]);
    }

    public static GeneratedCode VisitStringExpression(Unipi.MppgParser.Grammar.MppgParser.StringExpressionContext context)
    {
        var computableString = context.Accept(new ComputableStringVisitor());
        if (computableString is null)
            return GeneratedCode.Empty;

        var sb = new StringBuilder("$\"");
        foreach (var piece in computableString.Pieces)
        {
            if (piece is string text)
                sb.Append(text.Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal));
            else if (piece is Expression variable)
                sb.Append('{').Append(variable.VariableName).Append('}');
        }
        sb.Append('"');

        return GeneratedCode.Expression(ParseExpression(sb.ToString()));
    }

    /// <summary>
    /// Shared plot/plotTikz handling: parses the shared argument shapes (functions to plot, settings,
    /// output path) and builds the statements that render and save/print the plot. <paramref name="materialize"/>
    /// converts each function to plot into whatever the plotting API expects (identity for the Nancy API,
    /// where values are already Curve; .Compute() for Nancy.Expressions, where values are still symbolic).
    /// </summary>
    public static GeneratedCode VisitPlotCommand(
        Unipi.MppgParser.Grammar.MppgParser.PlotCommandContext context,
        Func<ExpressionSyntax, ExpressionSyntax> materialize) =>
        BuildPlotStatements(
            context.GetRuleContexts<Unipi.MppgParser.Grammar.MppgParser.PlotArgContext>(),
            materialize,
            PlotOutputKind.Image);

    public static GeneratedCode VisitPlotTikzCommand(
        Unipi.MppgParser.Grammar.MppgParser.PlotTikzCommandContext context,
        Func<ExpressionSyntax, ExpressionSyntax> materialize) =>
        BuildPlotStatements(
            context.GetRuleContexts<Unipi.MppgParser.Grammar.MppgParser.PlotArgContext>(),
            materialize,
            PlotOutputKind.Tikz);

    private static GeneratedCode BuildPlotStatements(
        Unipi.MppgParser.Grammar.MppgParser.PlotArgContext[] args,
        Func<ExpressionSyntax, ExpressionSyntax> materialize,
        PlotOutputKind outputKind)
    {
        var (functionsToPlot, settings, outPath) = ParsePlotArgs(args, outputKind);
        var plottedFunctions = functionsToPlot.Select(name => materialize(IdentifierName(name))).ToArray();

        var settingsInitializer = InitializerExpression(
            SyntaxKind.ObjectInitializerExpression,
            SeparatedList<ExpressionSyntax>(settings.Select(static setting =>
                (ExpressionSyntax)AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression, IdentifierName(setting.Property), setting.Value))));

        var statements = new List<StatementSyntax>();

        if (outputKind == PlotOutputKind.Image)
        {
            statements.Add(VariableDeclarationStatement("plotBytes", Invoke(
                Member(IdentifierName("ScottPlots"), "ToScottPlotImage"),
                Argument(CollectionOf(plottedFunctions)),
                NamedArgument("settings", ObjectCreate("ScottPlotSettings").WithInitializer(settingsInitializer)))));

            if (!string.IsNullOrWhiteSpace(outPath))
            {
                statements.Add(ExpressionStatement(Invoke(Member(IdentifierName("Console"), "WriteLine"),
                    Invoke(Member(IdentifierName("Path"), "GetFullPath"), StringLiteral(outPath)))));
                statements.Add(ExpressionStatement(Invoke(Member(IdentifierName("File"), "WriteAllBytes"),
                    StringLiteral(outPath), IdentifierName("plotBytes"))));
            }
            else
            {
                statements.Add(VariableDeclarationStatement("plotTmpPath", BinaryExpression(SyntaxKind.AddExpression,
                    BinaryExpression(SyntaxKind.AddExpression,
                        Invoke(Member(IdentifierName("Path"), "GetTempPath")),
                        CallMember(Invoke(Member(IdentifierName("Guid"), "NewGuid")), "ToString")),
                    StringLiteral(".png"))));
                statements.Add(ExpressionStatement(Invoke(Member(IdentifierName("Console"), "WriteLine"), IdentifierName("plotTmpPath"))));
                statements.Add(ExpressionStatement(Invoke(Member(IdentifierName("File"), "WriteAllBytes"),
                    IdentifierName("plotTmpPath"), IdentifierName("plotBytes"))));
            }
        }
        else
        {
            var functionNameLiterals = functionsToPlot.Select(name => (ExpressionSyntax)StringLiteral(name)).ToArray();
            statements.Add(VariableDeclarationStatement("plotTikzCode", Invoke(
                Member(IdentifierName("TikzPlots"), "ToTikzPlotCode"),
                Argument(CollectionOf(plottedFunctions)),
                Argument(CollectionOf(functionNameLiterals)),
                NamedArgument("settings", ObjectCreate("TikzPlotSettings").WithInitializer(settingsInitializer)))));

            if (!string.IsNullOrWhiteSpace(outPath))
            {
                statements.Add(ExpressionStatement(Invoke(Member(IdentifierName("Console"), "WriteLine"),
                    Invoke(Member(IdentifierName("Path"), "GetFullPath"), StringLiteral(outPath)))));
                statements.Add(ExpressionStatement(Invoke(Member(IdentifierName("File"), "WriteAllText"),
                    StringLiteral(outPath), IdentifierName("plotTikzCode"))));
            }
            else
            {
                statements.Add(ExpressionStatement(Invoke(Member(IdentifierName("Console"), "WriteLine"), IdentifierName("plotTikzCode"))));
            }
        }

        return GeneratedCode.Entries(statements.Select(static s => (GeneratedCodeEntry)new GeneratedStatementEntry(s)).ToList());
    }

    private static LocalDeclarationStatementSyntax VariableDeclarationStatement(string name, ExpressionSyntax value) =>
        LocalDeclarationStatement(VariableDeclaration(IdentifierName("var"))
            .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier(name))
                .WithInitializer(EqualsValueClause(value)))));

    public static LiteralExpressionSyntax StringLiteral(string value) =>
        LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(value));

    /// <summary>
    /// Parses the arguments shared by <c>plot</c> and <c>plotTikz</c>,
    /// i.e. the functions to plot and the plot settings, the latter as generated code for each property.
    /// </summary>
    private static (List<string> FunctionsToPlot, List<(string Property, ExpressionSyntax Value)> Settings, string OutPath) ParsePlotArgs(
        Unipi.MppgParser.Grammar.MppgParser.PlotArgContext[] args,
        PlotOutputKind outputKind)
    {
        var functionsToPlot = args
            .Select(arg => arg.GetChild<Unipi.MppgParser.Grammar.MppgParser.FunctionNameContext>(0))
            .Where(ctx => ctx is not null)
            .Select(ctx => ctx.GetText())
            .ToList();

        var plotOptionContexts = args
            .Select(arg => arg.GetChild<Unipi.MppgParser.Grammar.MppgParser.PlotOptionContext>(0))
            .Where(ctx => ctx is not null);

        var settings = new List<(string Property, ExpressionSyntax Value)>();
        void SetProperty(string name, ExpressionSyntax value)
        {
            var index = settings.FindIndex(s => s.Property == name);
            if (index >= 0)
                settings[index] = (name, value);
            else
                settings.Add((name, value));
        }

        var outPath = string.Empty;

        if (outputKind == PlotOutputKind.Image)
        {
            // populate then emit these settings first, to mimic default values of PlotSettings;
            // this is skipped for TikZ plots, as Nancy.Plots.Tikz has its own defaults for these
            SetProperty("Title", ParseExpression("string.Empty"));
            SetProperty("XLabel", ParseExpression("string.Empty"));
            SetProperty("YLabel", ParseExpression("string.Empty"));
        }

        foreach (var plotArgContext in plotOptionContexts)
        {
            var argName = plotArgContext.GetChild(0).GetText();
            var argString = plotArgContext.GetChild(2).GetText().TrimQuotes();

            switch (argName)
            {
                case "main":
                case "title":
                    SetProperty("Title", VisitStringExpression(
                        plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.StringExpressionContext>(0)).SingleExpression());
                    break;

                case "xlim":
                    SetProperty("XLimit", IntervalCode(plotArgContext));
                    break;

                case "ylim":
                    SetProperty("YLimit", IntervalCode(plotArgContext));
                    break;

                case "xlab":
                    SetProperty("XLabel", VisitStringExpression(
                        plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.StringExpressionContext>(0)).SingleExpression());
                    break;

                case "ylab":
                    SetProperty("YLabel", VisitStringExpression(
                        plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.StringExpressionContext>(0)).SingleExpression());
                    break;

                case "out":
                    outPath = PlotOutPath.Resolve(argString, outputKind);
                    break;

                // grid, bg: not implemented in Nancy.Plots; gui: not meaningful in convert
                default:
                    break;
            }
        }

        return (functionsToPlot, settings, outPath);
    }

    private static ExpressionSyntax IntervalCode(Unipi.MppgParser.Grammar.MppgParser.PlotOptionContext plotArgContext)
    {
        var intervalContext = plotArgContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.IntervalContext>(0);
        var numberVisitor = new NumberLiteralVisitor();
        var leftLimit = numberVisitor.Visit(intervalContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.RationalLiteralContext>(0));
        var rightLimit = numberVisitor.Visit(intervalContext.GetChild<Unipi.MppgParser.Grammar.MppgParser.RationalLiteralContext>(1));
        return ObjectCreate("Interval", ParseExpression(leftLimit.ToCodeString()), ParseExpression(rightLimit.ToCodeString()));
    }

    public static GeneratedCode VisitComment(Unipi.MppgParser.Grammar.MppgParser.CommentContext context)
    {
        var text = context.GetJoinedText();
        if (text.StartsWith("//", StringComparison.Ordinal))
            text = text[2..].TrimStart();

        return GeneratedCode.Entries([new GeneratedCommentEntry(text)]);
    }

    public static InvocationExpressionSyntax CallMember(
        ExpressionSyntax target,
        string memberName,
        params ExpressionSyntax[] arguments) =>
        Invoke(Member(ParenthesizedExpression(target), memberName), arguments);

    public static MemberAccessExpressionSyntax Member(ExpressionSyntax target, string memberName) =>
        MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            target,
            IdentifierName(memberName));

    public static InvocationExpressionSyntax Invoke(ExpressionSyntax expression, params ExpressionSyntax[] arguments) =>
        Invoke(expression, arguments.Select(Argument).ToArray());

    public static InvocationExpressionSyntax Invoke(ExpressionSyntax expression) =>
        InvocationExpression(expression, ArgumentList());

    public static InvocationExpressionSyntax Invoke(ExpressionSyntax expression, params ArgumentSyntax[] arguments) =>
        InvocationExpression(expression, ArgumentList(SeparatedList(arguments)));

    public static ArgumentSyntax NamedArgument(string name, ExpressionSyntax expression) =>
        Argument(expression).WithNameColon(NameColon(IdentifierName(name)));

    public static ObjectCreationExpressionSyntax ObjectCreate(string typeName, params ExpressionSyntax[] arguments) =>
        ObjectCreationExpression(IdentifierName(typeName))
            .WithArgumentList(ArgumentList(SeparatedList(arguments.Select(Argument))));

    public static LiteralExpressionSyntax IntLiteral(int value) =>
        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(value));

    public static CollectionExpressionSyntax CollectionOf(params ExpressionSyntax[] items) =>
        CollectionExpression(SeparatedList<CollectionElementSyntax>(items.Select(ExpressionElement)));

    /// <summary>
    /// A curve constant at the given value, over all time: the shape a scalar takes when an operator
    /// needs it mixed into a function-typed operand. The caller passes a value already in whatever
    /// form the target API expects (a raw Rational, or one already materialized via .Compute()).
    /// </summary>
    public static ExpressionSyntax ConstantCurveCode(ExpressionSyntax value) =>
        ObjectCreate("Curve",
            ObjectCreate("Sequence", CollectionOf(
                ObjectCreate("Point", IntLiteral(0), value),
                Invoke(Member(IdentifierName("Segment"), "Constant"), IntLiteral(0), IntLiteral(1), value))),
            IntLiteral(0), IntLiteral(1), IntLiteral(0));

    /// <summary>
    /// Shared assertion handling: the two sides are compared with C#'s own operators, except that
    /// Function-typed sides are compared with Curve.Equivalent (so different representations of an
    /// equal curve still compare equal), and a Number side mixed with a Function side is first turned
    /// into a constant curve. <paramref name="materialize"/> supplies whatever conversion a side needs
    /// before it can be compared or embedded in a constant curve: identity for the Nancy API (values are
    /// already Rational/Curve), or .Compute() for Nancy.Expressions (values are still symbolic).
    /// </summary>
    public static GeneratedCode VisitAssertion(
        Unipi.MppgParser.Grammar.MppgParser.AssertionContext context,
        MppgBaseVisitor<GeneratedCode> visitor,
        Func<ExpressionSyntax, ExpressionSyntax> materialize)
    {
        var leftContext = context.expression(0);
        var rightContext = context.expression(1);
        var operatorText = context.assertionOperator().GetText();

        var leftExpr = leftContext.Accept(visitor).SingleExpression();
        var rightExpr = rightContext.Accept(visitor).SingleExpression();
        var leftType = leftContext.GetExpressionType();
        var rightType = rightContext.GetExpressionType();

        if (leftType == ExpressionType.Function && rightType == ExpressionType.Number)
        {
            leftExpr = materialize(leftExpr);
            rightExpr = ConstantCurveCode(materialize(rightExpr));
            rightType = ExpressionType.Function;
        }
        else if (leftType == ExpressionType.Number && rightType == ExpressionType.Function)
        {
            leftExpr = ConstantCurveCode(materialize(leftExpr));
            rightExpr = materialize(rightExpr);
            leftType = ExpressionType.Function;
        }
        else
        {
            leftExpr = materialize(leftExpr);
            rightExpr = materialize(rightExpr);
        }

        if (leftType == ExpressionType.Function && rightType == ExpressionType.Function)
        {
            var equivalentCall = Invoke(Member(IdentifierName("Curve"), "Equivalent"), leftExpr, rightExpr);
            if (operatorText == "=")
                return GeneratedCode.Entries([new GeneratedStatementEntry(PrintToLowerString(equivalentCall))]);
            if (operatorText == "!=")
                return GeneratedCode.Entries([new GeneratedStatementEntry(PrintToLowerString(
                    ParenthesizedExpression(PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, equivalentCall))))]);
        }

        var comparison = ParenthesizedExpression(BinaryExpression(ComparisonKind(operatorText), leftExpr, rightExpr));
        return GeneratedCode.Entries([new GeneratedStatementEntry(PrintToLowerString(comparison))]);
    }

    private static SyntaxKind ComparisonKind(string operatorText) =>
        operatorText switch
        {
            "=" => SyntaxKind.EqualsExpression,
            "!=" => SyntaxKind.NotEqualsExpression,
            "<" => SyntaxKind.LessThanExpression,
            "<=" => SyntaxKind.LessThanOrEqualExpression,
            ">" => SyntaxKind.GreaterThanExpression,
            ">=" => SyntaxKind.GreaterThanOrEqualExpression,
            _ => SyntaxKind.EqualsExpression
        };

    private static StatementSyntax PrintToLowerString(ExpressionSyntax expression)
    {
        var toStringCall = Invoke(Member(expression, "ToString"));
        var toLowerCall = Invoke(Member(toStringCall, "ToLower"));
        return ExpressionStatement(Invoke(Member(IdentifierName("Console"), "WriteLine"), toLowerCall));
    }

    private static CompilationUnitSyntax BuildCompilationUnit(
        IReadOnlyList<GeneratedCodeEntry> entries,
        IEnumerable<string> packageDirectives,
        IEnumerable<string> usingNames)
    {
        var members = new List<MemberDeclarationSyntax>();
        var pendingTrivia = new StringBuilder();

        foreach (var entry in entries)
        {
            switch (entry)
            {
                case GeneratedBlankLineEntry:
                    pendingTrivia.AppendLine();
                    break;

                case GeneratedCommentEntry comment:
                    pendingTrivia.Append("// ");
                    pendingTrivia.AppendLine(comment.Text);
                    break;

                case GeneratedStatementEntry statement:
                    members.Add(GlobalStatement(statement.Statement.WithLeadingTrivia(
                        ParseLeadingTrivia(pendingTrivia.ToString()))));
                    pendingTrivia.Clear();
                    break;
            }
        }

        pendingTrivia.AppendLine();
        pendingTrivia.AppendLine("// END OF PROGRAM");

        var usings = usingNames
            .Select(name => UsingDirective(ParseName(name)))
            .ToList();

        if (usings.Count > 0)
        {
            usings[0] = usings[0].WithLeadingTrivia(ParseLeadingTrivia(
                string.Concat(packageDirectives.Select(static directive => $"{directive}\n"))
                + "\n"));
        }

        return CompilationUnit()
            .WithUsings(List(usings))
            .WithMembers(List(members))
            .WithEndOfFileToken(Token(
                ParseLeadingTrivia(pendingTrivia.ToString()),
                SyntaxKind.EndOfFileToken,
                TriviaList()));
    }

    private static List<GeneratedCodeEntry> AppendTrailingComment(
        IReadOnlyList<GeneratedCodeEntry> entries,
        string comment)
    {
        var result = entries.ToList();
        for (var index = result.Count - 1; index >= 0; index--)
        {
            if (result[index] is not GeneratedStatementEntry statementEntry)
                continue;

            result[index] = statementEntry with
            {
                Statement = statementEntry.Statement.WithTrailingTrivia(
                    TriviaList(Comment($" // {comment}")))
            };
            break;
        }

        return result;
    }
}
