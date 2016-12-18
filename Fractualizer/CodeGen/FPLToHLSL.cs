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

            losaRaytracer += LosaInputsAndTextures(raytracer.identifier(), raytracer.inputs(), GenU.ibufferRaytracer);

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
            FPLParser.InputsContext inputs = fractal.inputs();

            losaProg += LosaInputsAndTextures(fractal.identifier(), inputs, GenU.ibufferFractal);

            losaProg += LosaVisitGlobals(fractal.global());

            losaProg += VisitDistanceEstimator(fractal.distanceEstimator());
            losaProg += VisitColorFunc(fractal.colorFunc());

            return losaProg;
        }

        public override Losa VisitInputAccess(FPLParser.InputAccessContext inputAccess)
        {
            return VisitIdentifier(inputAccess.identifier());
        }

        public override Losa VisitSample(FPLParser.SampleContext sample)
        {
            FPLParser.InputAccessContext inputAccess = sample.inputAccess();
            return VisitInputAccess(inputAccess) + ".Sample(" + LosaSampName(inputAccess.identifier()) + ", " +
                   VisitExpr(sample.expr()) + ")";
        }

        public override Losa VisitFractalAccess(FPLParser.FractalAccessContext fractalAccess)
        {
            return Visit(fractalAccess.children[fractalAccess.ChildCount - 1]);
        }

        public override bool FCompilesOptionalBlocks()
        {
            return true;
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

        private Losa LosaSampName(FPLParser.IdentifierContext identifier)
        {
            return "samp" + VisitIdentifier(identifier);
        }

        private Losa LosaInputsAndTextures(FPLParser.IdentifierContext identifier, FPLParser.InputsContext inputs, int cbuffer)
        {
            Losa losaInputsAndTextures = "";

            FPLParser.TextureContext[] rgtexture = inputs.texture();
            if (rgtexture.Length > 0)
            {
                for (int itexture = 0; itexture < rgtexture.Length; itexture++)
                {
                    FPLParser.TextureContext texture = rgtexture[itexture];
                    losaInputsAndTextures += LneNew("Texture2D ") + VisitIdentifier(texture.identifier()) +
                                             " : register(t" + itexture.ToString() + ");";
                    losaInputsAndTextures += LneNew("SamplerState ") +
                                             LosaSampName(texture.identifier()) +
                                             " : register(s" + itexture.ToString() + ");";
                }
            }

            FPLParser.InputContext[] rginput = inputs.input();
            if (rginput.Length > 0)
            {
                Losa losaInputs = LneNew("cbuffer " + StNameFromIdentifier(identifier) + " : register(b" + cbuffer + ")") + LneNew("{");
                using (idtrCur.New())
                {
                    foreach (FPLParser.InputContext input in rginput)
                        losaInputs += VisitInput(input);
                }
                losaInputs += LneNew("}");
                losaInputsAndTextures += losaInputs;
            }

            return losaInputsAndTextures;
        }

        public override Losa VisitDistanceEstimator(FPLParser.DistanceEstimatorContext distanceEstimator)
        {
            Losa losaDE = LneNew("float DuDeFractal(float3 pos)") + VisitBlock(distanceEstimator.block());
            return losaDE;
        }

        public override Losa VisitColorFunc(FPLParser.ColorFuncContext context)
        {
            return LneNew("float3 Color(float3 pt)") + VisitBlock(context.block());
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
