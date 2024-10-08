﻿using System;
using PluginInterface;

namespace ProcessExplorer.components.impl
{
    public class SectionHeader : SuperHeader
    {
        public uint BodyStartPoint { get; private set; }
        public uint BodyEndPoint { get; private set; }

        public int VirtualAddress { get; private set; }

        public SectionHeader(DataStorage dataStorage, uint startingPoint, string sectionType) : base("section header", 10, 3)
        {
            StartPoint = startingPoint;
            Component = sectionType;

            Desc = new string[RowSize];
            Size = new int[RowSize];
            Size[0] = 8; Desc[0] = "Name (8 bytes) name of this section.";
            Size[1] = 4; Desc[1] = "Virtual Size (4 bytes) size of the section in memory.";
            Size[2] = 4; Desc[2] = "Virtual Address (4 bytes) relative virtual address of this section in memory.";
            Size[3] = 4; Desc[3] = "Size of Raw Data (4 bytes) size of the section on disk.";
            Size[4] = 4; Desc[4] = "Pointer to Raw Data (4 bytes) file offset to the begining of the section.";
            Size[5] = 4; Desc[5] = "Pointer to Relocations (4 bytes) file offset to the relocations entires for this section.";
            Size[6] = 4; Desc[6] = "Pointer to Line Numbers (4 bytes) file offset to the line number entires (used for debugging).";
            Size[7] = 2; Desc[7] = "Number of Relocations (2 bytes) number of relocation entires associated with this section.";
            Size[8] = 2; Desc[8] = "Number of Line Numbers (2 bytes) The number of line number entires.";
            Size[9] = 4; Desc[9] = "Characteristics (4 bytes) defines this sections properties.";

            SetEndPoint();

            VirtualAddress = int.Parse(GetData(2, 1, Enums.DataType.DECIMAL, 2, true, dataStorage));
            BodyStartPoint = Convert.ToUInt32(GetData(4, 1, Enums.DataType.DECIMAL, 2, true, dataStorage));
            BodyEndPoint = Convert.ToUInt32(GetData(3, 1, Enums.DataType.DECIMAL, 2, true, dataStorage)) + BodyStartPoint;

            Console.WriteLine($"Section Header:{sectionType} BodyStart:{BodyStartPoint} BodyEndPoint:{BodyEndPoint} VA:{VirtualAddress} Start2:{GetData(4, 1, Enums.DataType.DECIMAL, 2, true, dataStorage)}" +
                $" Size:{GetData(3, 1, Enums.DataType.DECIMAL, 2, true, dataStorage)}");
        }

        public override void OpenForm(int row, DataStorage dataStorage)
        {
            return;
        }
    }
}
