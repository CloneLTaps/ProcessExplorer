using System;

namespace ProcessExplorer.components
{
    class DosStub : PluginInterface.SuperHeader
    {
        public DosStub(ProcessHandler processHandler, PluginInterface.DataStorage dataStorage, int startPoint) : base("dos stub", 1, 3) 
        {
            if (processHandler.GetComponentFromMap("everything").EndPoint <= processHandler.GetComponentFromMap("dos header").EndPoint) 
            {   // This means our PE only consits of a PE Header 
                FailedToInitlize = true;
                return;
            }

            StartPoint = startPoint;
            Size = null;
            Desc = null;

            int startingPoint = (int)Math.Floor(StartPoint / 16.0);
            for (int row = startingPoint; row < dataStorage.GetFilesRows(); row++)
            {
                string[] hexArray = dataStorage.FilesHex[row, 1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                int offset = StartPoint;
                string signature = "";
                for (int i = 0; i < hexArray.Length; i++)
                {
                    string str = hexArray[i];
                    int rowSize = row - startingPoint;

                    if (str == "50")
                    {
                        offset = (row * 16) + i;
                        signature = "50";
                    }
                    else signature += str;

                    if(signature == "50450000")
                    {
                        EndPoint = offset;
                        RowSize = rowSize;
                        return;
                    }
                }
            }
            FailedToInitlize = true;
        }

        public override void OpenForm(int row, PluginInterface.DataStorage dataStorage)
        {
            return; // No custom forms required here
        }

    }
}
