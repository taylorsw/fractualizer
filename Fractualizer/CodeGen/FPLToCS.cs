using System;
using System.Collections.Generic;
using System.Diagnostics;
using FPL;
using Util;

namespace CodeGen
{
    internal class FPLToCS : FPLToCLL
    {
        private delegate Losa DgLosaCreator();

        public class Ipt
        {
            public int ibyteOffset;

            public Ipt(int ibyteOffset)
            {
                this.ibyteOffset = ibyteOffset;
            }
        }

        private class FPLPreProcessor : FPLBaseVisitor<int>
        {
            public readonly Dictionary<string, List<FPLParser.ArgModContext>> mpstFunc_rgargmod;
            public readonly Dictionary<string, FPLParser.InputContext> mpstIdentifier_input;
            public readonly Dictionary<FPLParser.InputContext, Ipt> mpinput_ipt;
            public int cbyteInputsTotal;

            public FPLPreProcessor()
            {
                mpstFunc_rgargmod = new Dictionary<string, List<FPLParser.ArgModContext>>();
                mpstIdentifier_input = new Dictionary<string, FPLParser.InputContext>();
                mpinput_ipt = new Dictionary<FPLParser.InputContext, Ipt>();
            }

            public override int VisitProg(FPLParser.ProgContext context)
            {
                mpstFunc_rgargmod.Clear();
                mpstIdentifier_input.Clear();
                return base.VisitProg(context);
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

            public override int VisitInputs(FPLParser.InputsContext inputs)
            {
                int cbyteMembers = 0;
                int ibyteOffset = 0;
                foreach (FPLParser.InputContext input in inputs.input())
                {
                    FPLParser.IdentifierContext identifier = input.identifier();
                    string stIdentifier = StFromIdentifier(identifier);
                    mpstIdentifier_input[stIdentifier] = input;

                    int cbyteInput = SizeOf(input);
                    int ibyteNextAlignment = Util.U.RoundToByteOffset(ibyteOffset);
                    int cbyteDiff = ibyteNextAlignment - ibyteOffset;
                    if (cbyteDiff != 0 && cbyteDiff < cbyteInput)
                        ibyteOffset = ibyteNextAlignment;

                    mpinput_ipt[input] = new Ipt(ibyteOffset);

                    cbyteMembers = ibyteOffset + cbyteInput;
                    ibyteOffset = ibyteOffset + cbyteInput;
                }
                cbyteInputsTotal = U.RoundToByteOffset(cbyteMembers);
                return base.VisitInputs(inputs);
            }

            public static string StFromIdentifier(FPLParser.IdentifierContext identifier)
            {
                return identifier.GetText();
            }
        }

        private FPLPreProcessor _fplPreProcessor;

        private static readonly Dictionary<string, string> mpstBuiltinFpl_stCs = new Dictionary<string, string>
        {
            {"length", "Vector3d.Length"},
            {"dot", "Vector3d.Dot" },
            {"cross", "Vector3d.Cross" },
            {"lerp", "U.Lerp" },
            {"clamp", "U.Clamp"},
            {"floor", "U.Floor"},
            {"frac", "U.Frac"},
            {"atan", "U.Atan"},
            {"abs", "U.Abs"},
            {"normalize", "VectorUtil.Normalize" },
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
            {"exp", "Math.Exp"},
        };

        public override Losa VisitPifdef(FPLParser.PifdefContext pifdef)
        {
            return "#if";
        }

        public override Losa VisitPifndef(FPLParser.PifndefContext pifndef)
        {
            return "#if !";
        }

        public override Losa VisitPendif(FPLParser.PendifContext pendif)
        {
            return "#endif";
        }

        public override Losa VisitPelse(FPLParser.PelseContext pelse)
        {
            return "#else";
        }

        private Losa LosaUsings()
        {
            return LneNew("using System;") +
                   LneNew("using Fractals;") +
                   LneNew("using Util;") +
                   LneNew("using SharpDX;") +
                   LneNew("using SharpDX.Direct3D11;");
        }

        private Losa LosaInNamespace(DgLosaCreator dgLosaInner)
        {
            Losa losaNamespace = LneNew("namespace Fractals") + LneNew("{");
            using (idtrCur.New())
            {
                losaNamespace += dgLosaInner();
            }
            losaNamespace += LneNew("}");
            return losaNamespace;
        }

        private Losa LosaInClass(DgLosaCreator dgLosaInner, string stClassName, string stBaseClass)
        {
            Losa losaClass = LneNew("public partial class ") + stClassName + " : " + stBaseClass + LneNew("{");
            using (idtrCur.New())
            {
                losaClass += dgLosaInner();
            }
            losaClass += LneNew("}");
            return losaClass;
        }

        private Losa LosaGlobals(FPLParser.GlobalContext[] rgglobal)
        {
            Losa losaGlobals = "";
            foreach (FPLParser.GlobalContext global in rgglobal)
            {
                losaGlobals += VisitGlobal(global);
                if (global.globalVal() == null)
                    losaGlobals += LneNew();
            }
            return losaGlobals;
        }

        public override Losa VisitProg(FPLParser.ProgContext prog)
        {
            _fplPreProcessor = new FPLPreProcessor();
            _fplPreProcessor.VisitProg(prog);
            return base.VisitProg(prog);
        }

        public override Losa VisitInput(FPLParser.InputContext input)
        {
            return base.VisitInput(input);
        }

        public override Losa VisitRaytracer(FPLParser.RaytracerContext raytracer)
        {
            Losa losaUsings = LosaUsings();
            FPLParser.IdentifierContext identifierRaytracer = raytracer.identifier();
            string stRaytracerClassName = StNameFromIdentifier(identifierRaytracer);
            Losa losaClass = LosaInNamespace(
                () => LosaInClass(
                    () =>
                    {
                        string stInputClassName, stInputClassMemberName;
                        Losa losaInputClass = LosaInputClass(stRaytracerClassName, raytracer.inputs(), out stInputClassName, out stInputClassMemberName);
                        Losa losaStructMemberAndBuffer = LosaClassMemberAndBuffer(stInputClassName, stInputClassMemberName);
                        Losa losaConstructor = LneNew("public " + stRaytracerClassName + "(Scene scene, int width, int height) : base(scene, width, height) { " + stInputClassMemberName + " = new " + stInputClassName + "(); }");
                        Losa losaBufferMethods = LosaBufferMethods(stInputClassMemberName, GenU.ibufferRaytracer);
                        Losa losaGlobals = LosaGlobals(raytracer.global());
                        Losa losaTracer = VisitTracer(raytracer.tracer());
                        return losaInputClass + losaStructMemberAndBuffer + losaConstructor + losaBufferMethods + LneNew() + losaGlobals + losaTracer;
                    },
                    stRaytracerClassName,
                    "Raytracer"));
            return losaUsings + losaClass;
        }

        public override Losa VisitTracer(FPLParser.TracerContext tracer)
        {
            return LneNew("public override Vector4d RgbaTrace(Vector2d pos)") + VisitBlock(tracer.block());
        }

        public override Losa VisitFractal(FPLParser.FractalContext fractal)
        {
            Losa losaFractal = LosaUsings();
            losaFractal += LosaInNamespace(() =>
            {
                return LosaInClass(() =>
                {
                    Losa losaClassBody = LosaClassBodyInputsExtras(fractal.identifier(), fractal.inputs());

                    losaClassBody += LosaGlobals(fractal.global());

                    losaClassBody += VisitDistanceEstimator(fractal.distanceEstimator());
                    return losaClassBody;
                },
                StNameFromIdentifier(fractal.identifier()),
                "Fractal3d");
            });
            return losaFractal;
        }

        private string StStructName(string stFractalName)
        {
            return "_" + stFractalName;
        }

        private string StStructMemberName(string stFractalName)
        {
            return StStructName(stFractalName).ToLower();
        }

        private Losa LosaInputDecl(FPLParser.InputContext input)
        {
            return VisitInputType(input.inputType()) + (input.arrayDecl() != null ? "[] " : " ") +
                   VisitIdentifier(input.identifier());
        }

        private Losa LosaDefaultValueForInput(FPLParser.InputContext input)
        {
            FPLParser.ArrayDeclContext arrayDecl = input.arrayDecl();
            if (arrayDecl != null)
            {
                Ipt ipt = _fplPreProcessor.mpinput_ipt[input];
                return "new " + LosaPaddedArray(input) + "(rgbyte, " + ipt.ibyteOffset.ToString() + ", " + VisitExpr(arrayDecl.expr()) + ");";
            }

            return (input.literal() == null
                       ? "default(" + VisitInputType(input.inputType()) + ")"
                       : VisitLiteral(input.literal())) + ";";
        }

        private Losa LosaDefaultAssignment(FPLParser.InputContext input)
        {
            return VisitIdentifier(input.identifier()) + " = " + LosaDefaultValueForInput(input);
        }

        private Losa LosaPaddedArray(FPLParser.InputContext input)
            => "PaddedArray<" + VisitInputType(input.inputType()) + ">";

        private Losa LosaInputFieldSingle(FPLParser.InputContext input)
        {
            Ipt ipt = _fplPreProcessor.mpinput_ipt[input];
            Losa losaField =
                LneNew("public ") + LosaInputDecl(input) + LneNew("{");
            using (idtrCur.New())
            {
                losaField += LneNew("get { return ") + LosaPaddedArray(input) + ".ValFromRgbyte(rgbyte, " + ipt.ibyteOffset.ToString() + "); } ";
                losaField += LneNew("set { ") + LosaPaddedArray(input) + ".SetVal(rgbyte, " + ipt.ibyteOffset.ToString() + ", value); } ";
            }
            losaField += LneNew("}");
            return losaField;
        }

        private Losa LosaInputFieldArray(FPLParser.InputContext input)
        {
            Losa losaField = LneNew("public readonly ") + LosaPaddedArray(input) + " " + VisitIdentifier(input.identifier()) + ";";
            return losaField;
        }

        private Losa LosaInputClass(string stProgName, FPLParser.InputsContext inputs, out string stClassName, out string stClassMemberName)
        {
            FPLParser.InputContext[] rginput = inputs.input();

            stClassName = StStructName(stProgName);
            stClassMemberName = StStructMemberName(stProgName);
            Losa losaInputClass =
                LneNew("public partial class " + stClassName) +
                LneNew("{");
            using (idtrCur.New())
            {
                Losa losaRgbyte = LneNew("internal readonly byte[] rgbyte = new byte[" + _fplPreProcessor.cbyteInputsTotal + "];");

                losaInputClass += losaRgbyte;

                foreach (FPLParser.InputContext input in rginput)
                {

                    if (input.arrayDecl() != null)
                        losaInputClass += LosaInputFieldArray(input);
                    else
                        losaInputClass += LosaInputFieldSingle(input);
                }

                Losa losaConstructorEmpty = LneNew("public " + stClassName + "()");
                losaConstructorEmpty += LneNew("{");
                using (idtrCur.New())
                {
                    foreach (FPLParser.InputContext input in rginput)
                        losaConstructorEmpty += LneNew() + LosaDefaultAssignment(input);
                }
                losaConstructorEmpty += LneNew("}");
                losaInputClass += losaConstructorEmpty;

                Losa losaStructConstructorVerbose = LneNew("public " + stClassName + "(");
                for (int iinput = 0; iinput < rginput.Length; iinput++)
                {
                    FPLParser.InputContext input = rginput[iinput];
                    losaStructConstructorVerbose += LosaInputDecl(input);
                    if (iinput < rginput.Length - 1)
                        losaStructConstructorVerbose += ", ";
                }
                losaStructConstructorVerbose += ")";
                losaStructConstructorVerbose += LneNew("{");
                using (idtrCur.New())
                {
                    foreach (FPLParser.InputContext input in rginput)
                    {
                        if (input.arrayDecl() != null)
                        {
                            losaStructConstructorVerbose += LneNew("this.") + LosaDefaultAssignment(input);
                            losaStructConstructorVerbose += LneNew("this.") + VisitIdentifier(input.identifier()) +
                                                            ".CopyValues(" + VisitIdentifier(input.identifier()) +
                                                            ", 0);";
                        }
                        else
                        {
                            losaStructConstructorVerbose += LneNew() + "this." + VisitIdentifier(input.identifier()) +
                                                            " = " + VisitIdentifier(input.identifier()) + ";";
                        }
                    }
                }
                losaStructConstructorVerbose += LneNew("}");
                losaInputClass += losaStructConstructorVerbose;
            }
            losaInputClass += LneNew("}");

            return losaInputClass;
        }

        private Losa LosaClassMemberAndBuffer(string stClassName, string stClassMemberName)
        {
            Losa losaClassMemberAndBuffer =
                LneNew("public readonly " + stClassName + " " + stClassMemberName + ";") +
                LneNew("private SharpDX.Direct3D11.Buffer buffer;");
            return losaClassMemberAndBuffer;
        }

        private Losa LosaBufferMethods(string stStructMemberName, int ibuffer)
        {
            Losa losaInitializeBuffer =
                LneNew("protected override void InitializeBuffer(Device device, DeviceContext deviceContext)") +
                LneNew("{");
            using (idtrCur.New())
            {
                losaInitializeBuffer +=
                    LneNew("buffer = U.BufferCreate(device, deviceContext, " + ibuffer + ", " + stStructMemberName + ".rgbyte);");
            }
            losaInitializeBuffer += LneNew("}");
            Losa losaBufferMethods = losaInitializeBuffer;

            Losa losaUpdateBuffer =
                LneNew("protected override void UpdateBuffer(Device device, DeviceContext deviceContext)") + LneNew("{");
            using (idtrCur.New())
            {
                losaUpdateBuffer +=
                    LneNew("U.UpdateBuffer(device, deviceContext, buffer, " + stStructMemberName + ".rgbyte);");
            }
            losaUpdateBuffer += LneNew("}");
            losaBufferMethods += losaUpdateBuffer;
            return losaBufferMethods;
        }

        public Losa LosaClassBodyInputsExtras(FPLParser.IdentifierContext identifier, FPLParser.InputsContext inputs)
        {
            FPLParser.InputContext[] rginput = inputs.input();
            if (rginput.Length == 0)
                return "";
            string stFractalName = StNameFromIdentifier(identifier);

            string stInputClassName, stInputClassMemberName;
            Losa losaClassBody = LosaInputClass(stFractalName, inputs, out stInputClassName, out stInputClassMemberName);

            losaClassBody += LosaClassMemberAndBuffer(stInputClassName, stInputClassMemberName);

            Losa losaClassContstructorEmpty = LneNew("public " + stFractalName + "()") +
                                              LneNew("{");
            using (idtrCur.New())
            {
                losaClassContstructorEmpty += LneNew("this." + stInputClassMemberName + " = new " + stInputClassName + "();");
            }
            losaClassContstructorEmpty += LneNew("}");
            losaClassBody += losaClassContstructorEmpty;

            Losa losaClassConstructorVerbose = LneNew("public " + stFractalName + "(" + stInputClassName + " " + stInputClassMemberName + ")")
                + LneNew("{");
            using (idtrCur.New())
            {
                losaClassConstructorVerbose += LneNew("this." + stInputClassMemberName + " = " + stInputClassMemberName + ";");
            }
            losaClassConstructorVerbose += LneNew("}");
            losaClassBody += losaClassConstructorVerbose;

            losaClassBody += LosaBufferMethods(stInputClassMemberName, GenU.ibufferFractal);

            Losa losaReset =
                LneNew("public override void ResetInputs()") + LneNew("{");
            using (idtrCur.New())
            {
                foreach (FPLParser.InputContext input in rginput)
                {
                    if (input.literal() != null)
                        losaReset += LneNew(stInputClassMemberName + ".") + LosaDefaultAssignment(input);
                }
            }
            losaReset += LneNew("}");
            losaClassBody += losaReset;


            Dictionary<string, List<FPLParser.InputContext>> mpstType_rginput = new Dictionary<string, List<FPLParser.InputContext>>(rginput.Length);
            foreach (FPLParser.InputContext input in rginput)
            {
                if (input.arrayDecl() != null)
                    continue;
                FPLParser.InputTypeContext inputType = input.inputType();
                string stType = VisitInputType(inputType).ToStringFollowing();
                List<FPLParser.InputContext> rginputOfType;
                if (!mpstType_rginput.TryGetValue(stType, out rginputOfType))
                    mpstType_rginput[stType] = rginputOfType = new List<FPLParser.InputContext>();
                rginputOfType.Add(input);
            }

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
                        losaInputGet += LneNew("if (iinput == " + iinput + ") return " + stInputClassMemberName + ".") + VisitIdentifier(input.identifier()) + ";";
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
                        losaInputSet += LneNew("if (iinput == " + iinput + ") " + stInputClassMemberName + ".") + VisitIdentifier(input.identifier()) + " = val;";
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

        public override Losa VisitDistanceEstimator(FPLParser.DistanceEstimatorContext distanceEstimator)
        {
            Losa losa = LneNew("protected internal override double DuDeFractal(Vector3d pos)") + VisitBlock(distanceEstimator.block());
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
            if (_fplPreProcessor.mpstFunc_rgargmod.TryGetValue(stFunc, out rgargmod)
                && rgargmod[iexpr] != null)
                losaExpr = VisitArgMod(rgargmod[iexpr]) + " " + losaExpr;
            return losaExpr;
        }

        public override Losa VisitInputAccess(FPLParser.InputAccessContext inputAccess)
        {
            FPLParser.IdentifierContext identifier = inputAccess.identifier();
            string stIdentifier = FPLPreProcessor.StFromIdentifier(identifier);
            FPLParser.InputContext input;
            if (_fplPreProcessor.mpstIdentifier_input.TryGetValue(stIdentifier, out input))
                return LosaUseInputIdentifier(input);
            Error("Unknown input " + identifier.GetText());
            throw new NotImplementedException();
        }

        public override Losa VisitFractalAccess(FPLParser.FractalAccessContext fractalAccess)
        {
            return "fractal." + Visit(fractalAccess.children[fractalAccess.ChildCount - 1]);
        }

        private Losa LosaUseInputIdentifier(FPLParser.InputContext input)
        {
            Losa losaAccess = StStructMemberName(StNameFromProg((FPLParser.ProgContext) input.Root())) + "." +
                              VisitIdentifier(input.identifier());
            return losaAccess;
        }

        public override Losa VisitLocalDecl(FPLParser.LocalDeclContext localDecl)
        {
            return LosaLocalDecl(localDecl.type(), localDecl.identifier(), localDecl.arrayDecl(), localDecl.expr());
        }

        private Losa LosaLocalDecl(FPLParser.TypeContext type, FPLParser.IdentifierContext identifier, FPLParser.ArrayDeclContext[] rgarrayDecl, FPLParser.ExprContext expr)
        {
            Losa losaLocalDecl = VisitType(type);
            Losa losaIdentifier = VisitIdentifier(identifier);
            if (rgarrayDecl.Length > 0)
            {
                for (int idecl = 0; idecl < rgarrayDecl.Length; idecl++)
                    losaLocalDecl += "[]";
                losaLocalDecl += " " + losaIdentifier + " = new " + VisitType(type);
                foreach (var arrayDecl in rgarrayDecl)
                    losaLocalDecl += "[" + VisitExpr(arrayDecl.expr()) + "]";
            }
            else
            {
                losaLocalDecl += " " + losaIdentifier;
                if (expr != null)
                {
                    losaLocalDecl += " = " + VisitExpr(expr);
                }
            }
            losaLocalDecl += ";";
            return losaLocalDecl;
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

        public override Losa VisitInputType(FPLParser.InputTypeContext inputType)
        {
            return StTypeInput(inputType.type().GetText());
        }

        private string StTypeInput(string stType)
        {
            switch (stType)
            {
                case "v2":
                    return "Vector2f";
                case "v3":
                    return "Vector3f";
                case "v4":
                    return "Vector4f";
                default:
                    return stType;
            }
        }

        public override Losa VisitType(FPLParser.TypeContext type)
        {
            return StTypeNonInput(type.GetText());
        }

        private string StTypeNonInput(string stType)
        {
            switch (stType)
            {
                case "v2":
                    return "Vector2d";
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

        protected override string StExtension() => "cs";
    }
}
