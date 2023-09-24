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
            Console.WriteLine("EVERYTHING Length:" + length + " NextToLastHex:" + dataStorage.FilesHex[length - 2, 1] + " LastHex:" + dataStorage.FilesHex[length - 1, 1] + " \n");

/*            for(int i=0; i<length; i++)
            {
                Console.WriteLine("i:" + i + " Offset:" + dataStorage.FilesHex[i, 0] + " Hex:" + dataStorage.FilesHex[i, 1]);
            }
            Console.WriteLine(" \n");*/

            string lastlineOfHex = dataStorage.FilesHex[length - 1, 1];
            string[] hexBytes = lastlineOfHex.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int byteCount = hexBytes.Length;
            EndPoint = Convert.ToInt32(dataStorage.FilesDecimal[length - 1, 0]) + byteCount;
        }

        public override void OpenForm(int row, PluginInterface.DataStorage dataStorage)
        {
            return; // No custom forms required here
        }

    }
}
