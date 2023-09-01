using System;
using System.Globalization;

namespace ProcessExplorer.components.impl
{

    class OptionalPeHeader : SuperHeader
    {
        public bool validHeader, peThirtyTwoPlus;

        public OptionalPeHeader(ProcessHandler processHandler, int startingPoint) : base(processHandler, 8, 3, true)
        {
            string[,] sizeAndDesc = new string[8, 2];
            sizeAndDesc[0, 0] = "2"; sizeAndDesc[0, 1] = "Magic (2 bytes) specifies PE32 or PE32+ (aka PE64).";
            sizeAndDesc[1, 0] = "1"; sizeAndDesc[1, 1] = "MajorLinkerVersion (1 byte) primary version number.";
            sizeAndDesc[2, 0] = "1"; sizeAndDesc[2, 1] = "MinorLinkerVersion (1 byte) secondary version number.";
            sizeAndDesc[3, 0] = "4"; sizeAndDesc[3, 1] = "SizeOfCode (4 bytes) size of the .text section.";
            sizeAndDesc[4, 0] = "4"; sizeAndDesc[4, 1] = "SizeOfInitializedData (4 bytes) size of initialized .data section.";
            sizeAndDesc[5, 0] = "4"; sizeAndDesc[5, 1] = "SizeOfUninitializedData (4 bytes) size of uninitialized .data section.";
            sizeAndDesc[6, 0] = "4"; sizeAndDesc[6, 1] = "AddressOfEntryPoint (4 bytes) relative virtual address entry point.";
            sizeAndDesc[7, 0] = "4"; sizeAndDesc[7, 1] = "BaseOfCode (4 bytes) points to starting relative virtual address of the .text section.";

            StartPoint = startingPoint; // Must set the start point before populating the arrays
            Console.WriteLine("Optional PeHeader StartPoint:" + startingPoint);
            populateArrays(sizeAndDesc);

            // if the magic bit does not equal 0x10B or 0x20B then this is not a valid optional header
            // if the magic bit is equal to 0x10B then we have a 32 bit header while 0x20B is for 64 bit
            
            string hex = SuperHeader.OptionsForm.GetBigEndianValue(hexArray[0, 1]);
            int magicBit = int.Parse(hex, NumberStyles.HexNumber);
            if (magicBit == 0x10B || magicBit == 0x20B) validHeader = true;
            peThirtyTwoPlus = (magicBit == 0x20B);

            Console.WriteLine("Optional PeHeader EndPoint:" + EndPoint + " \n");
        }

        public override void OpenForm(int row)
        {
            return;
        }
    }
}
