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

            string stFileOutput = Path.Combine(stDirectory, fractalContext.Identifier() + StExtension());
            Directory.CreateDirectory(stDirectory);
            using (FileStream fs = File.Create(stFileOutput))
            {
                using (TextWriter tw = new StreamWriter(fs))
                {
                    Losa losa = VisitFractal(fractalContext);

                    Lne lne = (Lne) losa;
                    StringBuilder sb = new StringBuilder();
                    while (lne != null)
                    {
                        sb.Append(lne);
                        sb.Append(Environment.NewLine);
                        lne = lne.lneNext;
                    }

                    tw.Write(sb.ToString());
                }
            }
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
        }

        public class Lne : Losa
        {
            public readonly Statm statm;
            public Lne lneNext { get; private set; }

            public Lne(Idtr idtr, Statm statm, Lne lneNext) : base(idtr)
            {
                this.statm = statm;
                this.lneNext = lneNext;
            }

            public override string ToString()
            {
                // special case of empty line is easy
                return statm == null ? Environment.NewLine : idtr.StIndent() + statm.ToString();
            }

            public static Lne operator +(Lne lneLeft, Lne lneRight)
            {
                Lne lneCur = lneLeft.lneNext;
                Lne lnePrev = lneLeft;
                while (lneCur != null)
                {
                    lnePrev = lneCur;
                    lneCur = lneCur.lneNext;
                }
                lnePrev.lneNext = lneRight;
                return lneLeft;
            }

            public static Lne operator +(Lne lneLeft, Statm statmRight)
            {
                return new Lne(lneLeft.idtr, lneLeft.statm == null ? statmRight : lneLeft.statm + statmRight, lneLeft.lneNext);
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
        }

        #endregion
    }

    class FPLToHLSL : FPLTranspiler
    {
        private Losa LosaInclude(string stFile) => LneNew(@"#include """ + stFile + @"""");
        public override Losa VisitFractal(FPLParser.FractalContext fractal)
        {
            Debugger.Launch();
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
            Losa losaFunc = LneNew() + VisitRetType(func.retType()) + " " + func.Identifier().GetText() + "(" + losaArgs + ")"
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
                losaArglist = losaArglist + VisitType(arg.type()) + " " + arg.Identifier().GetText();
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

        public override Losa VisitLocalDecl(FPLParser.LocalDeclContext localDecl)
        {
            return VisitType(localDecl.type()) + " " + localDecl.Identifier().GetText() + " = " + VisitExpr(localDecl.expr()) + ";";
        }

        public override Losa VisitExpr(FPLParser.ExprContext expr)
        {
            return expr.GetText();
        }

        public override Losa VisitIfStat(FPLParser.IfStatContext ifStat)
        {
            Losa losaIfStat = LneNew() + "if (" + VisitParExpr(ifStat.parExpr()) + ")";
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
            Losa losaForStat = LneNew() + "for (" + VisitForInit(forStat.forInit()) + ";" + VisitExpr(forStat.expr()[0]) + ";" + VisitExpr(forStat.expr()[1]) + ")" + VisitBlock(forStat.block());
            return losaForStat;
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

        protected override Losa AggregateResult(Losa aggregate, Losa nextResult)
        {
            Debug.Assert(aggregate == null || nextResult == null);
            return aggregate ?? nextResult;
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
