#define VALIDATE_LNE
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

        protected string StNameFromProg(FPLParser.ProgContext prog)
            =>
                StNameFromIdentifier(prog.raytracer() == null
                    ? prog.fractal().identifier()
                    : prog.raytracer().identifier());

        protected string StNameFromIdentifier(FPLParser.IdentifierContext identifier)
            => VisitIdentifier(identifier).ToString();

        protected string StRaytracerName(FPLParser.RaytracerContext raytracer)
            => VisitIdentifier(raytracer.identifier()).ToString();

        internal void GenFile(FPLParser.ProgContext prog, string stDirectory)
        {
            idtrCur = Idtr.Initial(this);

            bool fRaytracer = prog.raytracer() != null;
            string stFileName = fRaytracer ? StRaytracerName(prog.raytracer()) : StNameFromIdentifier(prog.fractal().identifier());

            string stFileOutput = Path.Combine(stDirectory, stFileName + ".gen." + StExtension());
            Directory.CreateDirectory(stDirectory);
            using (FileStream fs = File.Create(stFileOutput))
            {
                using (TextWriter tw = new StreamWriter(fs))
                {
                    Losa losaProg = Visit(prog);
#if VALIDATE_LNE && DEBUG
                    if (losaProg is Lne)
                        ((Lne) losaProg).Validate();
#endif
                    string stGenFile = losaProg.ToStringFollowing();
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

        protected static void Error(string stMessage)
        {
            Console.WriteLine("FPL Error: " + stMessage);
        }

        protected internal static void Validate(ParserRuleContext context)
        {
            if (context == null)
                return;
            if (context.exception != null)
            {
                Error("Line " + context.exception.OffendingToken.Line + ": " + context.exception.OffendingToken);
            }
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

            public Idtr NewZero()
            {
                return new Idtr(fplTranspiler, 0);
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
            private Lne lnePrev;
            private Lne lneLast;
            private Lne lneLastOrSelf => lneLast ?? this;

            public Lne(Idtr idtr, Statm statm, Lne lneNext) : base(idtr)
            {
                this.statm = statm;
                this.lneNext = lneNext;
                this.lnePrev = null;
                this.lneLast = null;
                Debug.Assert(lneNext != this && lneLast != this && !(lneNext == null && lneLast != null));
            }

            private Lne(Idtr idtr, Statm statm, Lne lneNext, Lne lnePrev, Lne lneLast) : base(idtr)
            {
                this.statm = statm;
                this.lneNext = lneNext;
                this.lnePrev = lnePrev;
                this.lneLast = lneLast;
                Debug.Assert(lneNext != this && lneLast != this && !(lneNext == null && lneLast != null));
            }

            public override string ToString()
            {
                // special case of empty line is easy
                return statm == null ? Environment.NewLine : idtr.StIndent() + statm.ToString();
            }

            public static Lne operator +(Lne lneLeft, Lne lneRight)
            {
                Lne lneLastCur = lneLeft.lneLastOrSelf;
                Lne lneLastNew = lneRight.lneLastOrSelf;
                Debug.Assert(lneLastNew.lneNext == null && lneLastCur.lneNext == null);

                lneLastCur.lneNext = lneRight;
                lneRight.lnePrev = lneLastCur;

                lneLeft.lneLast = lneLastNew;
                lneRight.lneLast = null;

                return lneLeft;
            }

            public static Lne operator +(Lne lneLeft, Statm statmRight)
            {
                Lne lneLast = lneLeft.lneLastOrSelf;
                Lne lneSecondToLast = lneLast.lnePrev;
                Lne lneLastNew = lneLast.DupWith(lneLast.statm == null ? statmRight : lneLast.statm + statmRight);
                Debug.Assert(lneLastNew.lneNext == null);
                // Single linked-list case
                if (lneSecondToLast == null)
                    return lneLastNew;
                lneSecondToLast.lneNext = lneLastNew;
                lneLeft.lneLast = lneLastNew;
                Debug.Assert(lneLastNew.lnePrev != null && lneLastNew.lnePrev == lneSecondToLast && lneLeft.lneLast == lneLastNew);
                return lneLeft;
            }

            public static Lne operator +(Statm statmLeft, Lne lneRight)
            {
                Lne lneNew = lneRight.DupWith(lneRight.statm == null ? statmLeft : statmLeft + lneRight.statm);
                if (lneNew.lneNext != null)
                    lneNew.lneNext.lnePrev = lneNew;
                if (lneNew.lnePrev != null)
                    lneNew.lnePrev.lneNext = lneNew;
                return lneNew;
            }

            protected override Losa LosaAdd(Losa losaRight)
            {
                Lne lneRight = losaRight as Lne;
                if (lneRight != null)
                    return this + lneRight;
                return this + (Statm)losaRight;
            }

            private Lne DupWith(Statm statm)
            {
                return new Lne(idtr, statm, lneNext, lnePrev, lneLast);
            }

            public override string ToStringFollowing()
            {
                string stThis = ToString() + Environment.NewLine;
                if (lneNext != null)
                    stThis += lneNext.ToStringFollowing();
                return stThis;
            }

#if DEBUG
            public void Validate()
            {
                Lne lneCur = this;
                while (lneCur != null)
                {
                    Debug.Assert(lneCur.lneLast?.lneNext == null);
                    Debug.Assert(lneCur.lneNext == null || lneCur.lneNext.lnePrev == lneCur);
                    Debug.Assert(lneCur.lnePrev == null || lneCur.lneLast == null);
                    Debug.Assert(lneCur.lnePrev != null || lneCur.lneLast != null);
                    lneCur = lneCur.lneNext;
                }
            }
#endif
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
