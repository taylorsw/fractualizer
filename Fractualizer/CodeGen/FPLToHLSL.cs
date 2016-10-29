using FPL;

namespace CodeGen
{
    internal class FPLToHLSL : FPLToCLL
    {
        private Losa LosaInclude(string stFile) => LneNew(@"#include """ + stFile + @"""");
        public override Losa VisitFractal(FPLParser.FractalContext fractal)
        {
#if VALIDATE_GEN
            Validate(fractal);
#endif
            Losa losaProg = LosaInclude("parameters.hlsl");

            if (fractal.input().Length > 0)
            {
                Losa losaInputs = LosaInputs(fractal);
                losaProg += losaInputs;
            }

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

        private Losa LosaInputs(FPLParser.FractalContext fractal)
        {
            Losa losaInputs = LneNew("cbuffer " + StFractalName(fractal) + " : register(b1)") + LneNew("{");
            using (idtrCur.New())
            {
                foreach (FPLParser.InputContext input in fractal.input())
                {
                    losaInputs += LneNew() + VisitInputType(input.inputType()) + " " +
                                  VisitIdentifier(input.identifier()) + ";";
                }
            }
            losaInputs += LneNew("}");
            return losaInputs;
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
}
