using System;

namespace ProcessExplorer.components.impl
{
    class OptionalPeHeader32 : SuperHeader
    {
        public OptionalPeHeader32(ProcessHandler processHandler, int startingPoint) : base(processHandler, 22, 3, true)
        {
            string[,] sizeAndDesc = new string[22, 2];
            sizeAndDesc[0, 0] = "4"; sizeAndDesc[0, 1] = "BaseOfData (4 bytes) ";
            sizeAndDesc[1, 0] = "4"; sizeAndDesc[1, 1] = "ImageBase (4 bytes) ";
            sizeAndDesc[2, 0] = "4"; sizeAndDesc[2, 1] = "SectionAlignment (4 bytes) ";
            sizeAndDesc[3, 0] = "4"; sizeAndDesc[3, 1] = "FileAlignment (4 bytes) ";
            sizeAndDesc[4, 0] = "2"; sizeAndDesc[4, 1] = "MajorOperatingSystemVersion (2 bytes) ";
            sizeAndDesc[5, 0] = "2"; sizeAndDesc[5, 1] = "MinorOperatingSystemVersion (2 bytes) ";
            sizeAndDesc[6, 0] = "2"; sizeAndDesc[6, 1] = "MajorImageVersion (2 bytes) ";
            sizeAndDesc[7, 0] = "2"; sizeAndDesc[7, 1] = "MinorImageVersion (2 bytes) ";
            sizeAndDesc[8, 0] = "2"; sizeAndDesc[8, 1] = "MajorSubsystemVersion (2 bytes) ";
            sizeAndDesc[9, 0] = "2"; sizeAndDesc[9, 1] = "MinorSubsystemVersion (2 bytes) ";
            sizeAndDesc[10, 0] = "4"; sizeAndDesc[10, 1] = "Win32VersionValue (4 bytes) ";
            sizeAndDesc[11, 0] = "4"; sizeAndDesc[11, 1] = "SizeOfImage (4 bytes) ";
            sizeAndDesc[12, 0] = "4"; sizeAndDesc[12, 1] = "SizeOfHeaders (4 bytes) ";
            sizeAndDesc[13, 0] = "4"; sizeAndDesc[13, 1] = "CheckSum (4 bytes) ";
            sizeAndDesc[14, 0] = "2"; sizeAndDesc[14, 1] = "Subsystem (2 bytes) ";
            sizeAndDesc[15, 0] = "2"; sizeAndDesc[15, 1] = "DllCharacteristics (2 bytes) ";
            sizeAndDesc[16, 0] = "4"; sizeAndDesc[16, 1] = "SizeOfStackReserve (4 bytes) ";
            sizeAndDesc[17, 0] = "4"; sizeAndDesc[17, 1] = "SizeOfStackCommit (4 bytes) ";
            sizeAndDesc[18, 0] = "4"; sizeAndDesc[18, 1] = "SizeOfHeapReserve (4 bytes) ";
            sizeAndDesc[19, 0] = "4"; sizeAndDesc[19, 1] = "SizeOfHeapCommit (4 bytes) ";
            sizeAndDesc[20, 0] = "4"; sizeAndDesc[20, 1] = "LoaderFlags (4 bytes) ";
            sizeAndDesc[21, 0] = "4"; sizeAndDesc[21, 1] = "NumberOfRvaAndSizes (4 bytes) ";

            StartPoint = startingPoint; // Must set the start point before populating the arrays
            Console.WriteLine("Optional PeHeader 32 StartPoint:" + startingPoint);
            populateArrays(sizeAndDesc);
        }

        public override void OpenForm(int row)
        {
            return;
        }
    }
}
