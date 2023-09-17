using System.Globalization;
using System;

namespace ProcessExplorer.components.impl
{
    class OptionalPeHeader : PluginInterface.SuperHeader
    {
        public bool validHeader, peThirtyTwoPlus;

        public OptionalPeHeader(PluginInterface.DataStorage dataStorage, int startingPoint) : base("optional pe header", 8, 3)
        {
            StartPoint = startingPoint; 

            Desc = new string[RowSize];
            Size = new int[RowSize];
            Size[0] = 2; Desc[0] = "Magic (2 bytes) specifies PE32 or PE32+ (aka PE64).";
            Size[1] = 1; Desc[1] = "MajorLinkerVersion (1 byte) primary version number.";
            Size[2] = 1; Desc[2] = "MinorLinkerVersion (1 byte) secondary version number.";
            Size[3] = 4; Desc[3] = "SizeOfCode (4 bytes) size of the .text section.";
            Size[4] = 4; Desc[4] = "SizeOfInitializedData (4 bytes) size of initialized data inside of the .data section.";
            Size[5] = 4; Desc[5] = "SizeOfUninitializedData (4 bytes) size of uninitialized data inside of the .data section.";
            Size[6] = 4; Desc[6] = "AddressOfEntryPoint (4 bytes) relative virtual address entry point.";
            Size[7] = 4; Desc[7] = "BaseOfCode (4 bytes) points to starting RVA of the .text section.";

            SetEndPoint();

            // if the magic bit does not equal 0x10B or 0x20B then this is not a valid optional header
            // if the magic bit is equal to 0x10B then we have a 32 bit header while 0x20B is for 64 bit
            string hex = GetBigEndianValue(GetData(0, 1, PluginInterface.Enums.DataType.HEX, false, true, dataStorage));
            int magicBit = int.Parse(hex, NumberStyles.HexNumber);
            if (magicBit == 0x10B || magicBit == 0x20B) validHeader = true;
            peThirtyTwoPlus = (magicBit == 0x20B);
        }

        public override void OpenForm(int row, PluginInterface.DataStorage dataStorage)
        {
            return;
        }
    }
}
