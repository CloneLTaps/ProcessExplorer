using System;

namespace ProcessExplorer.components
{
    class DosHeader : SuperHeader
    {
        public DosHeader(ProcessHandler processHandler) : base(processHandler, 19, 3, true)
        {
            string[,] sizeAndDesc = new string[19, 2];
            sizeAndDesc[0, 0] = "2"; sizeAndDesc[0, 1] = "e_magic (2 bytes) \"MZ\" signature indiciating this file is a DOS exe.";
            sizeAndDesc[1, 0] = "2"; sizeAndDesc[1, 1] = "e_cblp (2 bytes) number of bytes on the last page.";
            sizeAndDesc[2, 0] = "2"; sizeAndDesc[2, 1] = "e_cp (2 bytes) total number of pages in the file.";
            sizeAndDesc[3, 0] = "2"; sizeAndDesc[3, 1] = "e_crlc (2 bytes) number of relocations.";
            sizeAndDesc[4, 0] = "2"; sizeAndDesc[4, 1] = "e_cparhdr (2 bytes) number of paragraphs in the header.";
            sizeAndDesc[5, 0] = "2"; sizeAndDesc[5, 1] = "e_minalloc: (2 bytes) minimum number of paragraphs (16 bytes per para).";
            sizeAndDesc[6, 0] = "2"; sizeAndDesc[6, 1] = "e_maxalloc (2 bytes) maximum number of paragraphs (16 bytes per para).";
            sizeAndDesc[7, 0] = "2"; sizeAndDesc[7, 1] = "e_ss (2 bytes) inital value of the stack segment register.";
            sizeAndDesc[8, 0] = "2"; sizeAndDesc[8, 1] = "e_sp (2 bytes) inital value of the stack pointer register.";
            sizeAndDesc[9, 0] = "2"; sizeAndDesc[9, 1] = "e_csum: (2 bytes) check sum (verifys programs size in memory).";
            sizeAndDesc[10, 0] = "2"; sizeAndDesc[10, 1] = "e_ip: (2 bytes) initial value of the instruction pointer register.";
            sizeAndDesc[11, 0] = "2"; sizeAndDesc[11, 1] = "e_cs: (2 bytes) initial value of the code segment register.";
            sizeAndDesc[12, 0] = "2"; sizeAndDesc[12, 1] = "e_lfarlc: (2 bytes) stores offset to the relocation table.";
            sizeAndDesc[13, 0] = "2"; sizeAndDesc[13, 1] = "e_ovno: (2 bytes) overlay number (allows code swapping in mem).";
            sizeAndDesc[14, 0] = "8"; sizeAndDesc[14, 1] = "e_res: (8 bytes) reserved fields.";
            sizeAndDesc[15, 0] = "2"; sizeAndDesc[15, 1] = "e_oemid: (2 bytes) manufacturer or distributor info.";
            sizeAndDesc[16, 0] = "2"; sizeAndDesc[16, 1] = "e_oeminfo: (2 bytes) OEM info or version numbers";
            sizeAndDesc[17, 0] = "20"; sizeAndDesc[17, 1] = "e_res2: (20 bytes) reserved fields";
            sizeAndDesc[18, 0] = "4"; sizeAndDesc[18, 1] = "e_lfanew: (4 bytes) offset to the start of the PE Header.";

            StartPoint = 0; // Must set the start point before populating the arrays
            populateArrays(sizeAndDesc);
            Console.WriteLine("PeHeaderStart:" + hexArray[RowSize - 1, 0]);
        }

    }
}
