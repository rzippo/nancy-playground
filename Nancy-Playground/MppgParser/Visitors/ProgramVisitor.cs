using System.Text;
using Unipi.MppgParser.Grammar;

namespace Unipi.MppgParser.Visitors;

public class ProgramVisitor : MppgBaseVisitor<Program>
{
     public override Program VisitProgram(Grammar.MppgParser.ProgramContext context)
     {
          List<Statement> statements = [];
          for (int i = 0; i < context.ChildCount; i++)
          {
               var child = context.GetChild(i);
               if (child is Grammar.MppgParser.StatementLineContext)
               {
                    var visitor = new StatementVisitor();
                    var statement = child.Accept(visitor);
                    statements.Add(statement);
               }
          }
          
          var program = new Program(statements);
          return program;
     }
}