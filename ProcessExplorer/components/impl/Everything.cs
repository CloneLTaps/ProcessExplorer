using System;
using PluginInterface;

namespace ProcessExplorer.components.impl
{
    public class Everything : SuperHeader
    {
        public Everything(DataStorage dataStorage, int length) : base("everything", length, 3) 
        {
            StartPoint = 0;
            Size = null;
            Desc = null;

            RowSize = dataStorage.GetFilesRows();

            // This will set this files end point
            string lastlineOfHex = dataStorage.FilesHex[length - 1, 1];
            string[] hexBytes = lastlineOfHex.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int byteCount = hexBytes.Length;
            EndPoint = (uint)(Convert.ToUInt32(dataStorage.GetFilesDecimal(length - 1, 0)) + byteCount);
        }

        public override void OpenForm(int row, DataStorage dataStorage)
        {
            return; // No custom forms required here
        }

    }
}
