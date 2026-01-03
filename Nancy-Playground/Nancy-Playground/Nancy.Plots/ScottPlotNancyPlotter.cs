using ScottPlot;
using Unipi.Nancy.MinPlusAlgebra;

namespace Unipi.Nancy.Playground.Cli.Nancy.Plots;

public class ScottPlotNancyPlotter : NancyPlotter<Plot>
{
    public override Plot Plot(
        IReadOnlyList<Sequence> sequences, 
        IReadOnlyList<string> names
    )
    {
        var colors = new List<string>
        {
            "#636EFA",
            "#EF553B",
            "#00CC96",
            "#AB63FA",
            "#FFA15A",
            "#19D3F3",
            "#FF6692",
            "#B6E880",
            "#FF97FF",
            "#FECB52"
        };

        var plot = new Plot();

        foreach (var (sequence, idx) in sequences.WithIndex())
        {
            var color = Color.FromHex(colors[idx % colors.Count]);
            var sequenceTrace = new SequenceTraces(sequence);

            if (sequenceTrace.Points.Any())
            {
                var pointCoordinates = sequenceTrace.Points
                    .Select(p => new Coordinates(p.x, p.y))
                    .ToArray();
                var pointsScatter = plot.Add.ScatterPoints(pointCoordinates);
                pointsScatter.Color = color;
                pointsScatter.MarkerShape = MarkerShape.FilledCircle;
                if (!sequenceTrace.ContinuousLines.Any())
                    pointsScatter.LegendText = names[idx];
            }

            if (sequenceTrace.Discontinuities.Any())
            {
                var discontinuityCoordinates = sequenceTrace.Discontinuities
                    .Select(p => new Coordinates(p.x, p.y))
                    .ToArray();
                var discontinuityScatter = plot.Add.ScatterPoints(discontinuityCoordinates);
                discontinuityScatter.Color = color;
                discontinuityScatter.MarkerShape = MarkerShape.OpenCircle;
                // discontinuityScatter.LegendText = names[idx];
            }

            if (sequenceTrace.ContinuousLines.Any())
            {
                var legendApplied = false;
                foreach (var continuousLine in sequenceTrace.ContinuousLines)
                {
                    var coordinates = continuousLine
                        .Select(p => new Coordinates(p.x, p.y))
                        .ToArray();
                    var lineScatter = plot.Add.ScatterLine(coordinates);
                    lineScatter.Color = color;
                    if (!legendApplied)
                    {
                        lineScatter.LegendText = names[idx];
                        legendApplied = true;
                    }
                }
            }
        }

        return plot;
    }

    public override string GetHtml(Plot plot)
    {
        throw new NotImplementedException();
    }

    public override byte[] GetImage(Plot plot)
    {
        // todo: make format and size configurable?
        return plot.GetImageBytes(1200, 800, ImageFormat.Png);
    }

    private class SequenceTraces
    {
        public List<List<(double x, double y)>> ContinuousLines { get; } = [];

        public List<(double x, double y)> Points { get; } = [];

        public List<(double x, double y)> Discontinuities { get; } = [];

        public SequenceTraces(Sequence sequence)
        {
            var currentLine = new List<(double x, double y)>();
            if (sequence.IsLeftOpen)
            {
                var firstSegment = (Segment)sequence.Elements.First();
                var startCoord = StartCoord(firstSegment);
                Discontinuities.Add(startCoord);
                currentLine = [ startCoord ];   
            }
            var breakpoints = sequence.EnumerateBreakpoints();
            foreach (var (left, center, right) in breakpoints)
            {
                if (left is not null and not { IsPlusInfinite: true } &&
                    right is not null and not { IsPlusInfinite: true } &&
                    left.LeftLimitAtEndTime == center.Value && center.Value == right.RightLimitAtStartTime
                   )
                {
                    // continue the current line
                    currentLine.Add(Coord(center));
                }
                else
                {
                    if (left is null or {IsInfinite: true})
                    {
                        // no line is running yet
                        if (center is not {IsInfinite: true})
                            Points.Add(Coord(center));
                        if (right is not null and not { IsInfinite: true })
                        {
                            var startCoord = ((double)right.StartTime, (double)right.RightLimitAtStartTime);
                            if(right.RightLimitAtStartTime != center.Value)
                                Discontinuities.Add(startCoord);
                            // start new line
                            currentLine = [startCoord];
                        }
                    }
                    else
                    {
                        // left is finite, and assumed within the sequence
                        // first, continue the running line
                        var leftEndCoord = EndCoord(left); 
                        currentLine.Add(leftEndCoord);
                        // if any discontinuity occurs, break the line
                        // by above checks, the discontinuity SHOULD occurr
                        if (center is { IsInfinite: true } ||
                            right is null or { IsInfinite: true } ||
                            left.LeftLimitAtEndTime != center.Value ||
                            center.Value != right.RightLimitAtStartTime
                           )
                        {
                            ContinuousLines.Add(currentLine);
                            if (left.LeftLimitAtEndTime != center.Value)
                                Discontinuities.Add(leftEndCoord);
                            if (center is not {IsInfinite:true})
                                Points.Add(Coord(center));
                            if (right is not null and not { IsInfinite: true })
                            {
                                // start new line immediately
                                var rightStartCoord = StartCoord(right);
                                currentLine = [rightStartCoord];
                                if(center.Value != right.RightLimitAtStartTime)
                                    Discontinuities.Add(rightStartCoord);
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("Should never get here!");
                        }
                    }
                }
            }

            if (sequence.IsRightOpen)
            {
                var lastSegment = (Segment)sequence.Elements.Last();
                if (lastSegment is not { IsPlusInfinite: true }) {
                    var lastCoord = EndCoord(lastSegment);
                    currentLine.Add(lastCoord);
                    ContinuousLines.Add(currentLine);
                    Discontinuities.Add(lastCoord);
                }
            }
        }
    }

    private static (double x, double y) StartCoord(Segment segment)
    {
        return ((double)segment.StartTime, (double)segment.RightLimitAtStartTime);
    }

    private static (double x, double y) EndCoord(Segment segment)
    {
        return ((double)segment.EndTime, (double)segment.LeftLimitAtEndTime);
    }

    private static (double x, double y) Coord(Point point)
    {
        return ((double)point.Time, (double)point.Value);
    }
}