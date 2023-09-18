using System;
namespace ProcessExplorer.components
{
    class DosHeader : PluginInterface.SuperHeader
    {
        public DosHeader(PluginInterface.DataStorage dataStorage) : base("dos header", 19, 3)
        {
            string[] firstHexLine = dataStorage.FilesHex[0, 1].Split(' ');
            if (firstHexLine[0].ToLower() != "4d" || firstHexLine[1].ToLower() != "5a")
            {   // This means this file is not a PE
                FailedToInitlize = true;
                return;
            }

            StartPoint = 0;

            Desc = new string[RowSize];
            Size = new int[RowSize];
            Size[0] = 2; Desc[0] = "e_magic (2 bytes) \"MZ\" signature indiciating this file is a DOS exe.";
            Size[1] = 2; Desc[1] = "e_cblp (2 bytes) number of bytes on the last page.";
            Size[2] = 2; Desc[2] = "e_cp (2 bytes) total number of pages in the file.";
            Size[3] = 2; Desc[3] = "e_crlc (2 bytes) number of relocations.";
            Size[4] = 2; Desc[4] = "e_cparhdr (2 bytes) number of paragraphs in the header.";
            Size[5] = 2; Desc[5] = "e_minalloc: (2 bytes) minimum number of paragraphs (16 bytes per para).";
            Size[6] = 2; Desc[6] = "e_maxalloc (2 bytes) maximum number of paragraphs (16 bytes per para).";
            Size[7] = 2; Desc[7] = "e_ss (2 bytes) inital value of the stack segment register.";
            Size[8] = 2; Desc[8] = "e_sp (2 bytes) inital value of the stack pointer register.";
            Size[9] = 2; Desc[9] = "e_csum (2 bytes) check sum (verifys programs size in memory).";
            Size[10] = 2; Desc[10] = "e_ip (2 bytes) initial value of the instruction pointer register.";
            Size[11] = 2; Desc[11] = "e_cs (2 bytes) initial value of the code segment register.";
            Size[12] = 2; Desc[12] = "e_lfarlc (2 bytes) stores offset to the relocation table.";
            Size[13] = 2; Desc[13] = "e_ovno (2 bytes) overlay number (allows code swapping in mem).";
            Size[14] = 8; Desc[14] = "e_res (8 bytes) reserved fields.";
            Size[15] = 2; Desc[15] = "e_oemid: (2 bytes) manufacturer or distributor info.";
            Size[16] = 2; Desc[16] = "e_oeminfo (2 bytes) OEM info or version numbers";
            Size[17] = 20; Desc[17] = "e_res2: (20 bytes) reserved fields";
            Size[18] = 4; Desc[18] = "e_lfanew: (4 bytes) offset to the start of the PE Header.";
            
            SetEndPoint();
        }

        public override void OpenForm(int row, PluginInterface.DataStorage dataStorage)
        {
            return; // No custom forms required here
        }

    }
}
