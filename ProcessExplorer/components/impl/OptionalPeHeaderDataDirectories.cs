
namespace ProcessExplorer.components.impl
{
    class OptionalPeHeaderDataDirectories : SuperHeader
    {
        public OptionalPeHeaderDataDirectories(ProcessHandler processHandler, int startingPoint) 
            : base(processHandler, ProcessHandler.ProcessComponent.OPITIONAL_PE_HEADER_DATA_DIRECTORIES, 30, 3, true)
        {
            string[,] sizeAndDesc = new string[30, 2];
            sizeAndDesc[0, 0] = "4"; sizeAndDesc[0, 1] = "Export Table Pointer (4 bytes) RVA pointer, describing exported functions and symbols.";
            sizeAndDesc[1, 0] = "4"; sizeAndDesc[1, 1] = "Export Table Size (4 bytes) size of the .edata section.";
            sizeAndDesc[2, 0] = "4"; sizeAndDesc[2, 1] = "Import Table Pointer (4 bytes) RVA pointer, describing importated functions and synbols.";
            sizeAndDesc[3, 0] = "4"; sizeAndDesc[3, 1] = "Import Table Size (4 bytes) size of the .idata section.";
            sizeAndDesc[4, 0] = "4"; sizeAndDesc[4, 1] = "Resource Table Pointer (4 bytes) RVA pointer, describing images, strings, etc.";
            sizeAndDesc[5, 0] = "4"; sizeAndDesc[5, 1] = "Resource Table Size (4 bytse) size of the .rsrc section.";

            sizeAndDesc[6, 0] = "4"; sizeAndDesc[6, 1] = "Exception Table Pointer (4 bytes) RVA pointer, describing exceptions.";
            sizeAndDesc[7, 0] = "4"; sizeAndDesc[7, 1] = "Exception Table Size (4 bytes) size of the .pdata section.";
            sizeAndDesc[8, 0] = "4"; sizeAndDesc[8, 1] = "Certificate Table Pointer (4 bytes) RVA pointer, describing the securtity certificate.";
            sizeAndDesc[9, 0] = "4"; sizeAndDesc[9, 1] = "Certificate Table Size (4 bytes) size of the certificate table.";
            sizeAndDesc[10, 0] = "4"; sizeAndDesc[10, 1] = "Base Relocation Table Pointer (4 bytes) RVA pointer, describing address relocations.";
            sizeAndDesc[11, 0] = "4"; sizeAndDesc[11, 1] = "Base Relocation Table Size (4 bytes) size of the .reloc section.";
            sizeAndDesc[12, 0] = "4"; sizeAndDesc[12, 1] = "Debug Pointer (4 bytes) RVA pointer, describing debugging information.";
            sizeAndDesc[13, 0] = "4"; sizeAndDesc[13, 1] = "Debug Size (4 bytes) size of the .debug section.";
            sizeAndDesc[14, 0] = "8"; sizeAndDesc[14, 1] = "Architecture (8 bytes) reserved, must be 0.";

            sizeAndDesc[15, 0] = "4"; sizeAndDesc[15, 1] = "Global Ptr Pointer (4 bytes) RVA pointer, describing global data and functions (not used today).";
            sizeAndDesc[16, 0] = "4"; sizeAndDesc[16, 1] = "Global Ptr Size (4 bytes) size ia always set to 0.";
            sizeAndDesc[17, 0] = "4"; sizeAndDesc[17, 1] = "TLS Table Pointer (4 bytes) RVA Pointer, describing thread specific data used in multi-threading.";
            sizeAndDesc[18, 0] = "4"; sizeAndDesc[18, 1] = "TLS Table Size (4 bytes) size of the .tls section.";
            sizeAndDesc[19, 0] = "4"; sizeAndDesc[19, 1] = "Load Config Table Pointer (4 bytes) RVA pointer, describing extra config details.";
            sizeAndDesc[20, 0] = "4"; sizeAndDesc[20, 1] = "Load Config Table Size (4 bytes) size of the config table.";
            sizeAndDesc[21, 0] = "4"; sizeAndDesc[21, 1] = "Bound Import Pointer (8 bytes) RVA pointer, describing optimized dll function calls.";
            sizeAndDesc[22, 0] = "4"; sizeAndDesc[22, 1] = "Bound Import Size (8 bytes) size of the bound import table.";
            sizeAndDesc[23, 0] = "4"; sizeAndDesc[23, 1] = "IAT (Import Address Table) Pointer (4 bytes) RVA pointer, describing dll function calls.";
            sizeAndDesc[24, 0] = "4"; sizeAndDesc[24, 1] = "IAT (Import Address Table) Size (4 bytes) size of the Import Address Table.";
            sizeAndDesc[25, 0] = "4"; sizeAndDesc[25, 1] = "Delay Import Descriptor Pointer (4 bytes) RVA pointer, allowing delayed loading of dll's.";
            sizeAndDesc[26, 0] = "4"; sizeAndDesc[26, 1] = "Delay Import Descriptor Size (4 bytes) size of the Import Descriptor.";
            sizeAndDesc[27, 0] = "4"; sizeAndDesc[27, 1] = "CLR Runtime Header Pointer (4 bytes) RVA pointer, used as a .NET header.";
            sizeAndDesc[28, 0] = "4"; sizeAndDesc[28, 1] = "CLR Runtime Header Size (4 bytes) size of the .cormeta section.";
            sizeAndDesc[29, 0] = "8"; sizeAndDesc[29, 1] = "Reserved (8 bytes) reserved for future use must be 0.";

            StartPoint = startingPoint; // Must set the start point before populating the arrays
            populateArrays(sizeAndDesc);
        }

        public override void OpenForm(int row)
        {
            return;
        }
    }
}
