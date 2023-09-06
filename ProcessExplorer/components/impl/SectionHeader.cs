using System;

namespace ProcessExplorer.components.impl
{
    class SectionHeader : SuperHeader, ISection
    {
        private readonly SectionTypes sectionType;
        public readonly int bodyStartPoint, bodyEndPoint;

        public SectionHeader(ProcessHandler processHandler, int startingPoint, SectionTypes sectionType) : base(processHandler, 10, 3, true)
        {
            string[,] sizeAndDesc = new string[10, 2];
            sizeAndDesc[0, 0] = "8"; sizeAndDesc[0, 1] = "Name (8 bytes) name of this section.";
            sizeAndDesc[1, 0] = "4"; sizeAndDesc[1, 1] = "Virtual Size (4 bytes) size of the section in memory.";
            sizeAndDesc[2, 0] = "4"; sizeAndDesc[2, 1] = "Virtual Address (4 byts) relative virtual address of this section in memory.";
            sizeAndDesc[3, 0] = "4"; sizeAndDesc[3, 1] = "Size of Raw Data (4 bytes) size of the section on disk.";
            sizeAndDesc[4, 0] = "4"; sizeAndDesc[4, 1] = "Pointer to Raw Data (4 bytes) file offset to the begining of the section.";
            sizeAndDesc[5, 0] = "4"; sizeAndDesc[5, 1] = "Pointer to Relocations (4 bytes) file offset to the relocations entires for this section.";
            sizeAndDesc[6, 0] = "4"; sizeAndDesc[6, 1] = "Pointer to Line Numbers (4 bytes) file offset to the line number entires (used for debugging).";
            sizeAndDesc[7, 0] = "2"; sizeAndDesc[7, 1] = "Number of Relocations (2 bytes) number of relocation entires associated with this section.";
            sizeAndDesc[8, 0] = "2"; sizeAndDesc[8, 1] = "Number of Line Numbers (2 bytes) The number of line number entires.";
            sizeAndDesc[9, 0] = "4"; sizeAndDesc[9, 1] = "Characteristics (4 bytes) defines this sections properties.";

            StartPoint = startingPoint;
            this.sectionType = sectionType;

            populateArrays(sizeAndDesc);

            bodyStartPoint = Convert.ToInt32(OptionsForm.GetBigEndianValue(hexArray[4, 1]), 16);
            bodyEndPoint = Convert.ToInt32(OptionsForm.GetBigEndianValue(hexArray[3, 1]), 16) + bodyStartPoint;
        }

        public SectionTypes GetSectionType()
        {
            return sectionType;
        }

        public override void OpenForm(int row)
        {
            return;
        }
    }
}
