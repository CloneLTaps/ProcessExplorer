using System;
using PluginInterface;

namespace ProcessExplorer.components.impl
{
    class OptionalPeHeaderDataDirectories : SuperHeader
    {
        public uint CertificateTablePointer { get; private set; }
        public uint CertificateTableSize { get; private set; }

        public OptionalPeHeaderDataDirectories(DataStorage dataStorage, uint startingPoint) : base("optional pe header data directories", 30, 3)
        {
            StartPoint = startingPoint;

            Desc = new string[RowSize];
            Size = new int[RowSize];
            Size[0] = 4; Desc[0] = "Export Table Pointer (4 bytes) RVA pointer, describing exported functions and symbols.";
            Size[1] = 4; Desc[1] = "Export Table Size (4 bytes) size of the .edata section.";
            Size[2] = 4; Desc[2] = "Import Table Pointer (4 bytes) RVA pointer, describing importated functions and synbols.";
            Size[3] = 4; Desc[3] = "Import Table Size (4 bytes) size of the .idata section.";
            Size[4] = 4; Desc[4] = "Resource Table Pointer (4 bytes) RVA pointer, describing images, strings, etc.";
            Size[5] = 4; Desc[5] = "Resource Table Size (4 bytse) size of the .rsrc section.";

            Size[6] = 4; Desc[6] = "Exception Table Pointer (4 bytes) RVA pointer, describing exceptions.";
            Size[7] = 4; Desc[7] = "Exception Table Size (4 bytes) size of the .pdata section.";
            Size[8] = 4; Desc[8] = "Certificate Table Pointer (4 bytes) RVA pointer, describing the securtity certificate.";
            Size[9] = 4; Desc[9] = "Certificate Table Size (4 bytes) size of the certificate table.";
            Size[10] = 4; Desc[10] = "Base Relocation Table Pointer (4 bytes) RVA pointer, describing address relocations.";
            Size[11] = 4; Desc[11] = "Base Relocation Table Size (4 bytes) size of the .reloc section.";
            Size[12] = 4; Desc[12] = "Debug Pointer (4 bytes) RVA pointer, describing debugging information.";
            Size[13] = 4; Desc[13] = "Debug Size (4 bytes) size of the .debug section.";
            Size[14] = 8; Desc[14] = "Architecture (8 bytes) reserved, must be 0.";

            Size[15] = 4; Desc[15] = "Global Ptr Pointer (4 bytes) RVA pointer, describing global data and functions (not used today).";
            Size[16] = 4; Desc[16] = "Global Ptr Size (4 bytes) size ia always set to 0.";
            Size[17] = 4; Desc[17] = "TLS Table Pointer (4 bytes) RVA Pointer, describing thread specific data used in multi-threading.";
            Size[18] = 4; Desc[18] = "TLS Table Size (4 bytes) size of the .tls section.";
            Size[19] = 4; Desc[19] = "Load Config Table Pointer (4 bytes) RVA pointer, describing extra config details.";
            Size[20] = 4; Desc[20] = "Load Config Table Size (4 bytes) size of the config table.";
            Size[21] = 4; Desc[21] = "Bound Import Pointer (8 bytes) RVA pointer, describing optimized dll function calls.";
            Size[22] = 4; Desc[22] = "Bound Import Size (8 bytes) size of the bound import table.";
            Size[23] = 4; Desc[23] = "IAT (Import Address Table) Pointer (4 bytes) RVA pointer, describing dll function calls.";
            Size[24] = 4; Desc[24] = "IAT (Import Address Table) Size (4 bytes) size of the Import Address Table.";
            Size[25] = 4; Desc[25] = "Delay Import Descriptor Pointer (4 bytes) RVA pointer, allowing delayed loading of dll's.";
            Size[26] = 4; Desc[26] = "Delay Import Descriptor Size (4 bytes) size of the Import Descriptor.";
            Size[27] = 4; Desc[27] = "CLR Runtime Header Pointer (4 bytes) RVA pointer, used as a .NET header.";
            Size[28] = 4; Desc[28] = "CLR Runtime Header Size (4 bytes) size of the .cormeta section.";
            Size[29] = 8; Desc[29] = "Reserved (8 bytes) reserved for future use must be 0.";

            SetEndPoint();

            CertificateTablePointer = Convert.ToUInt32(GetData(8, 1, Enums.DataType.HEX, 2, true, dataStorage), 16);
            CertificateTableSize = Convert.ToUInt32(GetData(9, 1, Enums.DataType.HEX, 2, true, dataStorage), 16);
        }

        public override void OpenForm(int row, DataStorage dataStorage)
        {
            return;
        }
    }
}
