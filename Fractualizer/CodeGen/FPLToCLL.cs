using System;
using System.Diagnostics;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using FPL;

namespace CodeGen
{
    public static class TranspilerExtensions
    {
        public static RuleContext Root(this RuleContext context)
        {
            RuleContext node = context;
            while (node.Parent != null)
                node = node.Parent;
            return node;
        }
    }

    internal abstract class FPLToCLL : FPLTranspilerBase
    {
        public override Losa VisitProg(FPLParser.ProgContext context)
        {
            Losa losaPdefines = VisitPdefines(context.pdefines());
            Losa losaMain = Visit(context.children[context.ChildCount - 1]);
            return losaPdefines + losaMain;
        }

        public override Losa VisitPdefines(FPLParser.PdefinesContext pdefines)
        {
            Losa losaPdefines = "";
            foreach (FPLParser.PdefineContext pdefine in pdefines.pdefine())
                losaPdefines += VisitPdefine(pdefine) + LneNew();
            return losaPdefines;
        }

        public sealed override Losa VisitPdefine(FPLParser.PdefineContext pdefine)
        {
            return "#define " + VisitIdentifier(pdefine.identifier());
        }

        public sealed override Losa VisitDefCond(FPLParser.DefCondContext defCond)
        {
            using (idtrCur.NewZero())
            {
                Losa losaDefCond = LneNew() + Visit(defCond.children[0]) + " ";
                if (defCond.identifier() != null)
                    losaDefCond += VisitIdentifier(defCond.identifier());
                return losaDefCond;
            }
        }

        protected int RoundToByteOffset(int cybte, int cbyteOffset = 16)
        {
            return cybte + (cbyteOffset - cybte%cbyteOffset)%cbyteOffset;
        }

        protected int SizeOf(FPLParser.InputContext input)
        {
            FPLParser.InputTypeContext inputType = input.inputType();
            FPLParser.TypeContext type = inputType.type();
            int cbyte;
            if (type.BoolType() != null)
                cbyte = 1;
            else if (type.FloatType() != null || type.IntType() != null)
                cbyte = 4;
            else
            {
                switch (inputType.type().GetText())
                {
                    case "v2":
                        cbyte = 2 * 4;
                        break;
                    case "v3":
                        cbyte = 3 * 4;
                        break;
                    case "v4":
                        cbyte = 4 * 4;
                        break;
                    default:
                        Error("Size of type " + inputType.type().GetText() + " not defined.");
                        throw new NotImplementedException();
                }
            }
            FPLParser.ArrayDeclContext arrayDecl = input.arrayDecl();
            if (arrayDecl != null)
            {
                try
                {
                    int size = int.Parse(arrayDecl.expr().literal().literalInt().GetText());
                    cbyte *= size;
                }
                catch (Exception)
                {
                    Error("Input array size must be an explicit integer.");
                    throw;
                }
            }
            return cbyte;
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
                losaArglist += VisitArg(arg);
                if (iarg != rgarg.Length - 1)
                    losaArglist = losaArglist + ", ";
            }
            return losaArglist;
        }

        public override Losa VisitArg(FPLParser.ArgContext arg)
        {
            Losa losaArg = VisitType(arg.type()) + " " + VisitIdentifier(arg.identifier());
            if (arg.argMod() != null)
                losaArg = VisitArgMod(arg.argMod()) + " " + losaArg;
            return losaArg;
        }

        public override Losa VisitRetType(FPLParser.RetTypeContext retType)
        {
            FPLParser.TypeContext type = retType.type();
            if (type != null)
                return VisitType(type);
            return "void";
        }

        public override Losa VisitArgMod(FPLParser.ArgModContext context)
        {
            throw new NotImplementedException();
        }

        public override Losa VisitBlock(FPLParser.BlockContext block)
        {
            Losa losaBlock = LneNew("{");
            using (idtrCur.New())
            {
                foreach (FPLParser.BlockStatContext blockStat in block.blockStat())
                {
                    Losa losaBlockStat = VisitBlockStat(blockStat);
                    losaBlock = losaBlock + (
                        losaBlockStat is Lne
                            ? losaBlockStat
                            : LneNew() + losaBlockStat);
                }
            }
            losaBlock = losaBlock + LneNew("}");
            return losaBlock;
        }

        public override Losa VisitStat(FPLParser.StatContext stat)
        {
            Losa losaStat = base.VisitStat(stat);
            if (stat.expr() != null || stat.keywordExpr() != null)
                losaStat += ";";
            return losaStat;
        }

        public override Losa VisitIfStat(FPLParser.IfStatContext ifStat)
        {
            Losa losaIfStat = LneNew() + "if " + VisitParExpr(ifStat.parExpr());
            var rgstat = ifStat.stat();
            for (int istat = 0; istat < rgstat.Length; istat++)
            {
                FPLParser.StatContext stat = rgstat[istat];
                Losa losaStat = VisitStat(stat);
                if (!(losaStat is Lne))
                    using (idtrCur.New())
                        losaStat = LneNew() + losaStat;

                losaIfStat = istat == 0
                    ? losaIfStat + losaStat
                    : losaIfStat + ("else " + losaStat);
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
                Losa losaExpr;
                FPLParser.FuncCallContext funcCall = exprList.parent as FPLParser.FuncCallContext;
                if (funcCall != null)
                {
                    losaExpr = VisitFuncCallExpr(funcCall, rgexpr[iexpr], iexpr);
                }
                else
                {
                    losaExpr = VisitExpr(rgexpr[iexpr]);
                }
                losaExprList += iexpr == 0
                    ? losaExpr
                    : ", " + losaExpr;
            }
            return losaExprList;
        }

        public virtual Losa VisitFuncCallExpr(FPLParser.FuncCallContext funcCall, FPLParser.ExprContext expr, int iexpr)
        {
            return VisitExpr(expr);
        }

        public override Losa VisitParExpr(FPLParser.ParExprContext parExpr)
        {
            return "(" + VisitExpr(parExpr.expr()) + ")";
        }

        public override Losa VisitTernary(FPLParser.TernaryContext ternary)
        {
            return " ? " + VisitExpr(ternary.expr(0)) + " : " + VisitExpr(ternary.expr(1));
        }

        public override Losa VisitKeywordExpr(FPLParser.KeywordExprContext context)
        {
            if (context.expr() == null)
                return VisitKeywordSingle(context.keywordSingle());
            return VisitKeywordPrefix(context.keywordPrefix()) + " " + VisitExpr(context.expr());
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

        public override Losa VisitBinaryOp(FPLParser.BinaryOpContext binaryOp)
        {
            return binaryOp.GetText();
        }

        public override Losa VisitAssignmentOp(FPLParser.AssignmentOpContext assignmentOp)
        {
            return assignmentOp.GetText();
        }

        public override Losa VisitUnaryOp(FPLParser.UnaryOpContext unaryOp)
        {
            return unaryOp.GetText();
        }

        public override Losa VisitPrefixUnaryOp(FPLParser.PrefixUnaryOpContext prefixUnaryOp)
        {
            return prefixUnaryOp.GetText();
        }

        public override Losa VisitKeywordPrefix(FPLParser.KeywordPrefixContext keywordPrefix)
        {
            return keywordPrefix.GetText();
        }

        public override Losa VisitKeywordSingle(FPLParser.KeywordSingleContext keywordSingle)
        {
            return keywordSingle.GetText();
        }

        public override Losa VisitLiteral(FPLParser.LiteralContext literal)
        {
            return VisitChildren(literal);
        }

        public override Losa VisitLiteralFloat(FPLParser.LiteralFloatContext context)
        {
            return context.GetText();
        }

        public override Losa VisitLiteralInt(FPLParser.LiteralIntContext context)
        {
            return context.GetText();
        }

        public override Losa VisitIdentifier(FPLParser.IdentifierContext identifier)
        {
            return identifier.GetText();
        }

        protected override Losa AggregateResult(Losa aggregate, Losa nextResult)
        {
            // Debug.Assert(aggregate == null || nextResult == null);
            if (aggregate != null)
                return nextResult == null ? aggregate : aggregate + nextResult;
            return nextResult;
        }
    }
}
