using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using FPL;

namespace CodeGen
{
    class FPLToHLSL : FPLBaseListener, IDisposable
    {
        private readonly FileStream fs;
        private readonly TextWriter tw;

        public FPLToHLSL(string stFile, string stDirectory)
        {
            string stFileOutput = Path.Combine(stDirectory, Path.GetFileNameWithoutExtension(stFile) + ".hlsl");
            Directory.CreateDirectory(stDirectory);
            fs = File.Create(stFileOutput);
            tw = new StreamWriter(fs);
        }

        public override void EnterFractal(FPLParser.FractalContext context)
        {
            tw.Write("Hello Fractal");
            base.EnterFractal(context);
        }

        public void Dispose()
        {
            tw.Close();
            fs.Close();
        }
    }

    class Gen
    {
        static int Main(string[] args)
        {
            if (args.Length != 2)
                throw new ArgumentException("Did not supply correct parameters.");

            string stDirectoryIn = args[0];
            string stDirectoryOut = args[1];

            foreach (string stFile in Directory.GetFiles(stDirectoryIn))
            {
                if (Path.GetExtension(stFile) != ".frac")
                    continue;

                AntlrFileStream afs = new AntlrFileStream(stFile);
                FPLLexer fplLexer = new FPLLexer(afs);
                CommonTokenStream cts = new CommonTokenStream(fplLexer);
                FPLParser fplParser = new FPLParser(cts);

                FPLParser.FractalContext fractalContext = fplParser.fractal();

                using (FPLToHLSL fplToHlsl = new FPLToHLSL(stFile, stDirectoryOut))
                {
                    ParseTreeWalker.Default.Walk(fplToHlsl, fractalContext);
                }
            }

            return 0;
        }
    }
}
