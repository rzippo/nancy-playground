using Antlr4.Runtime;
using Unipi.MppgParser.Grammar;
using Unipi.Nancy.Playground.MppgParser.Statements;

namespace Unipi.Nancy.Playground.MppgParser.Visitors;

public class ProgramVisitor : MppgBaseVisitor<Program>
{
     private readonly IReadOnlyList<SyntaxErrorInfo> _syntaxErrors;

     public ProgramVisitor(IReadOnlyList<SyntaxErrorInfo>? syntaxErrors = null)
     {
          _syntaxErrors = syntaxErrors ?? [];
     }

     public override Program VisitProgram(Unipi.MppgParser.Grammar.MppgParser.ProgramContext context)
     {
          List<Statement> statements = [];
          for (int i = 0; i < context.ChildCount; i++)
          {
               var child = context.GetChild(i);
               if (child is Unipi.MppgParser.Grammar.MppgParser.StatementLineContext statementLine)
               {
                    var syntaxError = FindSyntaxError(statementLine);
                    var visitor = new StatementVisitor(syntaxError);
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
                    statements.Add(statement);
               }
          }

          var program = new Program(statements);
          return program;
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
