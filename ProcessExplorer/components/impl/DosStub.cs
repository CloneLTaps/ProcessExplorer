using System;

namespace ProcessExplorer.components
{
    class DosStub : SuperHeader
    {
        public DosStub(ProcessHandler processHandler) : base(processHandler, ProcessHandler.ProcessComponent.DOS_STUB, 1, 3, false)
        {
            Console.WriteLine("Start DosStub");
            if (processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.EVERYTHING).EndPoint 
                <= processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.DOS_HEADER).EndPoint) // abcdefghijklmnop _MouseHandlerInf
            {   // This means our PE only consits of a PE Header 
                FailedToInitlize = true;
                return;
            }
            Console.WriteLine("Start DosStub p2");

            SuperHeader dosHeader = processHandler.GetComponentFromMap(ProcessHandler.ProcessComponent.DOS_HEADER);
            StartPoint = dosHeader.EndPoint;
            string littleEndianHex = dosHeader.hexArray[dosHeader.RowSize - 1, 1];
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
