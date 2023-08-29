using System;
using System.Linq;
using System.Globalization;

namespace ProcessExplorer.components
{
    class DosStub : SuperHeader
    {
        public DosStub(ProcessHandler processHandler) : base(processHandler, 1, 3, false)
        {
            Console.WriteLine("Starting DosStub headerRowSize:" + processHandler.dosHeader.RowSize + " StartPoint:" + StartPoint);
            StartPoint = processHandler.dosHeader.EndPoint;
            string littleEndianHex = processHandler.dosHeader.hexArray[processHandler.dosHeader.RowSize - 1, 1];
            string[] hexBytes = littleEndianHex.Split(' ');
            Array.Reverse(hexBytes); // Reverse the order of bytes
            string hexEndPoint = string.Concat(hexBytes);

            Console.WriteLine("DosStub EndPoint:" + hexEndPoint);
            EndPoint = Convert.ToInt32(hexEndPoint.Replace("0x", ""), 16); 
            PopulateNonDescArrays();
        }

    }
}
