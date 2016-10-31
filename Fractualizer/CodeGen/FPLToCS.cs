using System.Collections.Generic;
using System.Diagnostics;
using FPL;

namespace CodeGen
{
    internal class FPLToCS : FPLToCLL
    {
        private class FPLTableBuilder : FPLBaseVisitor<int>
        {
            public readonly Dictionary<string, List<FPLParser.ArgModContext>> mpstFunc_rgargmod;
            public readonly Dictionary<string, FPLParser.InputContext> mpstIdentifier_input;

            public FPLTableBuilder()
            {
                mpstFunc_rgargmod = new Dictionary<string, List<FPLParser.ArgModContext>>();
                mpstIdentifier_input = new Dictionary<string, FPLParser.InputContext>();
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
                return base.VisitFunc(func);
            }

            public override int VisitInput(FPLParser.InputContext input)
            {
                FPLParser.IdentifierContext identifier = input.identifier();
                string stIdentifier = StFromIdentifier(identifier);
                mpstIdentifier_input[stIdentifier] = input;
                return base.VisitInput(input);
            }

            public static string StFromIdentifier(FPLParser.IdentifierContext identifier)
            {
                return identifier.GetText();
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

            Losa losaProg = LneNew("using System;") +
                            LneNew("using Fractals;") +
                            LneNew("using System.Runtime.InteropServices;") +
                            LneNew("using SharpDX;") +
                            LneNew("using SharpDX.Direct3D11;");
            losaProg += LneNew("namespace Fractals") + LneNew("{");
            using (idtrCur.New())
            {
                losaProg += LneNew("public class ") + StFractalName(fractal) + " : Fractal3d" + LneNew("{");
                using (idtrCur.New())
                {
                    if (fractal.input().Length > 0)
                    {
                        Losa losaInputs = LosaClassBodyInputsExtras(fractal);
                        losaProg += losaInputs;
                    }

                    foreach (var global in fractal.global())
                    {
                        losaProg += VisitGlobal(global);
                        if (global.globalVal() == null)
                            losaProg += LneNew();
                    }

                    losaProg += VisitDistanceEstimator(fractal.distanceEstimator());
                }
                losaProg += LneNew("}");
            }
            losaProg += LneNew("}");
            return losaProg;
        }

        private string StStructName(string stFractalName)
        {
            return "_" + stFractalName;
        }

        private string StStructMemberName(string stFractalName)
        {
            return StStructName(stFractalName).ToLower();
        }

        public Losa LosaClassBodyInputsExtras(FPLParser.FractalContext fractal)
        {
            FPLParser.InputContext[] rginput = fractal.input();
            string stFractalName = StFractalName(fractal);
            string stStructName = StStructName(stFractalName);
            string stStructMemberName = StStructMemberName(stFractalName);
            int cbyteMembers = rginput.Length*4;
            int cbyteTotal = cbyteMembers + (16 - cbyteMembers%16)%16;
            Losa losaStruct =
                LneNew("[StructLayout(LayoutKind.Explicit, Size=" + cbyteTotal + ")]") +
                LneNew("public struct " + stStructName) +
                LneNew("{");
            Dictionary<string, List<FPLParser.InputContext>> mpstType_rginput = new Dictionary<string, List<FPLParser.InputContext>>(rginput.Length);
            using (idtrCur.New())
            {
                Losa losaSingleton = LneNew("public static readonly " + stStructName + " I = new " + stStructName + "(");
                for (int iinput = 0; iinput < rginput.Length; iinput++)
                {
                    FPLParser.InputContext input = rginput[iinput];
                    losaSingleton += VisitIdentifier(input.identifier()) + ": " + VisitLiteral(input.literal());
                    if (iinput < rginput.Length - 1)
                        losaSingleton += ", ";
                }
                losaSingleton += ");";
                losaStruct += losaSingleton;

                int cbyteOffset = 0;
                foreach (FPLParser.InputContext input in rginput)
                {
                    Losa losaInputType = VisitInputType(input.inputType());
                    Losa losaField = 
                        LneNew("[FieldOffset(" + cbyteOffset + ")]") +
                        (LneNew("public ") + losaInputType + " " + VisitIdentifier(input.identifier()) + ";");
                    losaStruct += losaField;

                    string stType = losaInputType.ToStringFollowing();
                    List<FPLParser.InputContext> rginputOfType;
                    if (!mpstType_rginput.TryGetValue(stType, out rginputOfType))
                        mpstType_rginput[stType] = rginputOfType = new List<FPLParser.InputContext>();
                    rginputOfType.Add(input);

                    cbyteOffset += 4;
                }

                Losa losaStructConstructorVerbose = LneNew("public " + stStructName + "(");
                for (int iinput = 0; iinput < rginput.Length; iinput++)
                {
                    FPLParser.InputContext input = rginput[iinput];
                    losaStructConstructorVerbose += VisitInputType(input.inputType()) + " " + VisitIdentifier(input.identifier()) + " = " + VisitLiteral(input.literal());
                    if (iinput < rginput.Length - 1)
                        losaStructConstructorVerbose += ", ";
                }
                losaStructConstructorVerbose += ")";
                losaStructConstructorVerbose += LneNew("{");
                using (idtrCur.New())
                {
                    foreach (FPLParser.InputContext input in rginput)
                        losaStructConstructorVerbose += LneNew() + "this." + VisitIdentifier(input.identifier()) + " = " +
                                      VisitIdentifier(input.identifier()) + ";";
                }
                losaStructConstructorVerbose += LneNew("}");
                losaStruct += losaStructConstructorVerbose;
            }
            losaStruct += LneNew("}");

            Losa losaClassBody = losaStruct;
            losaClassBody += LneNew("public " + stStructName + " " + stStructMemberName + ";");
            losaClassBody += LneNew("private SharpDX.Direct3D11.Buffer buffer;");

            Losa losaClassContstructorEmpty = LneNew("public " + stFractalName + "()") +
                                              LneNew("{");
            using (idtrCur.New())
            {
                losaClassContstructorEmpty += LneNew("this." + stStructMemberName + " = " + stStructName + ".I;");
            }
            losaClassContstructorEmpty += LneNew("}");
            losaClassBody += losaClassContstructorEmpty;

            Losa losaClassConstructorVerbose = LneNew("public " + stFractalName + "(" + stStructName + " " + stStructMemberName + ")")
                + LneNew("{");
            using (idtrCur.New())
            {
                losaClassConstructorVerbose += LneNew("this." + stStructMemberName + " = " + stStructMemberName + ";");
            }
            losaClassConstructorVerbose += LneNew("}");
            losaClassBody += losaClassConstructorVerbose;


            Losa losaInitializeBuffer =
                LneNew("internal override void InitializeBuffer(Device device, DeviceContext deviceContext)") + LneNew("{");
            using (idtrCur.New())
            {
                losaInitializeBuffer +=
                    LneNew("buffer = Util.BufferCreate(device, deviceContext, 1, ref " + stStructMemberName + ");");
            }
            losaInitializeBuffer += LneNew("}");
            losaClassBody += losaInitializeBuffer;

            Losa losaUpdateBuffer =
                LneNew("internal override void UpdateBuffer(Device device, DeviceContext deviceContext)") + LneNew("{");
            using (idtrCur.New())
            {
                losaUpdateBuffer += LneNew("Util.UpdateBuffer(device, deviceContext, buffer, ref " + stStructMemberName + ");");
            }
            losaUpdateBuffer += LneNew("}");
            losaClassBody += losaUpdateBuffer;

            Losa losaReset =
                LneNew("public override void ResetInputs() { " + stStructMemberName + " = " + stStructName + ".I; }");
            losaClassBody += losaReset;

            foreach (string stTypeInput in mpstType_rginput.Keys)
            {
                FPLParser.InputTypeContext inputType = mpstType_rginput[stTypeInput][0].inputType();
                List<FPLParser.InputContext> rginputForType = mpstType_rginput[stTypeInput];

                string stTypeCap = char.ToUpper(stTypeInput[0]) + stTypeInput.Substring(1, stTypeInput.Length - 1);
                string stGetMethodName = "GetInput" + stTypeCap;

                losaClassBody += LneNew("public override int cinput" + stTypeCap + " => " + rginputForType.Count + ";");

                Losa losaInputGet = LneNew("public override ") + VisitInputType(inputType) + " " + stGetMethodName + "(int iinput)" + LneNew("{");
                using (idtrCur.New())
                {
                    for (int iinput = 0; iinput < rginputForType.Count; iinput++)
                    {
                        FPLParser.InputContext input = rginputForType[iinput];
                        losaInputGet += LneNew("if (iinput == " + iinput + ") return " + stStructMemberName + ".") + VisitIdentifier(input.identifier()) + ";";
                    }

                    losaInputGet += LneNew("return base." + stGetMethodName + "(iinput);");
                }
                losaInputGet += LneNew("}");

                losaClassBody += losaInputGet;

                string stSetMethodName = "SetInput" + stTypeCap;
                Losa losaInputSet = LneNew("public override void " + stSetMethodName + "(int iinput, ") +
                                    VisitInputType(inputType) + " val)"
                                    + LneNew("{");
                using (idtrCur.New())
                {
                    for (int iinput = 0; iinput < rginputForType.Count; iinput++)
                    {
                        FPLParser.InputContext input = rginputForType[iinput];
                        losaInputSet += LneNew("if (iinput == " + iinput + ") " + stStructMemberName + ".") + VisitIdentifier(input.identifier()) + " = val;";
                    }

                    losaInputSet += LneNew("base." + stSetMethodName + "(iinput, val);");
                }
                losaInputSet += LneNew("}");

                losaClassBody += losaInputSet;
            }

            Losa losaDispose = LneNew("public override void Dispose() { base.Dispose(); buffer.Dispose(); }");
            losaClassBody += losaDispose;

            return losaClassBody;
        }

        private Losa LosaUseInputIdentifier(FPLParser.InputContext input)
        {
            return StStructMemberName(StFractalName((FPLParser.FractalContext) input.Parent)) + "." +
                   VisitIdentifier(input.identifier());
        }

        public override Losa VisitDistanceEstimator(FPLParser.DistanceEstimatorContext distanceEstimator)
        {
            Losa losa = LneNew("protected override double DuEstimateI(Vector3d pos)") + VisitBlock(distanceEstimator.block());
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

        public override Losa VisitExpr(FPLParser.ExprContext expr)
        {
            FPLParser.IdentifierContext identifier = expr.identifier();
            if (identifier != null && expr.ChildCount == 1)
            {
                string stIdentifier = FPLTableBuilder.StFromIdentifier(identifier);
                FPLParser.InputContext input;
                if (fplTableBuilder.mpstIdentifier_input.TryGetValue(stIdentifier, out input))
                {
                    return LosaUseInputIdentifier(input);
                }
            }
            return base.VisitExpr(expr);
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

        public override Losa VisitLiteralFloat(FPLParser.LiteralFloatContext literalFloat)
        {
            Losa losaFloat = base.VisitLiteralFloat(literalFloat);
            if (literalFloat?.parent?.parent is FPLParser.InputContext)
                losaFloat += "f";
            return losaFloat;
        }

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
}
