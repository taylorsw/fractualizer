//#define VALIDATE_GEN
//#define DEBUG_GEN

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using FPL;

namespace CodeGen
{
    abstract class FPLTranspiler : FPLBaseVisitor<FPLTranspiler.Losa>
    {
        public Idtr idtrCur { get; private set; }

        public void GenFile(FPLParser.FractalContext fractalContext, string stDirectory)
        {
            idtrCur = Idtr.Initial(this);

            string stFileOutput = Path.Combine(stDirectory, VisitIdentifier(fractalContext.identifier()).ToString() + StExtension());
            Directory.CreateDirectory(stDirectory);
            using (FileStream fs = File.Create(stFileOutput))
            {
                using (TextWriter tw = new StreamWriter(fs))
                {
                    Losa losa = VisitFractal(fractalContext);
                    tw.Write(losa.ToStringFollowing());
                }
            }
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

    class FPLToHLSL : FPLTranspiler
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
            foreach (FPLParser.FuncContext func in fractal.func())
            {
                Losa losaFunc = VisitFunc(func);
                losaProg = losaProg + losaFunc;
            }
            losaProg = losaProg + LosaInclude("rayTracer.hlsl");
            return losaProg;
        }

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
                losaArglist = losaArglist + VisitType(arg.type()) + " " + VisitIdentifier(arg.identifier());
                if (iarg != rgarg.Length - 1)
                    losaArglist = losaArglist + ", ";
            }
            return losaArglist;
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

        public override Losa VisitBlockStat(FPLParser.BlockStatContext blockStat)
        {
            Losa losaBlockStat = base.VisitBlockStat(blockStat);
            if (blockStat.stat() != null && blockStat.stat().expr() != null)
                losaBlockStat += ";";
            return losaBlockStat;
        }

        public override Losa VisitLocalDecl(FPLParser.LocalDeclContext localDecl)
        {
            return VisitType(localDecl.type()) + " " + VisitIdentifier(localDecl.identifier()) + " = " + VisitExpr(localDecl.expr()) + ";";
        }

        public override Losa VisitIfStat(FPLParser.IfStatContext ifStat)
        {
            Losa losaIfStat = LneNew() + "if " + VisitParExpr(ifStat.parExpr());
            var rgstat = ifStat.stat();
            for (int istat = 0; istat < rgstat.Length; istat++)
            {
                Losa losaStat = VisitStat(rgstat[istat]);
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
                Losa losaExpr = VisitExpr(rgexpr[iexpr]);
                losaExprList += iexpr == 0
                    ? losaExpr
                    : ", " + losaExpr;
            }
            return losaExprList;
        }

        public override Losa VisitParExpr(FPLParser.ParExprContext parExpr)
        {
            return "(" + VisitExpr(parExpr.expr()) + ")";
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

        public override Losa VisitBinaryOperator(FPLParser.BinaryOperatorContext binaryOperator)
        {
            return binaryOperator.GetText();
        }

        public override Losa VisitAssignmentOperator(FPLParser.AssignmentOperatorContext assignmentOperator)
        {
            return assignmentOperator.GetText();
        }

        public override Losa VisitType(FPLParser.TypeContext type)
        {
            string stType = type.GetText();
            switch (stType)
            {
                case "v3":
                    return "float3";
                default:
                    return stType;
            }
        }

        public override Losa VisitLiteral(FPLParser.LiteralContext literal)
        {
            return literal.GetText();
        }

        public override Losa VisitIdentifier(FPLParser.IdentifierContext identifier)
        {
            return identifier.GetText();
        }

        protected override Losa AggregateResult(Losa aggregate, Losa nextResult)
        {
            // Debug.Assert(aggregate == null || nextResult == null);
            if (aggregate != null)
                return nextResult == null ? aggregate : nextResult + aggregate;
            return nextResult;
        }

        protected override string StExtension() => ".hlsl";
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
                FPLLexer fplLexer = new FPLLexer(afs);
                CommonTokenStream cts = new CommonTokenStream(fplLexer);
                FPLParser fplParser = new FPLParser(cts);

                FPLParser.FractalContext fractalContext = fplParser.fractal();

                FPLToHLSL fplToHlsl = new FPLToHLSL();
                fplToHlsl.GenFile(fractalContext, stDirectoryOut);
            }

            return 0;
        }
    }
}
