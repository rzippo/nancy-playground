using Antlr4.Runtime;
using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Playground.MppgParser.Statements;
using Unipi.Nancy.Playground.MppgParser.Utility;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

/// <summary>
/// Builds a program from its parse tree, i.e. the statements it is made of.
/// </summary>
public class ProgramVisitor : MppgBaseVisitor<Program>
{
     private readonly IReadOnlyList<SyntaxErrorInfo> _syntaxErrors;
     private readonly SyntaxVersion _syntaxVersion;

     /// <summary>
     /// A visitor carrying <paramref name="syntaxErrors"/> onto the program it builds, and reading it with <paramref name="syntaxVersion"/>.
     /// </summary>
     public ProgramVisitor(
          IReadOnlyList<SyntaxErrorInfo>? syntaxErrors = null,
          SyntaxVersion syntaxVersion = default)
     {
          _syntaxErrors = syntaxErrors ?? [];
          _syntaxVersion = syntaxVersion == default ? SyntaxVersion.Latest : syntaxVersion;
     }

     /// <summary>
     /// Builds the program, statement by statement.
     /// </summary>
     public override Program VisitProgram(Unipi.MppgParser.Grammar.MppgParser.ProgramContext context)
     {
          List<Statement> statements = [];
          for (int i = 0; i < context.ChildCount; i++)
          {
               var child = context.GetChild(i);
               if (child is Unipi.MppgParser.Grammar.MppgParser.PreambleContext preamble)
               {
                    // the directive that was applied is metadata and says nothing, the others report that they were not
                    statements.AddRange(DirectivesNotApplied(preamble));
                    continue;
               }
               if (child is Unipi.MppgParser.Grammar.MppgParser.StatementLineContext statementLine)
               {
                    var syntaxError = FindSyntaxError(statementLine);
                    var visitor = new StatementVisitor(syntaxError, _syntaxVersion);
                    var statement = statementLine.Accept(visitor);
                    if (statement is null)
                    {
                         statement = new SyntaxErrorStatement
                         {
                              Text = statementLine.GetJoinedText(),
                              SyntaxError = syntaxError,
                              Message = "Statement could not be parsed."
                         };
                    }
                    // a directive in statement position is never the one applied, that being the one the program opens with
                    if (statement is VersionDirectiveStatement vds)
                    {
                         statement = vds with { IsDuplicate = true, ActiveVersion = _syntaxVersion };
                    }
                    statement = statement with
                    {
                         Warnings = ScalarDivisionGrouping.WarningsFor(statementLine)
                    };
                    statements.Add(statement);
               }
          }

          var program = new Program(statements)
          {
               SyntaxVersion = _syntaxVersion
          };
          return program;
     }

     /// <summary>
     /// The directives of the preamble that were not applied, as the statements that report so.
     /// </summary>
     /// <remarks>
     /// Only the directive the program opens with is applied, so a second one on the next line is as ineffective as one written after a statement, and is told so the same way.
     /// A directive declaring a version this build cannot apply is left out, being reported as an error by <see cref="Program.FromText"/>.
     /// </remarks>
     private IEnumerable<Statement> DirectivesNotApplied(Unipi.MppgParser.Grammar.MppgParser.PreambleContext preamble)
     {
          foreach (var preambleStatement in preamble.preambleStatement())
          {
               if (preambleStatement.versionDirective() is not { } directive || IsTheOneApplied(directive))
                    continue;

               var text = directive.GetJoinedText();
               if (VersionDirective.Read(text, out _) is not { } version)
                    continue;

               yield return new VersionDirectiveStatement(version)
               {
                    Text = text,
                    IsDuplicate = true,
                    ActiveVersion = _syntaxVersion
               };
          }
     }

     /// <summary>
     /// True for the directive the program opens with, which is the one the lexer applies.
     /// </summary>
     private static bool IsTheOneApplied(Unipi.MppgParser.Grammar.MppgParser.VersionDirectiveContext directive)
          => directive.Start?.TokenIndex == 0;

     /// <summary>
     /// Reads the preamble, which is where a version directive is allowed to stand.
     /// </summary>
     public override Program VisitPreamble(Unipi.MppgParser.Grammar.MppgParser.PreambleContext context)
     {
          // The preamble is only visited via VisitProgram above — the parser action
          // ParseVersionFromShebang already set the parser's version.
          return new Program([]);
     }

     /// <summary>
     /// Reads one line of the preamble.
     /// </summary>
     public override Program VisitPreambleStatement(Unipi.MppgParser.Grammar.MppgParser.PreambleStatementContext context)
     {
          return new Program([]);
     }

     /// <summary>
     /// Reads the version directive of the preamble.
     /// </summary>
     public override Program VisitVersionDirective(Unipi.MppgParser.Grammar.MppgParser.VersionDirectiveContext context)
     {
          return new Program([]);
     }

     private SyntaxErrorInfo? FindSyntaxError(ParserRuleContext context)
     {
          var startLine = context.Start?.Line ?? 0;
          var stopLine = context.Stop?.Line ?? startLine;

          return _syntaxErrors.FirstOrDefault(e =>
                    e.Line >= startLine && e.Line <= stopLine)
               ?? _syntaxErrors.FirstOrDefault(e =>
                    e.Line == startLine);
     }
}
