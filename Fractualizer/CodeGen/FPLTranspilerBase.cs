using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Antlr4.Runtime;
using FPL;

namespace CodeGen
{

    internal abstract class FPLTranspilerBase : FPLBaseVisitor<FPLTranspilerBase.Losa>
    {
        protected Idtr idtrCur { get; private set; }

        protected string StFractalName(FPLParser.FractalContext fractal)
            => VisitIdentifier(fractal.identifier()).ToString();

        protected string StRaytracerName(FPLParser.RaytracerContext raytracer)
            => VisitIdentifier(raytracer.identifier()).ToString();

        internal void GenFile(FPLParser.ProgContext prog, string stDirectory)
        {
            idtrCur = Idtr.Initial(this);

            bool fRaytracer = prog.raytracer() != null;
            string stFileName = fRaytracer ? StRaytracerName(prog.raytracer()) : StFractalName(prog.fractal());

            string stFileOutput = Path.Combine(stDirectory, stFileName + StExtension());
            Directory.CreateDirectory(stDirectory);
            using (FileStream fs = File.Create(stFileOutput))
            {
                using (TextWriter tw = new StreamWriter(fs))
                {
                    string stGenFile = Visit(prog).ToStringFollowing();
                    tw.Write(stGenFile);
                }
            }
        }

        internal static FPLParser.ProgContext ProgFromAntlrInputStream(AntlrInputStream antlrInputStream)
        {
            FPLLexer fplLexer = new FPLLexer(antlrInputStream);
            CommonTokenStream cts = new CommonTokenStream(fplLexer);
            FPLParser fplParser = new FPLParser(cts);
            FPLParser.ProgContext prog = fplParser.prog();
            return prog;
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
            private readonly FPLTranspilerBase fplTranspiler;
            private readonly int cindent;
            private readonly Idtr idtrOuter;
#if DEBUG
            private bool fDisposed = false;
#endif

            private Idtr(FPLTranspilerBase fplTranspiler, int cindent)
            {
                this.fplTranspiler = fplTranspiler;
                this.cindent = cindent;
                this.idtrOuter = fplTranspiler.idtrCur;
                fplTranspiler.idtrCur = this;
            }

            public static Idtr Initial(FPLTranspilerBase fplTranspiler)
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
#if DEBUG
                Debug.Assert(!fDisposed);
                fDisposed = true;
#endif
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

            private Lne(Idtr idtr, Statm statm, Lne lneNext, Lne lneLast) : base(idtr)
            {
                this.statm = statm;
                this.lneNext = lneNext;
                this.lneLast = lneLast;
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
                return new Lne(lneRight.idtr, lneRight.statm == null ? statmLeft : statmLeft + lneRight.statm, lneRight.lneNext, lneRight.lneLast);
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
}
