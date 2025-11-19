
using System.Runtime.CompilerServices;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;

namespace NancyMppg.Nancy.Plots;

public abstract partial class NancyPlotter<TChart>
{
    /// <summary>
    /// Returns a byte array representing an image of the plot.
    /// Specific for <see cref="TChart"/>.
    /// </summary>
    /// <param name="plot"></param>
    /// <remarks>
    /// This method is, in the default implementation, used internally by all PlotToImage overloads.
    /// </remarks>
    public abstract byte[] GetImage(TChart plot);
    
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
    public byte[] PlotToImage(
        IReadOnlyList<Curve> curves,
        IReadOnlyList<string> names,
        Rational? upTo = null
    )
    {
        var plot = Plot(curves, names, upTo);
        return GetImage(plot);
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
    public byte[] PlotToImage(
        Curve curve,
        [CallerArgumentExpression("curve")] string name = "f",
        Rational? upTo = null
    )
    {
        var plot = Plot([curve], [name], upTo);
        return GetImage(plot);
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
    public byte[] PlotToImage(
        IReadOnlyList<Curve> curves,
        Rational? upTo = null
    )
    {
        var names = curves
            .Select((_, i) => $"{(char)('f' + i)}")
            .ToList();
        var plot = Plot(curves, names, upTo);
        return GetImage(plot);
    }

    /// <summary>
    /// Plots a set of curves.
    /// The curves will be given default names f, g, h and so on.
    /// The x-axis right edge of the plot will be set to $max_{i}(T_i + 2 * d_i)$.
    /// </summary>
    /// <param name="curves">The curves to plot.</param>
    /// <returns>A <see cref="TChart"/> object.</returns>
    public byte[] PlotToImage(
        params Curve[] curves
    )
    {
        var plot = Plot(curves, null);
        return GetImage(plot);
    }

    #endregion

    #region Sequences

    /// <summary>
    /// Plots a set of sequences.
    /// </summary>
    /// <param name="sequences">The sequences to plot.</param>
    /// <param name="names">The names of the sequences.</param>
    /// <returns>A <see cref="TChart"/> object.</returns>
    public byte[] PlotToImage(
        IReadOnlyList<Sequence> sequences,
        IReadOnlyList<string> names
    )
    {
        var plot = Plot(sequences, names);
        return GetImage(plot);
    }

    /// <summary>
    /// Plots a set of sequences.
    /// The sequences will be given default names f, g, h and so on.
    /// </summary>
    /// <param name="sequences">The sequences to plot.</param>
    /// <returns>A <see cref="TChart"/> object.</returns>
    public byte[] PlotToImage(
        IReadOnlyList<Sequence> sequences
    )
    {
        var names = sequences
            .Select((_, i) => $"{(char)('f' + i)}")
            .ToList();
        var plot = Plot(sequences, names);
        return GetImage(plot);
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
    public byte[] PlotToImage(
        Sequence sequence,
        [CallerArgumentExpression("sequence")] string name = "f"
    )
    {
        var plot = Plot([sequence], [name]);
        return GetImage(plot);
    }
    
    #endregion
}