using System;

namespace ProcessExplorer.components
{
    class PeHeader : SuperHeader
    {

        public PeHeader(ProcessHandler processHandler, int startingPoint) : base(processHandler, 8, 3, true)
        {
            string[,] sizeAndDesc = new string[8, 2];
            sizeAndDesc[0, 0] = "4"; sizeAndDesc[0, 1] = "Signature (4 bytes) \"PE\0\0\" denotes start of the PE.";
            sizeAndDesc[1, 0] = "2"; sizeAndDesc[1, 1] = "Machine (2 bytes) ";
            sizeAndDesc[2, 0] = "2"; sizeAndDesc[2, 1] = "NumberOfSections (2 bytes) total number of sections.";
            sizeAndDesc[3, 0] = "4"; sizeAndDesc[3, 1] = "TimeDateStamp (4 bytes) ";
            sizeAndDesc[4, 0] = "4"; sizeAndDesc[4, 1] = "PointerToSymbolTable (4 bytes) present in .obj files but not in final build.";
            sizeAndDesc[5, 0] = "4"; sizeAndDesc[5, 1] = "NumberOfSymbols: (4 bytes) ^ examples of symbols are function and variable names.";
            sizeAndDesc[6, 0] = "2"; sizeAndDesc[6, 1] = "SizeOfOptionalHeader (2 bytes) size of the optional header which precedes this.";
            sizeAndDesc[7, 0] = "2"; sizeAndDesc[7, 1] = "Characteristics (2 bytes) ";

            StartPoint = startingPoint; // Must set the start point before populating the arrays
            Console.WriteLine("PeHeader StartPoint:" + startingPoint);
            populateArrays(sizeAndDesc);
            Console.WriteLine("OptionalPeHeaderStart:" + hexArray[RowSize - 1, 0]);
        }

    }
}
