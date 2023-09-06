using System;

namespace ProcessExplorer.components.impl
{
    class Everything : SuperHeader
    {
        public Everything(ProcessHandler processHandler, string[,] hex, string[,] deci, string[,] binary) : base(processHandler, 8, 3, false) // We override 8 (end point) in the constructor 
        {
            StartPoint = 0;
            SetAllArrays(hex, deci, binary);

            // This will set this files end point
            int length = hexArray.GetLength(0);
            string lastlineOfHex = hexArray[length - 1, 1];
            string[] hexBytes = lastlineOfHex.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int byteCount = hexBytes.Length;
            EndPoint = Convert.ToInt32(deciArray[length - 1, 0]) + byteCount;
        }

        public override void OpenForm(int row)
        {
            return; // No custom forms required here
        }

    }
}
