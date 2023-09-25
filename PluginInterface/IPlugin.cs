using System;
using System.Collections.Generic;
using static PluginInterface.Enums;

namespace PluginInterface
{
    public interface IPlugin
    {
        /// <summary> This will return true if this plugin supports this file type. </summary>
        public bool Initialized(DataStorage data);

        /// <summary> This is called when data has been edited which means we can now recalculate data such as checksums. </summary>
        public void ReclaculateHeaders(int rowChanged, DataStorage data);
        public Dictionary<string, SuperHeader> RecieveCompnents();

        public MyTreeNode RecieveTreeNodes();

        public string GetPluginsName();

        public void Cleanup();

        /// <summary> This can be used to determine which header needs their checksum updated depending on the row that was updated </summary>
        public static List<SuperHeader> GetHeaderFromRow(int row, Dictionary<string, SuperHeader> compMap, DataStorage data)
        {
            List<SuperHeader> list = new List<SuperHeader>();

            foreach (var map in compMap)
            {
                SuperHeader header = map.Value;
                if (header.Component == "everything") continue;

                int startingRow = (int)Math.Floor(header.StartPoint / 16.0);
                int endingRow = (int)Math.Floor(header.EndPoint / 16.0);

                int rowOffset = int.Parse(header.GetData(row, 0, DataType.DECIMAL, false, true, data));
                int compensatedRow = (int)Math.Floor(rowOffset / 16.0); // This is our row relative to the start of the file

                if (compensatedRow >= startingRow && compensatedRow <= endingRow) list.Add(header);
            }
            return list;
        }
    }
}
