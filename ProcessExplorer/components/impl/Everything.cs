using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessExplorer.components.impl
{
    class Everything : SuperHeader
    {
        public Everything(ProcessHandler processHandler, string[,] hex, string[,] deci, string[,] binary) : base(processHandler, 8, 3, false) // We override 8 (end point) in the constructor 
        {
            Console.WriteLine("Starting Everything");
            StartPoint = 0;
            SetAllArrays(hex, deci, binary);
            Console.WriteLine("FInished setting all arrays");

            // This will set this files end point
            int length = hexArray.GetLength(0);
            string lastlineOfHex = hexArray[length - 1, 1];
            string[] hexBytes = lastlineOfHex.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int byteCount = hexBytes.Length;
            EndPoint = Convert.ToInt32(deciArray[length - 1, 0] + byteCount);
            Console.WriteLine("EndPointOffset:" + hexArray[length - 1, 0]);
        }

        public override void OpenForm(int row)
        {
            return; // No custom forms required here
        }

    }
}
