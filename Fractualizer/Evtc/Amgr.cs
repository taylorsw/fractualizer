using System;
using System.Collections.Generic;
using System.Diagnostics;
using Util;

namespace Evtc
{
    // Kind of AniMation manaGmenT
    public enum Kamgt
    {
        Overridable,
        Fixed
    }

    // AniMation manaGeR
    public class Amgr
    {
        private readonly Dictionary<Avark, LinkedListNode<Avar>> mpavark_avar;
        private readonly LinkedList<Avar> llavar;

        public Amgr()
        {
            mpavark_avar = new Dictionary<Avark, LinkedListNode<Avar>>(5);
            llavar = new LinkedList<Avar>();
        }

        internal void Update(double dtms)
        {
            var lln = llavar.First;
            while (lln != null)
            {
                var llnNext = lln.Next;

                Avar avar = lln.Value;
                Debug.Assert(mpavark_avar.ContainsKey(avar.avark));
                if (avar.FExpired())
                {
                    Remove(lln);
                    Avar avarNext = avar.dgNext?.Invoke(avar);
                    if (avarNext != null)
                        Add(avarNext);
                }
                else
                {
                    avar.Update(dtms);
                }

                lln = llnNext;
            }
        }

        private void Add(Avar avar)
        {
            LinkedListNode<Avar> llnavarCur;
            if (mpavark_avar.TryGetValue(avar.avark, out llnavarCur))
            {
                switch (avar.kamgt)
                {
                    case Kamgt.Overridable:
                        Remove(llnavarCur);
                        break;
                    case Kamgt.Fixed:
                        return;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            avar.Initialize();
            var llnavar = llavar.AddLast(avar);
            mpavark_avar.Add(avar.avark, llnavar);
        }

        private void Remove(LinkedListNode<Avar> llnavar)
        {
            mpavark_avar.Remove(llnavar.Value.avark);
            llavar.Remove(llnavar);
        }

        public void Tween(Avar avar)
        {
            Add(avar);
        }

        public void Cancel(Avar avar)
        {
            LinkedListNode<Avar> llnavar;
            if (mpavark_avar.TryGetValue(avar.avark, out llnavar))
            {
                Remove(llnavar);
            }
        }
    }

    public class Avark
    {
        public static Avark New()
        {
            return new Avark();
        }

        private Avark()
        {
        }
    }

    // Animated VARiable
    public abstract class Avar
    {
        public delegate Avar DgAvarNext(Avar avarPrev);

        internal DgAvarNext dgNext { get; private set; }

        internal readonly Kamgt kamgt;

        internal double val;
        internal double valDst;

        public readonly Avark avark;

        protected Avar(Avark avark = null, Kamgt kamgt = Kamgt.Overridable)
        {
            this.dgNext = dgNext;
            this.avark = avark ?? Avark.New();
            this.kamgt = kamgt;
            val = valDst = double.NaN;
        }

        internal abstract void Initialize();
        internal abstract bool FExpired();

        internal void Update(double dtms)
        {
            val = ValUpdated(dtms);
            UpdateI(dtms);
        }

        protected abstract void UpdateI(double dtms);
        protected abstract double ValUpdated(double dtms);

        public void SetDgNext(DgAvarNext dgNext)
        {
            this.dgNext = dgNext;
        }
    }

    public abstract class TavarNone
    {
    }

    public abstract class Avar<T> : Avar
    {
        public delegate double DgReadVal(Avar<T> avar);

        protected static readonly DgReadVal dgReadNan = avar => double.NaN;
        protected static readonly DgReadVal dgReadZero = avar => 0;

        public delegate void DgWriteVal(Avar<T> avar, double val);

        private readonly DgReadVal dgInitialValue, dgValDst;
        private readonly DgWriteVal dgWrite;

        public T tval;

        protected Avar(DgReadVal dgInitialValue, DgReadVal dgValDst, DgWriteVal dgWriteVal, T tval = default(T), Avark avark = null, Kamgt kamgt = Kamgt.Overridable) : base(avark, kamgt)
        {
            this.tval = tval;
            this.dgInitialValue = dgInitialValue;
            this.dgValDst = dgValDst;
            this.dgWrite = dgWriteVal;
        }

        internal sealed override void Initialize()
        {
            val = dgInitialValue(this);
            valDst = dgValDst(this);
            InitializeI();
        }

        protected abstract void InitializeI();

        protected sealed override void UpdateI(double dtms)
        {
            dgWrite(this, val);
        }
    }

    public class AvarLinearDiscrete<T> : Avar<T>
    {
        protected readonly double dtmsPeriod;
        protected double dval_dtms = double.NaN;

        private static double Dval_dtms(double dval_dtms, double val, double valDst)
            => Math.Abs(dval_dtms)*Math.Sign(valDst - val);

        public AvarLinearDiscrete(DgReadVal dgReadVal, double valDst, DgWriteVal dgWriteVal, double dtmsPeriod, T tval = default(T), Avark avark = null, Kamgt kamgt = Kamgt.Overridable) : base(dgReadVal, avar => valDst, dgWriteVal, tval, avark, kamgt)
        {
            this.dtmsPeriod = dtmsPeriod;
        }

        public AvarLinearDiscrete(DgReadVal dgReadVal, double valDst, double dval_dtms, DgWriteVal dgWriteVal, T tval = default(T), Avark avark = null, Kamgt kamgt = Kamgt.Overridable) : this(dgReadVal, valDst, dgWriteVal, double.NaN, tval, avark, kamgt)
        {
            this.dval_dtms = dval_dtms;
        }

        public AvarLinearDiscrete(double valInitial, double valDst, double dval_dtms, DgWriteVal dgWriteVal, T tval = default(T), Avark avark = null, Kamgt kamgt = Kamgt.Overridable) : this(avar => valInitial, valDst, dgWriteVal, double.NaN, tval, avark, kamgt)
        {
            this.dval_dtms = dval_dtms;
        }

        public AvarLinearDiscrete(double valInitial, double valDst, DgWriteVal dgWriteVal, double dtmsPeriod, T tval = default(T), Avark avark = null, Kamgt kamgt = Kamgt.Overridable) : this(avar => valInitial, valDst, dgWriteVal, dtmsPeriod, tval, avark, kamgt)
        {
        }

        public static AvarLinearDiscrete<T> BounceBetween(DgReadVal dgReadVal, DgWriteVal dgWriteVal, double valMin, double valMax, double dval_dtms, Avark avark = null)
        {
            Avark avarkKey = avark ?? Avark.New();
            var avarBounceForward = new AvarLinearDiscrete<T>(dgReadVal: dgReadVal, valDst: valMax, dgWriteVal: dgWriteVal, dval_dtms: dval_dtms, avark: avarkKey);
            var avarBounceBackwards = new AvarLinearDiscrete<T>(dgReadVal: dgReadVal, valDst: valMin, dgWriteVal: dgWriteVal, dval_dtms: dval_dtms, avark: avarkKey);
            avarBounceForward.SetDgNext(avarPrev => avarBounceBackwards);
            avarBounceBackwards.SetDgNext(avarPrev => avarBounceForward);
            return avarBounceForward;
        }

        public delegate void DgSetPt(Vector3f pt);
        public static AvarLinearDiscrete<T> LerpPt(Vector3f ptStart, Vector3f ptEnd, float dtmsPeriod, DgSetPt dgSetPt)
        {
            return new AvarLinearDiscrete<T>(
                0,
                1.0,
                (avar, fr) => dgSetPt(U.Lerp(ptStart, ptEnd, fr)),
                dtmsPeriod);
        }

        protected override void InitializeI()
        {
            if (!double.IsNaN(dtmsPeriod))
                dval_dtms = (valDst - val)/dtmsPeriod;
            else
            {
                dval_dtms = Dval_dtms(dval_dtms, val, valDst);
            }
        }

        internal override bool FExpired()
        {
            return dval_dtms > 0 ? val > valDst : val < valDst;
        }

        protected override double ValUpdated(double dtms)
        {
            return val + dval_dtms*dtms;
        }
    }

    public abstract class AvarLinearDiscreteEasing<T> : Avar<T>
    {
        private double dtmsCur, valInitial;
        private readonly double dtmsDuration;

        protected AvarLinearDiscreteEasing(DgReadVal dgInitialValue, DgReadVal dgValDst, DgWriteVal dgWriteVal,
            float dtmsDuration, T tval = default(T), Avark avark = null, Kamgt kamgt = Kamgt.Overridable)
            : base(dgInitialValue, dgValDst, dgWriteVal, tval, avark, kamgt)
        {
            dtmsCur = 0;
            this.dtmsDuration = dtmsDuration;
        }

        protected AvarLinearDiscreteEasing(DgReadVal dgInitialValue, double valDst, DgWriteVal dgWriteVal,
            float dtmsDuration, T tval = default(T), Avark avark = null, Kamgt kamgt = Kamgt.Overridable)
            : this(dgInitialValue, _ => valDst, dgWriteVal, dtmsDuration, tval, avark, kamgt)
        {
        }

        internal override bool FExpired() => dtmsCur >= dtmsDuration;

        protected override double ValUpdated(double dtms)
        {
            this.dtmsCur += Math.Min(dtms, dtmsDuration - dtmsCur);
            double valUpdated = Ease(dtmsCur, dtmsDuration, valInitial, valDst - valInitial);
            Debug.WriteLine(valUpdated);
            return valUpdated;
        }

        protected abstract double Ease(double dtmsCur, double dtmsDuration, double valInitial, double dval);

        protected override void InitializeI()
        {
            this.valInitial = val;
        }
    }

    public class AvarLinearDiscreteQuadraticEaseIn<T> : AvarLinearDiscreteEasing<T>
    {
        public AvarLinearDiscreteQuadraticEaseIn(DgReadVal dgInitialValue, double valDst, DgWriteVal dgWriteVal,
            float dtmsDuration, T tval = default(T), Avark avark = null, Kamgt kamgt = Kamgt.Overridable)
            : base(dgInitialValue, valDst, dgWriteVal, dtmsDuration, tval, avark, kamgt)
        {
        }

        protected override double Ease(double dtmsCur, double dtmsDuration, double valInitial, double dval)
        {
            dtmsCur /= dtmsDuration;
            return dval*dtmsCur*dtmsCur + valInitial;
        }
    }

    public class AvarLinearDiscreteQuadraticEaseInOut<T> : AvarLinearDiscreteEasing<T>
    {
        public AvarLinearDiscreteQuadraticEaseInOut(DgReadVal dgInitialValue, double valDst, DgWriteVal dgWriteVal,
            float dtmsDuration, T tval = default(T), Avark avark = null, Kamgt kamgt = Kamgt.Overridable)
            : base(dgInitialValue, valDst, dgWriteVal, dtmsDuration, tval, avark, kamgt)
        {
        }

        protected override double Ease(double dtmsCur, double dtmsDuration, double valInitial, double dval)
        {
            dtmsCur /= dtmsDuration/2;
            if (dtmsCur < 1)
                return dval/2*dtmsCur*dtmsCur + valInitial;
            dtmsCur--;
            return -dval/2*(dtmsCur*(dtmsCur - 2) - 1) + valInitial;
        }
    }

    public class AvarLinearIndefinite<T> : Avar<T>
    {
        private readonly double dval_dtms;

        public AvarLinearIndefinite(DgReadVal dgReadVal, DgWriteVal dgWriteVal, double dval_dtms, T tval = default(T), Avark avark = null, Kamgt kamgt = Kamgt.Overridable) : base(dgReadVal, dgReadNan, dgWriteVal, tval, avark, kamgt)
        {
            this.dval_dtms = dval_dtms;
        }

        public AvarLinearIndefinite(double valInitial, DgWriteVal dgWriteVal, double dval_dtms, T tval = default(T), Avark avark = null, Kamgt kamgt = Kamgt.Overridable) : this(avar => valInitial, dgWriteVal, dval_dtms, tval, avark, kamgt)
        {
            this.dval_dtms = dval_dtms;
        }

        protected override void InitializeI()
        {
        }

        internal override bool FExpired() => false;

        protected override double ValUpdated(double dtms)
        {
            return val + dval_dtms*dtms;
        }
    }

    public class AvarIndefinite<T> : Avar<T>
    {
        public AvarIndefinite(DgWriteVal dgWriteVal, T tval = default(T), Avark avark = null, Kamgt kamgt = Kamgt.Overridable) : base(dgReadZero, dgReadNan, dgWriteVal, tval, avark, kamgt)
        {
        }

        internal override bool FExpired() => false;

        protected override double ValUpdated(double dtms) => dtms;

        protected override void InitializeI()
        {
        }
    }
}
