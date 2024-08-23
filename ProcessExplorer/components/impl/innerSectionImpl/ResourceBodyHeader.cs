using PluginInterface;
using System;
using static PluginInterface.Enums;

namespace ProcessExplorer.components.impl.innerSectionImpl
{
    public class ResourceBodyHeader : SuperHeader
    {
        public int BodySize { get; private set; }
        public int RVA { get; private set; }

        public ResourceBodyHeader(uint startingPoint, string name, DataStorage dataStorage) : base(name, 4, 3)
        {
            StartPoint = startingPoint;

            Desc = new string[RowSize];
            Size = new int[RowSize];
            Size[0] = 4; Desc[0] = "Data RVA (4 bytes) offset from module base in virtual memory.";
            Size[1] = 4; Desc[1] = "Size (4 bytes) the size of the resource data in bytes.";
            Size[2] = 2; Desc[2] = "CodePage (4 bytes) primary version number.";
            Size[3] = 2; Desc[3] = "Reserved (4 bytes) must be zero.";

            BodySize = int.Parse(GetData(1, 1, DataType.DECIMAL, 2, true, dataStorage));
            RVA = int.Parse(GetData(0, 1, DataType.DECIMAL, 2, true, dataStorage));

            Console.WriteLine($"ResourceBodyHeader:{name} BodySize:{BodySize} RVA:{RVA}");
            SetEndPoint();
        }

        public override void OpenForm(int row, DataStorage dataStorage)
        {
            return;
        }
    }
}
