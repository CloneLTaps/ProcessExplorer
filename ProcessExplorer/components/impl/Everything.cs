using System;

namespace ProcessExplorer.components.impl
{
    class Everything : SuperHeader
    {
        public Everything(ProcessHandler processHandler, int length) : base(processHandler, "everything", length, 3) 
        {
            StartPoint = 0;
            Size = null;
            Desc = null;

            // This will set this files end point
            string lastlineOfHex = GetFilesHex(length - 1, 1);
            string[] hexBytes = lastlineOfHex.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int byteCount = hexBytes.Length;
            EndPoint = Convert.ToInt32(GetFilesDecimal(length - 1, 0)) + byteCount;
        }

        public override void OpenForm(int row)
        {
            return; // No custom forms required here
        }

    }
}
