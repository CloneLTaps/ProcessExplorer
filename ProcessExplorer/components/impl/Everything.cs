using System;

namespace ProcessExplorer.components.impl
{
    class Everything : PluginInterface.SuperHeader
    {
        public Everything(PluginInterface.DataStorage dataStorage, int length) : base("everything", length, 3) 
        {
            StartPoint = 0;
            Size = null;
            Desc = null;

            RowSize = dataStorage.GetFilesRows();

            // This will set this files end point
            string lastlineOfHex = dataStorage.FilesHex[length - 1, 1];
            string[] hexBytes = lastlineOfHex.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int byteCount = hexBytes.Length;
            EndPoint = Convert.ToInt32(dataStorage.GetFilesDecimal(length - 1, 0)) + byteCount;
        }

        public override void OpenForm(int row, PluginInterface.DataStorage dataStorage)
        {
            return; // No custom forms required here
        }

    }
}
