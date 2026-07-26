using Antlr4.Runtime;
using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

public class ProgramVisitor : MppgBaseVisitor<Program>
{
     private readonly IReadOnlyList<SyntaxErrorInfo> _syntaxErrors;
     private readonly SyntaxVersion _syntaxVersion;

     public ProgramVisitor(
          IReadOnlyList<SyntaxErrorInfo>? syntaxErrors = null,
          SyntaxVersion syntaxVersion = default)
     {
          _syntaxErrors = syntaxErrors ?? [];
          _syntaxVersion = syntaxVersion == default ? SyntaxVersion.Latest : syntaxVersion;
     }

     public override Program VisitProgram(Unipi.MppgParser.Grammar.MppgParser.ProgramContext context)
     {
          List<Statement> statements = [];
          for (int i = 0; i < context.ChildCount; i++)
          {
               var child = context.GetChild(i);
               if (child is Unipi.MppgParser.Grammar.MppgParser.PreambleContext)
               {
                    // preamble is metadata, handled above — no statements from it
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
                    // All version directives in statement position are duplicates/warnings.
                    // Only preamble version directives are valid.
                    if (statement is VersionDirectiveStatement vds)
                    {
                         statement = vds with { IsDuplicate = true };
                    }
                    statements.Add(statement);
               }
          }

          var program = new Program(statements)
          {
               SyntaxVersion = _syntaxVersion
          };
          return program;
     }

     public override Program VisitPreamble(Unipi.MppgParser.Grammar.MppgParser.PreambleContext context)
     {
          // The preamble is only visited via VisitProgram above — the parser action
          // ParseVersionFromShebang already set the parser's version.
          return new Program([]);
     }

     public override Program VisitPreambleStatement(Unipi.MppgParser.Grammar.MppgParser.PreambleStatementContext context)
     {
          return new Program([]);
     }

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
