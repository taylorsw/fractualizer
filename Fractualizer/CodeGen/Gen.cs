using System;
using System.Diagnostics;
using System.IO;
using Antlr4.Runtime;
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
                if (Path.GetExtension(stFile) != ".frac")
                    continue;

                AntlrFileStream afs = new AntlrFileStream(stFile);
                FPLParser.ProgContext prog = FPLTranspilerBase.ProgFromAntlrInputStream(afs);

                FPLTranspilerBase.Validate(prog);

                FPLToHLSL fplToHlsl = new FPLToHLSL();
                fplToHlsl.GenFile(prog, stDirectoryOut);

                FPLToCS fpltoCs = new FPLToCS();
                fpltoCs.GenFile(prog, stDirectoryOut);
            }

            return 0;
        }
    }
}
