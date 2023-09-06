using System;
using System.Linq;
using System.Globalization;

namespace ProcessExplorer.components
{
    class DosStub : SuperHeader
    {
        public DosStub(ProcessHandler processHandler) : base(processHandler, 1, 3, false)
        {
            if(processHandler.everything.EndPoint <= processHandler.dosHeader.EndPoint)
            {   // This means our PE only consits of a PE Header 
                FailedToInitlize = true;
                return;
            }

            Console.WriteLine("EVERYTHING END POINT:" + processHandler.everything.EndPoint + " DosHeaderEndPoint:" + processHandler.dosHeader.EndPoint);

            StartPoint = processHandler.dosHeader.EndPoint;
            string littleEndianHex = processHandler.dosHeader.hexArray[processHandler.dosHeader.RowSize - 1, 1];
            string[] hexBytes = littleEndianHex.Split(' ');
            Array.Reverse(hexBytes); // Reverse the order of bytes
            string hexEndPoint = string.Concat(hexBytes);

            EndPoint = Convert.ToInt32(hexEndPoint.Replace("0x", ""), 16); 
            PopulateNonDescArrays();
        }

        public override void OpenForm(int row)
        {
            return; // No custom forms required here
        }

    }
}
