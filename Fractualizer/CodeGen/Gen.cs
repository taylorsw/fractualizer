//#define VALIDATE_GEN
//#define DEBUG_GEN

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using FPL;

namespace CodeGen
{
    internal abstract class FPLTranspiler : FPLBaseVisitor<FPLTranspiler.Losa>
    {
        protected Idtr idtrCur { get; private set; }

        protected string StFractalName(FPLParser.FractalContext fractal)
            => VisitIdentifier(fractal.identifier()).ToString();

        internal void GenFile(FPLParser.FractalContext fractal, string stDirectory)
        {
            idtrCur = Idtr.Initial(this);

            string stFileOutput = Path.Combine(stDirectory, StFractalName(fractal) + StExtension());
            Directory.CreateDirectory(stDirectory);
            using (FileStream fs = File.Create(stFileOutput))
            {
                using (TextWriter tw = new StreamWriter(fs))
                {
                    string stGenFile = StTranspile(fractal);
                    tw.Write(stGenFile);
                }
            }
        }

        internal static FPLParser.FractalContext FractalFromAntlrInputStream(AntlrInputStream antlrInputStream)
        {
            FPLLexer fplLexer = new FPLLexer(antlrInputStream);
            CommonTokenStream cts = new CommonTokenStream(fplLexer);
            FPLParser fplParser = new FPLParser(cts);
            FPLParser.FractalContext fractal = fplParser.fractal();
            return fractal;
        }

        internal string StTranspile(FPLParser.FractalContext fractal)
        {
            Losa losa = VisitFractal(fractal);
            return losa.ToStringFollowing();
        }

        protected static void Validate(ParserRuleContext context)
        {
            if (context == null)
                return;
            if (context.exception != null)
                throw context.exception;
            for (int i = 0; i < context.ChildCount; i++)
                Validate(context.GetChild(i) as ParserRuleContext);
        }

        protected abstract string StExtension();

        #region Formatting Helpers

        protected Lne LneNew(Statm statm = null)
        {
            return new Lne(idtrCur, statm, null);
        }

        public class Idtr : IDisposable
        {
            private readonly FPLTranspiler fplTranspiler;
            private readonly int cindent;
            private readonly Idtr idtrOuter;

            private Idtr(FPLTranspiler fplTranspiler, int cindent)
            {
                this.fplTranspiler = fplTranspiler;
                this.cindent = cindent;
                this.idtrOuter = fplTranspiler.idtrCur;
                fplTranspiler.idtrCur = this;
            }

            public static Idtr Initial(FPLTranspiler fplTranspiler)
            {
                return new Idtr(fplTranspiler, 0);
            }

            public Idtr New()
            {
                return new Idtr(fplTranspiler, cindent + 1);
            }

            public string StIndent()
            {
                StringBuilder sb = new StringBuilder();
                for (int iindent = 0; iindent < cindent; iindent++)
                    sb.Append('\t');
                return sb.ToString();
            }

            public void Dispose()
            {
                fplTranspiler.idtrCur = idtrOuter;
            }
        }

        // Line Or String Atom
        public abstract class Losa
        {
            public readonly Idtr idtr;

            protected Losa(Idtr idtr)
            {
                Debug.Assert(idtr != null || this is Statm);
                this.idtr = idtr;
            }

            public static Losa operator +(Losa losaLeft, Losa losaRight)
            {
                return losaLeft.LosaAdd(losaRight);
            }

            public static Losa operator +(Losa losaLeft, string stRight)
            {
                return losaLeft.LosaAdd(new Statm(losaLeft.idtr, stRight));
            }

            public static Losa operator +(string stLeft, Losa losaRight)
            {
                return new Statm(losaRight.idtr, stLeft).LosaAdd(losaRight);
            }

            public static implicit operator Losa(string st)
            {
                return new Statm(null, st);
            }

            protected abstract Losa LosaAdd(Losa losaRight);

            public abstract string ToStringFollowing();
        }

        public class Lne : Losa
        {
            private readonly Statm statm;
            private Lne lneNext;
            private Lne lneLast;

            public Lne(Idtr idtr, Statm statm, Lne lneNext) : base(idtr)
            {
                this.statm = statm;
                this.lneNext = lneNext;
                this.lneLast = this;
            }

            public override string ToString()
            {
                // special case of empty line is easy
                return statm == null ? Environment.NewLine : idtr.StIndent() + statm.ToString();
            }

            public static Lne operator +(Lne lneLeft, Lne lneRight)
            {
                Lne lneLastCur = lneLeft.lneLast;
                Lne lneLastNew = lneRight.lneLast;
                Debug.Assert(lneLastNew.lneNext == null && lneLastCur.lneNext == null);

                lneLastCur.lneNext = lneRight;

                lneLeft.lneLast = lneLastNew;
                lneRight.lneLast = null;

                return lneLeft;
            }

            public static Lne operator +(Lne lneLeft, Statm statmRight)
            {
                Lne lneLast = lneLeft.lneLast;
                return new Lne(lneLast.idtr, lneLast.statm == null ? statmRight : lneLast.statm + statmRight, lneLast.lneNext);
            }

            public static Lne operator +(Statm statmLeft, Lne lneRight)
            {
                return new Lne(lneRight.idtr, lneRight.statm == null ? statmLeft : statmLeft + lneRight.statm, lneRight.lneNext);
            }

            protected override Losa LosaAdd(Losa losaRight)
            {
                Lne lneRight = losaRight as Lne;
                if (lneRight != null)
                    return this + lneRight;
                return this + (Statm)losaRight;
            }

            public override string ToStringFollowing()
            {
                string stThis = ToString() + Environment.NewLine;
                if (lneNext != null)
                    stThis += lneNext.ToStringFollowing();
                return stThis;
            }
        }

        public class Statm : Losa
        {
            private readonly string st;

            public Statm(Idtr idtr, string st) : base(idtr)
            {
                Debug.Assert(!st.Contains(Environment.NewLine), "Create a Lne instead.");
                this.st = st;
            }

            public static Statm operator +(Statm saLeft, Statm saRight)
            {
                return new Statm(null, saLeft.st + saRight.st);
            }

            public static implicit operator Statm(string st)
            {
                return new Statm(null, st);
            }

            public override string ToString()
            {
                return st;
            }

            protected override Losa LosaAdd(Losa losaRight)
            {
                Statm statmRight = losaRight as Statm;
                if (statmRight != null)
                    return this + statmRight;
                return this + (Lne)losaRight;
            }

            public override string ToStringFollowing()
            {
                return ToString();
            }
        }

        #endregion
    }

    internal abstract class FPLToCLL : FPLTranspiler
    {
        public override Losa VisitFunc(FPLParser.FuncContext func)
        {
            Losa losaArgs = VisitArglist(func.arglist());
            Losa losaBlock = VisitBlock(func.block());
            Losa losaFunc = LneNew() + VisitRetType(func.retType()) + " " + VisitIdentifier(func.identifier()) + "(" + losaArgs + ")"
                + losaBlock;
            return losaFunc;
        }

        public override Losa VisitArglist(FPLParser.ArglistContext arglist)
        {
            Losa losaArglist = "";
            FPLParser.ArgContext[] rgarg = arglist.arg();
            for (int iarg = 0; iarg < rgarg.Length; iarg++)
            {
                FPLParser.ArgContext arg = rgarg[iarg];
                losaArglist += VisitArg(arg);
                if (iarg != rgarg.Length - 1)
                    losaArglist = losaArglist + ", ";
            }
            return losaArglist;
        }

        public override Losa VisitArg(FPLParser.ArgContext arg)
        {
            Losa losaArg = VisitType(arg.type()) + " " + VisitIdentifier(arg.identifier());
            if (arg.argMod() != null)
                losaArg = VisitArgMod(arg.argMod()) + " " + losaArg;
            return losaArg;
        }

        public override Losa VisitRetType(FPLParser.RetTypeContext retType)
        {
            FPLParser.TypeContext type = retType.type();
            if (type != null)
                return VisitType(type);
            return "void";
        }

        public override Losa VisitArgMod(FPLParser.ArgModContext context)
        {
            throw new NotImplementedException();
        }

        public override Losa VisitBlock(FPLParser.BlockContext block)
        {
            Losa losaBlock = LneNew("{");
            using (idtrCur.New())
            {
                foreach (FPLParser.BlockStatContext blockStat in block.blockStat())
                {
                    Losa losaBlockStat = VisitBlockStat(blockStat);
                    losaBlock = losaBlock + (losaBlockStat is Lne
                        ? losaBlockStat
                        : LneNew() + losaBlockStat);
                }
            }
            losaBlock = losaBlock + LneNew("}");
            return losaBlock;
        }

        public override Losa VisitLocalDecl(FPLParser.LocalDeclContext localDecl)
        {
            return VisitType(localDecl.type()) + " " + VisitIdentifier(localDecl.identifier()) + " = " + VisitExpr(localDecl.expr()) + ";";
        }

        public override Losa VisitStat(FPLParser.StatContext stat)
        {
            Losa losaStat = base.VisitStat(stat);
            if (stat.expr() != null || stat.keywordExpr() != null)
                losaStat += ";";
            return losaStat;
        }

        public override Losa VisitIfStat(FPLParser.IfStatContext ifStat)
        {
            Losa losaIfStat = LneNew() + "if " + VisitParExpr(ifStat.parExpr());
            var rgstat = ifStat.stat();
            for (int istat = 0; istat < rgstat.Length; istat++)
            {
                FPLParser.StatContext stat = rgstat[istat];
                Losa losaStat = VisitStat(stat);
                if (stat.block() == null)
                    using (idtrCur.New())
                        losaStat = LneNew() + losaStat;

                losaIfStat = istat == 0
                    ? losaIfStat + losaStat
                    : losaIfStat + LneNew("else") + losaStat;
            }
            return losaIfStat;
        }

        public override Losa VisitForStat(FPLParser.ForStatContext forStat)
        {
            Losa losaForStat = LneNew() + "for (" + VisitForInit(forStat.forInit()) + " " + VisitExpr(forStat.expr()) + "; " + VisitForUpdate(forStat.forUpdate()) + ")" + VisitStat(forStat.stat());
            return losaForStat;
        }

        public override Losa VisitForInit(FPLParser.ForInitContext forInit)
        {
            Losa losaChildren = base.VisitForInit(forInit);
            if (forInit.localDecl() == null)
                losaChildren += ";";
            return losaChildren;
        }

        public override Losa VisitExprList(FPLParser.ExprListContext exprList)
        {
            Losa losaExprList = "";
            FPLParser.ExprContext[] rgexpr = exprList.expr();
            for (int iexpr = 0; iexpr < rgexpr.Length; iexpr++)
            {
                Losa losaExpr;
                FPLParser.FuncCallContext funcCall = exprList.parent as FPLParser.FuncCallContext;
                if (funcCall != null)
                {
                    losaExpr = VisitFuncCallExpr(funcCall, rgexpr[iexpr], iexpr);
                }
                else
                {
                    losaExpr = VisitExpr(rgexpr[iexpr]);
                }
                losaExprList += iexpr == 0
                    ? losaExpr
                    : ", " + losaExpr;
            }
            return losaExprList;
        }

        public virtual Losa VisitFuncCallExpr(FPLParser.FuncCallContext funcCall, FPLParser.ExprContext expr, int iexpr)
        {
            return VisitExpr(expr);
        }

        public override Losa VisitParExpr(FPLParser.ParExprContext parExpr)
        {
            return "(" + VisitExpr(parExpr.expr()) + ")";
        }

        public override Losa VisitKeywordExpr(FPLParser.KeywordExprContext context)
        {
            if (context.expr() == null)
                return VisitKeywordSingle(context.keywordSingle());
            return VisitKeywordPrefix(context.keywordPrefix()) + " " + VisitExpr(context.expr());
        }

        public override Losa VisitExpr(FPLParser.ExprContext expr)
        {
            Losa losaExpr = "";
            foreach (IParseTree child in expr.children)
            {
                if (child is ITerminalNode)
                    losaExpr += child.GetText();
                else
                    losaExpr += Visit(child);
            }
            return losaExpr;
        }

        public override Losa VisitBinaryOp(FPLParser.BinaryOpContext binaryOp)
        {
            return binaryOp.GetText();
        }

        public override Losa VisitAssignmentOp(FPLParser.AssignmentOpContext assignmentOp)
        {
            return assignmentOp.GetText();
        }

        public override Losa VisitUnaryOp(FPLParser.UnaryOpContext unaryOp)
        {
            return unaryOp.GetText();
        }

        public override Losa VisitPrefixUnaryOp(FPLParser.PrefixUnaryOpContext prefixUnaryOp)
        {
            return prefixUnaryOp.GetText();
        }

        public override Losa VisitKeywordPrefix(FPLParser.KeywordPrefixContext keywordPrefix)
        {
            return keywordPrefix.GetText();
        }

        public override Losa VisitKeywordSingle(FPLParser.KeywordSingleContext keywordSingle)
        {
            return keywordSingle.GetText();
        }

        public override Losa VisitLiteral(FPLParser.LiteralContext literal)
        {
            return VisitChildren(literal);
        }

        public override Losa VisitLiteralFloat(FPLParser.LiteralFloatContext context)
        {
            return context.GetText();
        }

        public override Losa VisitLiteralInt(FPLParser.LiteralIntContext context)
        {
            return context.GetText();
        }

        public override Losa VisitIdentifier(FPLParser.IdentifierContext identifier)
        {
            return identifier.GetText();
        }

        protected override Losa AggregateResult(Losa aggregate, Losa nextResult)
        {
            // Debug.Assert(aggregate == null || nextResult == null);
            if (aggregate != null)
                return nextResult == null ? aggregate : aggregate + nextResult;
            return nextResult;
        }
    }

    internal class FPLToHLSL : FPLToCLL
    {
        private Losa LosaInclude(string stFile) => LneNew(@"#include """ + stFile + @"""");
        public override Losa VisitFractal(FPLParser.FractalContext fractal)
        {
#if DEBUG_GEN
            Debugger.Launch();
#endif
#if VALIDATE_GEN
            Validate(fractal);
#endif
            Losa losaProg = LosaInclude("parameters.hlsl");
            foreach (var global in fractal.global())
            {
                losaProg += VisitGlobal(global);
                if (global.globalVal() == null)
                    losaProg += LneNew();
            }

            losaProg += VisitDistanceEstimator(fractal.distanceEstimator());

            losaProg += LosaInclude("rayTracer.hlsl");
            return losaProg;
        }
        
        public override Losa VisitDistanceEstimator(FPLParser.DistanceEstimatorContext distanceEstimator)
        {
            Losa losaDE = LneNew("float DuDeFractal(float3 pos)") + VisitBlock(distanceEstimator.block());
            return losaDE;
        }

        public override Losa VisitFuncCall(FPLParser.FuncCallContext funcCall)
        {
            return VisitIdentifier(funcCall.identifier()) + "(" + VisitExprList(funcCall.exprList()) + ")";
        }

        public override Losa VisitArgMod(FPLParser.ArgModContext argMod)
        {
            string stMod = argMod.GetText();
            switch (stMod)
            {
                case "ref":
                    return "inout";
                default:
                    return stMod;
            }
        }

        public override Losa VisitGlobalVal(FPLParser.GlobalValContext global)
        {
            return LneNew("static const ") + VisitLocalDecl(global.localDecl());
        }

        public override Losa VisitInstantiation(FPLParser.InstantiationContext instantiation)
        {
            return VisitType(instantiation.type()) + "(" + VisitExprList(instantiation.exprList()) + ")";
        }

        public override Losa VisitType(FPLParser.TypeContext type)
        {
            string stType = type.GetText();
            switch (stType)
            {
                case "v3":
                    return "float3";
                case "v4":
                    return "float4";
                default:
                    return stType;
            }
        }

        protected override string StExtension() => ".hlsl";
    }

    internal class FPLToCs : FPLToCLL
    {
        private class FPLTableBuilder : FPLBaseVisitor<int>
        {
            public readonly Dictionary<string, List<FPLParser.ArgModContext>> mpstFunc_rgargmod;

            public FPLTableBuilder()
            {
                 mpstFunc_rgargmod = new Dictionary<string, List<FPLParser.ArgModContext>>();
            }

            public override int VisitFractal(FPLParser.FractalContext fractal)
            {
                mpstFunc_rgargmod.Clear();
                return base.VisitFractal(fractal);
            }

            public override int VisitFunc(FPLParser.FuncContext func)
            {
                foreach (FPLParser.ArgContext arg in func.arglist().arg())
                {
                    List<FPLParser.ArgModContext> rgstMod;
                    string stFunc = func.identifier().GetText();
                    if (!mpstFunc_rgargmod.TryGetValue(stFunc, out rgstMod))
                        mpstFunc_rgargmod[stFunc] = rgstMod = new List<FPLParser.ArgModContext>();
                    rgstMod.Add(arg.argMod());
                }
                return DefaultResult;
            }
        }

        private FPLTableBuilder fplTableBuilder;

        private static readonly Dictionary<string, string> mpstBuiltinFpl_stCs = new Dictionary<string, string>
        {
            {"length", "Vector3d.Length"},
            {"dot", "Vector3d.Dot" },
            {"clamp", "Util.Clamp"},
            {"atan", "Util.Atan"},
            {"abs", "Util.Abs"},
            {"acos", "Math.Acos"},
            {"asin", "Math.Asin"},
            {"sin", "Math.Sin"},
            {"cos", "Math.Cos"},
            {"tan", "Math.Tan"},
            {"log", "Math.Log"},
            {"pow", "Math.Pow"},
            {"min", "Math.Min"},
            {"max", "Math.Max"},
            {"sqrt", "Math.Sqrt"},
            {"sign", "Math.Sign"},
        };

        public override Losa VisitFractal(FPLParser.FractalContext fractal)
        {
            fplTableBuilder = new FPLTableBuilder();
            fplTableBuilder.VisitFractal(fractal);

            Losa losaProg = LneNew("using System;") + LneNew("using Fractals;") + LneNew("using SharpDX;");
            losaProg += LneNew("namespace Fractals") + LneNew("{");
            using (idtrCur.New())
            {
                losaProg += LneNew("public class ") + StFractalName(fractal) + " : Fractal3d" + LneNew("{");
                using (idtrCur.New())
                {
                    foreach (var global in fractal.global())
                    {
                        losaProg += VisitGlobal(global);
                        if (global.globalVal() == null)
                            losaProg += LneNew();
                    }

                    //losaProg += LneNew("public override double DuEstimate(Vector3d pt) => (float)DuEstimateF(pt);");
                    losaProg += VisitDistanceEstimator(fractal.distanceEstimator());
                }
                losaProg += LneNew("}");
            }
            losaProg += LneNew("}");
            return losaProg;
        }

        public override Losa VisitDistanceEstimator(FPLParser.DistanceEstimatorContext distanceEstimator)
        {
            Losa losa = LneNew("public override double DuEstimate(Vector3d pos)") + VisitBlock(distanceEstimator.block());
            return losa;
        }

        public override Losa VisitFuncCall(FPLParser.FuncCallContext funcCall)
        {
            string stFuncNameFpl = VisitIdentifier(funcCall.identifier()).ToString();
            string stFuncNameCs;
            if (!mpstBuiltinFpl_stCs.TryGetValue(stFuncNameFpl, out stFuncNameCs))
                stFuncNameCs = stFuncNameFpl;

            return stFuncNameCs + "(" + VisitExprList(funcCall.exprList()) + ")";
        }

        public override Losa VisitFuncCallExpr(FPLParser.FuncCallContext funcCall, FPLParser.ExprContext expr, int iexpr)
        {
            Losa losaExpr = base.VisitFuncCallExpr(funcCall, expr, iexpr);
            string stFunc = funcCall.identifier().GetText();
            List<FPLParser.ArgModContext> rgargmod;
            if (fplTableBuilder.mpstFunc_rgargmod.TryGetValue(stFunc, out rgargmod)
                && rgargmod[iexpr] != null)
                losaExpr = VisitArgMod(rgargmod[iexpr]) + " " + losaExpr;
            return losaExpr;
        }

        public override Losa VisitArgMod(FPLParser.ArgModContext argMod)
        {
            return argMod.GetText();
        }

        public override Losa VisitGlobalVal(FPLParser.GlobalValContext global)
        {
            return LneNew("static ") + VisitLocalDecl(global.localDecl());
        }

        public override Losa VisitInstantiation(FPLParser.InstantiationContext instantiation)
        {
            return "new " + VisitType(instantiation.type()) + "(" + VisitExprList(instantiation.exprList()) + ")";
        }

        //public override Losa VisitLiteralFloat(FPLParser.LiteralFloatContext literalFloat) => literalFloat.GetText() + "f";

        public override Losa VisitType(FPLParser.TypeContext type)
        {
            string stType = type.GetText();
            switch (stType)
            {
                case "v3":
                    return "Vector3d";
                case "v4":
                    return "Vector4d";
                case "float":
                    return "double";
                default:
                    return stType;
            }
        }

        protected override string StExtension() => ".cs";
    }

    class Gen
    {
        static int Main(string[] args)
        {
            if (args.Length != 2)
                throw new ArgumentException("Did not supply correct parameters.");

            string stDirectoryIn = args[0];
            string stDirectoryOut = args[1];

            foreach (string stFile in Directory.GetFiles(stDirectoryIn))
            {
                if (Path.GetExtension(stFile) != ".frac")
                    continue;

                AntlrFileStream afs = new AntlrFileStream(stFile);
                FPLParser.FractalContext fractal = FPLTranspiler.FractalFromAntlrInputStream(afs);

                FPLToHLSL fplToHlsl = new FPLToHLSL();
                fplToHlsl.GenFile(fractal, stDirectoryOut);
                
                FPLToCs fplToCs = new FPLToCs();
                fplToCs.GenFile(fractal, stDirectoryOut);
            }

            return 0;
        }
    }
}
