using System;
using Antlr4.Runtime.Tree;
using FPL;

namespace CodeGen
{
    internal abstract class FPLToCLL : FPLTranspilerBase
    {
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
            return VisitType(localDecl.type()) + " " + VisitIdentifier(localDecl.identifier()) + " = " + VisitExpr(localDecl.expr()) + ";";
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

        public override Losa VisitInputType(FPLParser.InputTypeContext inputType)
        {
            return inputType.GetText();
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
