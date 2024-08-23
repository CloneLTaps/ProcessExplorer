using PluginInterface;

namespace ProcessExplorer.components.impl.innerSectionImpl
{
    public class ResourceTable : SuperHeader
    {
        public ResourceTable(uint startingPoint, int nameCount, int entryCount, string tableName) : base(tableName, (entryCount * 2), 3)
        {
            StartPoint = startingPoint;

            Desc = new string[RowSize];
            Size = new int[RowSize];

            for(int i=0; i<entryCount * 2; i+=2)
            {   
                Size[i] = 4; // Name entries always come first
                bool isName = i / 2 < nameCount;
                Desc[i] = $"{(isName ? "NamesOffset" : "ID")} (4 bytes) {(isName ? "relative offset to the names location." : "")}";

                Size[i + 1] = 4;
                Desc[i + 1] = "DataEntry or SubdirectoryOffset (4 bytes) dataEntries high bit won't be set.";
            }
            SetEndPoint();
        }

        public override void OpenForm(int row, DataStorage dataStorage)
        {
            return;
        }
    }
}
