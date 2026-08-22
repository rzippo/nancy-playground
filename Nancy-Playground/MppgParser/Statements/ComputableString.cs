using System.Text;

namespace Unipi.Nancy.Playground.MppgParser.Statements;

/// <summary>
/// A string built of literal text and expressions, which are evaluated when the whole is computed.
/// </summary>
public class ComputableString
{
    internal List<object> Pieces { get; } = [];

    /// <summary>
    /// Appends literal text.
    /// </summary>
    public void Append(string s)
    {
        Pieces.Add(s);
    }

    /// <summary>
    /// Appends an expression, to be evaluated when the string is computed.
    /// </summary>
    public void Append(Expression e)
    {
        Pieces.Add(e);
    }

    /// <summary>
    /// Appends the pieces of another string to this one.
    /// </summary>
    public void Concat(ComputableString cs)
    {
        Pieces.AddRange(cs.Pieces);
    }

    /// <summary>
    /// The text, with every expression evaluated against <paramref name="state"/>.
    /// </summary>
    public string Compute(State state)
    {
        var sb = new StringBuilder();
        foreach (var piece in Pieces)
        {
            if(piece is string s)
                sb.Append(s);
            else if (piece is Expression expression)
            {
                expression.ParseTree(state);
                var (c, r) = expression.Compute();
                if(c is not null)
                    sb.Append(c);
                else if (r is not null)
                    sb.Append(r);
            }
        }
        return sb.ToString();
    }
}
