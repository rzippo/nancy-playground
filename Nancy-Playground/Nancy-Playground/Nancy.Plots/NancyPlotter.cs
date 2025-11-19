using System.Runtime.CompilerServices;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;

namespace NancyMppg.Nancy.Plots;

/// <summary>
/// Abstract class for plotters of Nancy types.
/// </summary>
/// <typeparam name="TChart">
/// The type of the generated plot, usually belongs to a specific plotting library. 
/// </typeparam>
public abstract partial class NancyPlotter<TChart>
{
    #region Curves
    
    /// <summary>
    /// Plots a set of curves.
    /// </summary>
    /// <param name="curves">The curves to plot.</param>
    /// <param name="names">The names of the curves.</param>
    /// <param name="upTo">
    /// The x-axis right edge of the plot.
    /// If null, it is set by default as $max_{i}(T_i + 2 * d_i)$.
    /// </param>
    /// <returns>A <see cref="TChart"/> object.</returns>
    public TChart Plot(
        IReadOnlyList<Curve> curves,
        IReadOnlyList<string> names,
        Rational? upTo = null
    )
    {
        Rational t;
        if (upTo is not null)
            t = (Rational)upTo;
        else
            t = curves.Max(c => c.SecondPseudoPeriodEnd);
        t = t == 0 ? 10 : t;

        var cuts = curves
            .Select(c => c.Cut(0, t, isEndIncluded: true))
            .ToList();

        return Plot(cuts, names);
    }

    /// <summary>
    /// Plots a curve.
    /// </summary>
    /// <param name="curve">The curve to plot.</param>
    /// <param name="name">
    /// The name of the curve.
    /// By default, it captures the expression used for <paramref name="curve"/>.
    /// </param>
    /// <param name="upTo">
    /// The x-axis right edge of the plot.
    /// If null, it is set by default as $T + 2 * d$.
    /// </param>
    /// <returns>A <see cref="TChart"/> object.</returns>
    public TChart Plot(
        Curve curve,
        [CallerArgumentExpression("curve")] string name = "f",
        Rational? upTo = null
    )
    {
        return Plot([curve], [name], upTo);
    }

    /// <summary>
    /// Plots a set of curves.
    /// The curves will be given default names f, g, h and so on.
    /// </summary>
    /// <param name="curves">The curves to plot.</param>
    /// <param name="upTo">
    /// The x-axis right edge of the plot.
    /// If null, it is set by default as $max_{i}(T_i + 2 * d_i)$.
    /// </param>
    /// <returns>A <see cref="TChart"/> object.</returns>
    public TChart Plot(
        IReadOnlyList<Curve> curves,
        Rational? upTo = null
    )
    {
        var names = curves
            .Select((_, i) => $"{(char)('f' + i)}")
            .ToList();
        return Plot(curves, names, upTo);
    }

    /// <summary>
    /// Plots a set of curves.
    /// The curves will be given default names f, g, h and so on.
    /// The x-axis right edge of the plot will be set to $max_{i}(T_i + 2 * d_i)$.
    /// </summary>
    /// <param name="curves">The curves to plot.</param>
    /// <returns>A <see cref="TChart"/> object.</returns>
    public TChart Plot(
        params Curve[] curves
    )
    {
        return Plot(curves, null);
    }

    #endregion Curves
    
    #region Sequences

    /// <summary>
    /// Plots a set of sequences.
    /// </summary>
    /// <param name="sequences">The sequences to plot.</param>
    /// <param name="names">The names of the sequences.</param>
    /// <returns>A <see cref="TChart"/> object.</returns>
    public abstract TChart Plot(
        IReadOnlyList<Sequence> sequences,
        IReadOnlyList<string> names
    );
    
    /// <summary>
    /// Plots a set of sequences.
    /// The sequences will be given default names f, g, h and so on.
    /// </summary>
    /// <param name="sequences">The sequences to plot.</param>
    /// <returns>A <see cref="TChart"/> object.</returns>
    public TChart Plot(
        IReadOnlyList<Sequence> sequences
    )
    {
        var names = sequences
            .Select((_, i) => $"{(char)('f' + i)}")
            .ToList();
        return Plot(sequences, names);
    }
    
    /// <summary>
    /// Plots a sequence.
    /// </summary>
    /// <param name="sequence">The sequence to plot.</param>
    /// <param name="name">
    /// The name of the sequence.
    /// By default, it captures the expression used for <paramref name="sequence"/>.
    /// </param>
    /// <returns>A <see cref="TChart"/> object.</returns>
    public TChart Plot(
        Sequence sequence,
        [CallerArgumentExpression("sequence")] string name = "f"
    )
    {
        return Plot([sequence], [name]);
    }


    #endregion Sequences
}