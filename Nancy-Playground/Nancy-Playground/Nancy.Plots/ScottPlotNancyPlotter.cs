using ScottPlot;
using Unipi.Nancy.MinPlusAlgebra;

namespace NancyMppg.Nancy.Plots;

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
            var sequenceTrace = new SequenceTrace(sequence);

            if (sequence.IsContinuous)
            {
                var pointCoordinates = sequenceTrace.Points
                    .Select(p => new Coordinates(p.x, p.y))
                    .ToList();
                
                if (sequence.IsLeftOpen)
                {
                    var head = sequenceTrace.Segments.First();
                    pointCoordinates.Insert(0, new Coordinates(head.a.x, head.a.y));
                }
                
                if (sequence.IsRightOpen)
                {
                    var tail = sequenceTrace.Segments.Last();
                    pointCoordinates.Add(new Coordinates(tail.b.x, tail.b.y));
                }
                
                var lineScatter = plot.Add.ScatterLine(pointCoordinates);
                lineScatter.Color = color;
                lineScatter.MarkerShape = MarkerShape.FilledCircle;
                lineScatter.LegendText = names[idx];
            }
            else
            {
                if (sequenceTrace.Points.Any())
                {
                    var pointCoordinates = sequenceTrace.Points
                        .Select(p => new Coordinates(p.x, p.y))
                        .ToArray();
                    var pointsScatter = plot.Add.ScatterPoints(pointCoordinates);
                    pointsScatter.Color = color;
                    pointsScatter.MarkerShape = MarkerShape.FilledCircle;
                    if (!sequenceTrace.Segments.Any())
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

                if (sequenceTrace.Segments.Any())
                {
                    var legendApplied = false;
                    foreach (var segment in sequenceTrace.Segments)
                    {
                        var segmentCoordinates = new Coordinates[]
                        {
                            new (segment.a.x, segment.a.y),
                            new (segment.b.x, segment.b.y),
                        };
                        var segmentScatter = plot.Add.ScatterLine(segmentCoordinates);
                        segmentScatter.Color = color;
                        if (!legendApplied)
                        {
                            segmentScatter.LegendText = names[idx];
                            legendApplied = true;
                        }
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

    private class SequenceTrace
    {
        public List<((double x, double y) a, (double x, double y) b)> Segments { get; } = [];
        
        public List<(double x, double y)> Points { get; } = [];
        
        public List<(double x, double y)> Discontinuities { get; } = [];
        
        public SequenceTrace(Sequence sequence)
        {
            var breakpoints = sequence.EnumerateBreakpoints();
            foreach (var (left, center, right) in breakpoints)
            {
                if( center is not { IsPlusInfinite: true })
                    Points.Add((x: (double)center.Time, y: (double)center.Value));
                    
                if (left is not null and not { IsPlusInfinite: true } && left.LeftLimitAtEndTime != center.Value)
                {
                    Discontinuities.Add((x: (double)center.Time, y: (double)left.LeftLimitAtEndTime));
                }

                if (right is not null and not { IsPlusInfinite: true })
                {
                    Segments.Add((
                        a: (x: (double)right.StartTime, y: (double)right.RightLimitAtStartTime),
                        b: (x: (double)right.EndTime, y: (double)right.LeftLimitAtEndTime)
                    ));
                    if (right.RightLimitAtStartTime != center.Value)
                    {
                        Discontinuities.Add((x: (double)center.Time, y: (double)right.RightLimitAtStartTime));
                    }
                }
            }

            if (sequence.IsRightOpen)
            {
                var tail = (Segment)sequence.Elements.Last();
                if (tail is not { IsPlusInfinite: true }) {
                    Segments.Add((
                        a: (x: (double)tail.StartTime, y: (double)tail.RightLimitAtStartTime),
                        b: (x: (double)tail.EndTime, y: (double)tail.LeftLimitAtEndTime)
                    ));
                }
            }
        }
    }
}