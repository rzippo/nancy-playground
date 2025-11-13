namespace Unipi.MppgParser;

public class ProgramContext
{
    public State State { get; init; } =  new ();

    public StatementOutput? ExecuteStatement(
        Statement statement,
        IStatementFormatter formatter,
        bool immediateComputeValue
    )
    {
        formatter.FormatStatementPreamble(statement);
        try
        {
            var output = statement switch
            {
                Assignment assignment => assignment.ExecuteToFormattable(State, immediateComputeValue),
                _ => statement.ExecuteToFormattable(State)
            };
            formatter.FormatStatementOutput(statement, output);
            return output;
        }
        catch (Exception e)
        {
            #if DEBUG
            throw;
            #else
            var error = new ErrorOutput
            {
                StatementText = statement.Text,
                OutputText = string.Empty,
                Exception = e
            };
            formatter.FormatError(statement, error);
            return error;
            #endif
        }
    }
}