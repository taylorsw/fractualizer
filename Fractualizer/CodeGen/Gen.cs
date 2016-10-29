//#define VALIDATE_GEN
//#define DEBUG_GEN

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using FPL;

namespace CodeGen
{
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
#if DEBUG_GEN
            Debugger.Launch();
#endif
                if (Path.GetExtension(stFile) != ".frac")
                    continue;

                AntlrFileStream afs = new AntlrFileStream(stFile);
                FPLParser.FractalContext fractal = FPLTranspilerBase.FractalFromAntlrInputStream(afs);

                FPLToHLSL fplToHlsl = new FPLToHLSL();
                fplToHlsl.GenFile(fractal, stDirectoryOut);
                
                FPLToCS fpltoCs = new FPLToCS();
                fpltoCs.GenFile(fractal, stDirectoryOut);
            }

            return 0;
        }
    }
}
