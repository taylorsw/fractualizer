using System;
using System.Diagnostics;
using FPL;

namespace CodeGen
{
    internal class FPLToHLSL : FPLToCLL
    {
        public override Losa VisitPifdef(FPLParser.PifdefContext pifdef)
        {
            return "#ifdef";
        }

        public override Losa VisitPifndef(FPLParser.PifndefContext pifndef)
        {
            return "#ifndef";
        }

        public override Losa VisitPendif(FPLParser.PendifContext pendif)
        {
            return "#endif";
        }

        public override Losa VisitPelse(FPLParser.PelseContext pelse)
        {
            return "#else";
        }

        private Losa LosaInclude(string stFile) => LneNew(@"#include """ + stFile + @"""");

        public override Losa VisitRaytracer(FPLParser.RaytracerContext raytracer)
        {
            Losa losaRaytracer = LosaInclude(GenU.stFractalInclude);

            if (raytracer.input().Length > 0)
                losaRaytracer += LosaInputs(raytracer.identifier(), raytracer.input(), GenU.ibufferRaytracer);

            losaRaytracer += LosaVisitGlobals(raytracer.global());

            losaRaytracer += VisitTracer(raytracer.tracer());

            return losaRaytracer;
        }

        public override Losa VisitTracer(FPLParser.TracerContext tracer)
        {
            Losa losaTracer = LneNew("float4 main(float4 pos : SV_POSITION) : SV_TARGET") + VisitBlock(tracer.block());
            return losaTracer;
        }

        public override Losa VisitFractal(FPLParser.FractalContext fractal)
        {
            Losa losaProg = "";

            if (fractal.input().Length > 0)
            {
                Losa losaInputs = LosaInputs(fractal.identifier(), fractal.input(), GenU.ibufferFractal);
                losaProg += losaInputs;
            }

            losaProg += LosaVisitGlobals(fractal.global());

            losaProg += VisitDistanceEstimator(fractal.distanceEstimator());

            return losaProg;
        }

        public override Losa VisitInputAccess(FPLParser.InputAccessContext inputAccess)
        {
            return VisitIdentifier(inputAccess.identifier());
        }

        public override Losa VisitFractalAccess(FPLParser.FractalAccessContext fractalAccess)
        {
            return Visit(fractalAccess.children[fractalAccess.ChildCount - 1]);
        }

        private Losa LosaVisitGlobals(FPLParser.GlobalContext[] rgglobal)
        {
            Losa losaGlobals = LneNew();
            foreach (FPLParser.GlobalContext global in rgglobal)
            {
                losaGlobals += VisitGlobal(global);
                if (global.globalVal() == null)
                    losaGlobals += LneNew();
            }
            return losaGlobals;
        }

        public override Losa VisitInput(FPLParser.InputContext input)
        {
            return LneNew() +
                   LosaLocalDecl(input.inputType().type(), input.identifier(),
                       input.arrayDecl() == null ? new FPLParser.ArrayDeclContext[0] : new[] {input.arrayDecl()}, null);
        }

        private Losa LosaInputs(FPLParser.IdentifierContext identifier, FPLParser.InputContext[] rginput, int cbuffer)
        {
            Losa losaInputs = LneNew("cbuffer " + StNameFromIdentifier(identifier) + " : register(b" + cbuffer + ")") + LneNew("{");
            using (idtrCur.New())
            {
                foreach (FPLParser.InputContext input in rginput)
                    losaInputs += VisitInput(input);
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

        public override Losa VisitLocalDecl(FPLParser.LocalDeclContext localDecl)
        {
            return LosaLocalDecl(localDecl.type(), localDecl.identifier(), localDecl.arrayDecl(), localDecl.expr());
        }

        private Losa LosaLocalDecl(FPLParser.TypeContext type, FPLParser.IdentifierContext identifier, FPLParser.ArrayDeclContext[] rgarrayDecl, FPLParser.ExprContext expr)
        {
            Losa losaLocalDecl = VisitType(type) + " " + VisitIdentifier(identifier);
            if (rgarrayDecl.Length > 0)
            {
                foreach (var arrayDecl in rgarrayDecl)
                    losaLocalDecl += "[" + VisitExpr(arrayDecl.expr()) + "]";
            }
            else if (expr != null)
            {
                losaLocalDecl += " = " + VisitExpr(expr);
            }
            losaLocalDecl += ";";
            return losaLocalDecl;
        }

        public override Losa VisitGlobalVal(FPLParser.GlobalValContext global)
        {
            return LneNew("static const ") + VisitLocalDecl(global.localDecl());
        }

        public override Losa VisitInstantiation(FPLParser.InstantiationContext instantiation)
        {
            return VisitType(instantiation.type()) + "(" + VisitExprList(instantiation.exprList()) + ")";
        }

        public override Losa VisitInputType(FPLParser.InputTypeContext inputType)
        {
            return VisitType(inputType.type());
        }

        public override Losa VisitType(FPLParser.TypeContext type)
        {
            Losa losaType;
            if (type.V2Type() != null)
                losaType = "float2";
            else if (type.V3Type() != null)
                losaType = "float3";
            else if (type.V4Type() != null)
                losaType = "float4";
            else
                losaType = type.GetText();

            return losaType;
        }

        protected override string StExtension() => "hlsl";
    }
}
