using System.Text;
using System.Text.RegularExpressions;
using Unipi.MppgParser.Grammar;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;

namespace Unipi.MppgParser.Visitors;

class ToNancyCodeVisitor : MppgBaseVisitor<List<string>>
{
    private ExpressionTypeVisitor TypeVisitor { get; set; } = new();
    
    public override List<string> VisitProgram(Grammar.MppgParser.ProgramContext context)
    {
        var statementLineContexts = context.GetRuleContexts<Grammar.MppgParser.StatementLineContext>();

        List<string> code = [
            "#:package Unipi.Nancy@1.2.20",
            string.Empty,
            "using Unipi.Nancy.NetworkCalculus;",
            "using Unipi.Nancy.MinPlusAlgebra;",
            "using Unipi.Nancy.Numerics;"
        ];
        
        foreach (var statementLineContext in statementLineContexts)
        {
            var statementLineCode = statementLineContext.Accept(this);
            if (statementLineCode.Count <= 0) continue;
            code.Add(string.Empty);
            code.AddRange(statementLineCode);
        }

        code.Add(string.Empty);
        code.Add("// END OF PROGRAM");

        code = CleanupReassignments(code);
        
        return code;
    }

    public override List<string> VisitStatementLine(Grammar.MppgParser.StatementLineContext context)
    {
        var statementContext = context.GetChild<Grammar.MppgParser.StatementContext>(0);
        var inlineCommentContext = context.GetChild<Grammar.MppgParser.InlineCommentContext>(0);

        if (statementContext.GetChild<Grammar.MppgParser.EmptyContext>(0) is not null)
        {
            return [];
        }
        if (statementContext.GetChild<Grammar.MppgParser.CommentContext>(0) is not null)
        {
            var comment = statementContext.Accept(this).Single();
            if (inlineCommentContext != null)
            {
                var inlineComment = inlineCommentContext.GetJoinedText();
                comment = $"{comment} {inlineComment}";
            }
            return [comment];
        }
        else
        {
            List<string> code = [
                $"// code for: {context.GetJoinedText()}"
            ];
            
            var statementCode = statementContext.Accept(this);
            if(statementCode != null)
            {
                if (inlineCommentContext != null)
                {
                    var inlineComment = inlineCommentContext.GetJoinedText();
                    statementCode[^1] = $"{statementCode[^1]} // {inlineComment}";
                }
                code.AddRange(statementCode);
            }
            else
            {
                code.Add("// NOT IMPLEMENTED");
            }
            
            return code;
        }
    }

    public override List<string> VisitAssignment(Grammar.MppgParser.AssignmentContext context)
    {
        var name = context.GetChild(0).GetText();
        var expressionContext = context.GetChild<Grammar.MppgParser.ExpressionContext>(0);
        
        var expressionCode = expressionContext.Accept(this);
        var expressionType = expressionContext.Accept(TypeVisitor);
        var lhs = TypeVisitor.State.ContainsKey(name) ? $"{name}" : $"var {name}";
        List<string> result;
        if (expressionCode is null || expressionCode.Count == 0)
            // throw new InvalidOperationException("Expression code empty");
            result = [$"// {lhs} = ...;"];
        else if (expressionCode.Count == 1)
            result = [$"{lhs} = {expressionCode.Single()};"];
        else
        {
            expressionCode[^1] = $"{lhs} = {expressionCode[^1]};";
            result = expressionCode;
        }

        TypeVisitor.State[name] = expressionType;
        return result;
    }

    public override List<string> VisitExpressionCommand(Grammar.MppgParser.ExpressionCommandContext context)
    {
        var expressionContext = context.GetChild<Grammar.MppgParser.ExpressionContext>(0);
        var expression = expressionContext.Accept(this).Single();

        return [$"Console.WriteLine({expression});"];
    }

    public override List<string> VisitNumberVariableExp(Grammar.MppgParser.NumberVariableExpContext context)
    {
        var name = context.GetChild(0).GetText();
        return [name];
    }

    public override List<string> VisitEncNumberVariableExp(Grammar.MppgParser.EncNumberVariableExpContext context)
    {
        var name = context.GetChild(0).GetText();
        return [name];
    }

    public override List<string> VisitComment(Grammar.MppgParser.CommentContext context)
    {
        var text = context.GetJoinedText();
        return [$"// {text}"];
    }

    public override List<string> VisitFunctionVariableExp(Grammar.MppgParser.FunctionVariableExpContext context)
    {
        var name = context.GetChild(0).GetText();
        return [name];
    }
    
    public override List<string> VisitFunctionName(Grammar.MppgParser.FunctionNameContext context)
    {
        var name = context.GetChild(0).GetText();
        return [name];
    }

    public override List<string> VisitNumberLiteral(Grammar.MppgParser.NumberLiteralContext context)
    {
        var visitor = new NumberLiteralVisitor();
        var number = context.Accept(visitor);
        return [ExplicitToCodeString(number)];
        
        // todo: include this in Nancy
        string ExplicitToCodeString(Rational r)
        {
            var sb = new StringBuilder();
            sb.Append("new Rational(");
            sb.Append(r.Numerator.ToString());
            if (r.Denominator != 1)
            {
                sb.Append(", ");
                sb.Append(r.Denominator.ToString());
            }
            sb.Append(")");

            return sb.ToString();
        }
    }

    public override List<string> VisitFunctionBrackets(Grammar.MppgParser.FunctionBracketsContext context)
    {
        var innerCode = context.GetChild(1).Accept(this).Single();
        return [$"( {innerCode} )"];
    }

    public override List<string> VisitNumberBrackets(Grammar.MppgParser.NumberBracketsContext context)
    {
        var innerCode = context.GetChild(1).Accept(this).Single();
        return [$"( {innerCode} )"];
    }

    #region Function binary operators

    public override List<string> VisitFunctionMinimum(Grammar.MppgParser.FunctionMinimumContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        var firstType = context.GetChild(0).Accept(TypeVisitor);
        var secondType = context.GetChild(2).Accept(TypeVisitor);
        
        // todo: add and use a PureConstant of sorts to Nancy
        if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
            return [$"Curve.Minimum({first}, {second})"];
        else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
            return [$"Curve.Minimum({first}, new Curve(new Sequence([ new Point(0, {second}), Segment.Constant(0, 1, {second})]), 0, 1, 0))"];
        else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
            return [$"Curve.Minimum({second}, new Curve(new Sequence([ new Point(0, {first}), Segment.Constant(0, 1, {first})]), 0, 1, 0))"];        else
            return [$"Rational.Min({first}, {second})"];
    }

    public override List<string> VisitFunctionMaximum(Grammar.MppgParser.FunctionMaximumContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        var firstType = context.GetChild(0).Accept(TypeVisitor);
        var secondType = context.GetChild(2).Accept(TypeVisitor);
        
        // todo: add and use a PureConstant of sorts to Nancy
        if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
            return [$"Curve.Maximum({first}, {second})"];
        else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
            return [$"Curve.Maximum({first}, new Curve(new Sequence([ new Point(0, {second}), Segment.Constant(0, 1, {second})]), 0, 1, 0))"];
        else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
            return [$"Curve.Maximum({second}, new Curve(new Sequence([ new Point(0, {first}), Segment.Constant(0, 1, {first})]), 0, 1, 0))"];        else
            return [$"Rational.Max({first}, {second})"];
    }

    public override List<string> VisitFunctionMinPlusConvolution(Grammar.MppgParser.FunctionMinPlusConvolutionContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        var firstType = context.GetChild(0).Accept(TypeVisitor);
        var secondType = context.GetChild(2).Accept(TypeVisitor);
        
        if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
            return [$"Curve.Convolution({first}, {second})"];
        else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
            return [$"{first} * {second}"];
        else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
            return [$"{second} * {first}"];
        else
            return [$"{first} * {second}"];
    }

    public override List<string> VisitFunctionMaxPlusConvolution(Grammar.MppgParser.FunctionMaxPlusConvolutionContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        return [$"Curve.MaxPlusConvolution({first}, {second})"];
    }

    public override List<string> VisitFunctionMinPlusDeconvolution(Grammar.MppgParser.FunctionMinPlusDeconvolutionContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        var firstType = context.GetChild(0).Accept(TypeVisitor);
        var secondType = context.GetChild(2).Accept(TypeVisitor);
        
        if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
            return [$"Curve.Deconvolution({first}, {second})"];
        else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
            return [$"{first} / {second}"];
        else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
            throw new InvalidOperationException($"Unexpected expression type: {context.GetJoinedText()}");
        else
            return [$"{first} / {second}"];
    }

    public override List<string> VisitFunctionMaxPlusDeconvolution(Grammar.MppgParser.FunctionMaxPlusDeconvolutionContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        return [$"Curve.MaxPlusDeconvolution({first}, {second})"];
    }

    public override List<string> VisitFunctionComposition(Grammar.MppgParser.FunctionCompositionContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        return [$"Curve.Composition({first}, {second})"];
    }

    public override List<string> VisitFunctionScalarMultiplicationLeft(Grammar.MppgParser.FunctionScalarMultiplicationLeftContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        var firstType = context.GetChild(0).Accept(TypeVisitor);
        var secondType = context.GetChild(2).Accept(TypeVisitor);
        
        if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
            return [$"Curve.Convolution({first}, {second})"];
        else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
            return [$"{first} * {second}"];
        else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
            return [$"{second} * {first}"];
        else
            return [$"{first} * {second}"];
    }
    
    public override List<string> VisitFunctionScalarMultiplicationRight(Grammar.MppgParser.FunctionScalarMultiplicationRightContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        var firstType = context.GetChild(0).Accept(TypeVisitor);
        var secondType = context.GetChild(2).Accept(TypeVisitor);
        
        if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
            return [$"Curve.Convolution({first}, {second})"];
        else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
            return [$"{first} * {second}"];
        else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
            return [$"{second} * {first}"];
        else
            return [$"{first} * {second}"];
    }

    public override List<string> VisitFunctionScalarDivision(Grammar.MppgParser.FunctionScalarDivisionContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        var firstType = context.GetChild(0).Accept(TypeVisitor);
        var secondType = context.GetChild(2).Accept(TypeVisitor);
        
        if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
            return [$"Curve.Deconvolution({first}, {second})"];
        else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
            return [$"{first} / {second}"];
        else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
            throw new InvalidOperationException($"Unexpected expression type: {context.GetJoinedText()}");
        else
            return [$"{first} / {second}"];
    }

    public override List<string> VisitFunctionSum(Grammar.MppgParser.FunctionSumContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        return [$"{first} + {second}"];
    }

    public override List<string> VisitFunctionSubtraction(Grammar.MppgParser.FunctionSubtractionContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        return [$"{first} - {second}"];
    }

    #endregion

    #region Function unary operators

    public override List<string> VisitFunctionSubadditiveClosure(Grammar.MppgParser.FunctionSubadditiveClosureContext context)
    {
        var curve = context.GetChild(2).Accept(this).Single();

        return [$"Curve.SubAdditiveClosure({curve})"];
    }

    public override List<string> VisitFunctionHShift(Grammar.MppgParser.FunctionHShiftContext context)
    {
        var curve = context.GetChild(2).Accept(this).Single();
        var shift = context.GetChild(4).Accept(this).Single();

        return [$"({curve}).HorizontalShift({shift})"];
    }

    public override List<string> VisitFunctionVShift(Grammar.MppgParser.FunctionVShiftContext context)
    {
        var curve = context.GetChild(2).Accept(this).Single();
        var shift = context.GetChild(4).Accept(this).Single();

        return [$"({curve}).VerticalShift({shift})"];
    }

    public override List<string> VisitFunctionLowerPseudoInverse(Grammar.MppgParser.FunctionLowerPseudoInverseContext context)
    {
        var curve = context.GetChild(2).Accept(this).Single();

        return [$"({curve}).LowerPseudoInverse()"];
    }

    public override List<string> VisitFunctionUpperPseudoInverse(Grammar.MppgParser.FunctionUpperPseudoInverseContext context)
    {
        var curve = context.GetChild(2).Accept(this).Single();

        return [$"({curve}).UpperPseudoInverse()"];
    }

    public override List<string> VisitFunctionUpNonDecreasingClosure(Grammar.MppgParser.FunctionUpNonDecreasingClosureContext context)
    {
        var curve = context.GetChild(2).Accept(this).Single();
        
        return [$"({curve}).ToUpperNonDecreasing()"];
    }

    public override List<string> VisitFunctionNonNegativeUpNonDecreasingClosure(Grammar.MppgParser.FunctionNonNegativeUpNonDecreasingClosureContext context)
    {
        var curve = context.GetChild(2).Accept(this).Single();
        
        return [$"({curve}).ToNonNegative().ToUpperNonDecreasing()"];
    }

    public override List<string> VisitFunctionLeftExt(Grammar.MppgParser.FunctionLeftExtContext context)
    {
        var curve = context.GetChild(2).Accept(this).Single();
        
        return [$"({curve}).ToLeftContinuous()"];
    }

    public override List<string> VisitFunctionRightExt(Grammar.MppgParser.FunctionRightExtContext context)
    {
        var curve = context.GetChild(2).Accept(this).Single();
        
        return [$"({curve}).ToRightContinuous()"];
    }

    #endregion Function unary operators
    
    #region Function constructors

    public override List<string> VisitRateLatency(Grammar.MppgParser.RateLatencyContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var rate = context.GetChild(2).Accept(this).Single();
        var latency = context.GetChild(4).Accept(this).Single();

        return [$"new RateLatencyServiceCurve({rate}, {latency})"];
    }

    public override List<string> VisitTokenBucket(Grammar.MppgParser.TokenBucketContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var a = context.GetChild(2).Accept(this).Single();
        var b = context.GetChild(4).Accept(this).Single();

        return [$"new SigmaRhoArrivalCurve({b}, {a})"];
    }

    public override List<string> VisitAffineFunction(
        Grammar.MppgParser.AffineFunctionContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var slope = context.GetChild(2).Accept(this).Single();
        var constant = context.GetChild(4).Accept(this).Single();

        return [$"new Curve(new Sequence([new Point(0, {constant}), new Segment(0, 1, {constant}, {slope}) ]), 0, 1, {slope})"];
    }
    
    public override List<string> VisitStepFunction(
        Grammar.MppgParser.StepFunctionContext context)
    {
        if (context.ChildCount != 6)
            throw new Exception("Expected 6 child expression");

        var o = context.GetChild(2).Accept(this).Single();
        var h = context.GetChild(4).Accept(this).Single();

        return [$"new StepCurve({h}, {o})"];
    }

    public override List<string> VisitStairFunction(Grammar.MppgParser.StairFunctionContext context)
    {
        if (context.ChildCount != 8)
            throw new Exception("Expected 8 child expression");

        var o = context.GetChild(2).Accept(this).Single();
        var l = context.GetChild(4).Accept(this).Single();
        var h = context.GetChild(6).Accept(this).Single();

        return [$"new Curve(new Sequence([Point.Origin(), new Segment(0, {l}, {h}, 0)]),0, {l}, {h}).DelayBy({o})"];
    }
    
    public override List<string> VisitDelayFunction(
        Grammar.MppgParser.DelayFunctionContext context)
    {
        if (context.ChildCount != 4)
            throw new Exception("Expected 4 child expression");

        var d = context.GetChild(2).Accept(this).Single();

        return [$"new DelayServiceCurve({d})"];
    }

    public override List<string> VisitZeroFunction(
        Grammar.MppgParser.ZeroFunctionContext context)
    {
        return ["Curve.Zero()"];
    }

    public override List<string> VisitEpsilonFunction(
        Grammar.MppgParser.EpsilonFunctionContext context)
    {
        return ["Curve.PlusInfinite()"];
    }

    #endregion Function constructors

    #region Number-returning function operators

    public override List<string> VisitFunctionValueAt(Grammar.MppgParser.FunctionValueAtContext context)
    {
        var curve = context.GetChild(0).Accept(this).Single();
        var time = context.GetChild(2).Accept(this).Single();

        return [$"{curve}.ValueAt({time})"];
    }

    public override List<string> VisitFunctionLeftLimitAt(Grammar.MppgParser.FunctionLeftLimitAtContext context)
    {
        var curve = context.GetChild(0).Accept(this).Single();
        var time = context.GetChild(2).Accept(this).Single();

        return [$"{curve}.LeftLimitAt({time})"];
    }

    public override List<string> VisitFunctionRightLimitAt(Grammar.MppgParser.FunctionRightLimitAtContext context)
    {
        var curve = context.GetChild(0).Accept(this).Single();
        var time = context.GetChild(2).Accept(this).Single();

        return [$"{curve}.RightLimitAt({time})"];
    }

    public override List<string> VisitFunctionHorizontalDeviation(Grammar.MppgParser.FunctionHorizontalDeviationContext context)
    {
        var l = context.GetChild(2).Accept(this).Single();
        var r = context.GetChild(4).Accept(this).Single();

        return [$"Curve.HorizontalDeviation({l}, {r})"];
    }

    public override List<string> VisitFunctionVerticalDeviation(Grammar.MppgParser.FunctionVerticalDeviationContext context)
    {
        var l = context.GetChild(2).Accept(this).Single();
        var r = context.GetChild(4).Accept(this).Single();

        return [$"Curve.VerticalDeviation({l}, {r})"];
    }

    #endregion
    
    #region Number binary operators

    public override List<string> VisitNumberMultiplication(Grammar.MppgParser.NumberMultiplicationContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        var firstType = context.GetChild(0).Accept(TypeVisitor);
        var secondType = context.GetChild(2).Accept(TypeVisitor);
        
        if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
            return [$"Curve.Convolution({first}, {second})"];
        else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
            return [$"{first} * {second}"];
        else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
            return [$"{second} * {first}"];
        else
            return [$"{first} * {second}"];
    }

    public override List<string> VisitNumberDivision(Grammar.MppgParser.NumberDivisionContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        var firstType = context.GetChild(0).Accept(TypeVisitor);
        var secondType = context.GetChild(2).Accept(TypeVisitor);
        
        if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
            return [$"Curve.Deconvolution({first}, {second})"];
        else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
            return [$"{first} / {second}"];
        else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
            throw new InvalidOperationException($"Unexpected expression type: {context.GetJoinedText()}");
        else
            return [$"{first} / {second}"];
    }

    public override List<string> VisitNumberSum(Grammar.MppgParser.NumberSumContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        return [$"{first} + {second}"];
    }

    public override List<string> VisitNumberSub(Grammar.MppgParser.NumberSubContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        return [$"{first} - {second}"];
    }

    public override List<string> VisitNumberMinimum(Grammar.MppgParser.NumberMinimumContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        var firstType = context.GetChild(0).Accept(TypeVisitor);
        var secondType = context.GetChild(2).Accept(TypeVisitor);
        
        // todo: add and use a PureConstant of sorts to Nancy
        if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
            return [$"Curve.Minimum({first}, {second})"];
        else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
            return [$"Curve.Minimum({first}, new Curve(new Sequence([ new Point(0, {second}), Segment.Constant(0, 1, {second})]), 0, 1, 0))"];
        else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
            return [$"Curve.Minimum({second}, new Curve(new Sequence([ new Point(0, {first}), Segment.Constant(0, 1, {first})]), 0, 1, 0))"];        else
            return [$"Rational.Min({first}, {second})"];
    }

    public override List<string> VisitNumberMaximum(Grammar.MppgParser.NumberMaximumContext context)
    {
        var first = context.GetChild(0).Accept(this).Single();
        var second = context.GetChild(2).Accept(this).Single();

        var firstType = context.GetChild(0).Accept(TypeVisitor);
        var secondType = context.GetChild(2).Accept(TypeVisitor);
        
        // todo: add and use a PureConstant of sorts to Nancy
        if (firstType == ExpressionType.Function && secondType == ExpressionType.Function)
            return [$"Curve.Maximum({first}, {second})"];
        else if (firstType == ExpressionType.Function && secondType == ExpressionType.Number)
            return [$"Curve.Maximum({first}, new Curve(new Sequence([ new Point(0, {second}), Segment.Constant(0, 1, {second})]), 0, 1, 0))"];
        else if (firstType == ExpressionType.Number && secondType == ExpressionType.Function)
            return [$"Curve.Maximum({second}, new Curve(new Sequence([ new Point(0, {first}), Segment.Constant(0, 1, {first})]), 0, 1, 0))"];        else
            return [$"Rational.Max({first}, {second})"];
    }

    #endregion

    #region Utility

    private static List<string> CleanupReassignments(List<string> code)
    {
        var variableNames = code
            .Where(l => l.StartsWith("var "))
            .Select(l =>
            {
                var match = Regex.Match(l, @"^var (.+?) =");
                return match.Groups[1].Value;
            })
            .Distinct();

        var newCode = new List<string>(code);
        foreach (var name in variableNames)
        {
            var assignments = code
                .WithIndex()
                .Where(l => l.Item1.StartsWith($"var {name} ="))
                .ToList();
            if (assignments.Count > 1)
            {
                foreach (var (line, index) in assignments.Skip(1))
                {
                    newCode[index] = line.Replace($"var {name} = ", $"{name} = ");
                }
            }
        }

        return newCode;
    }

    #endregion
}