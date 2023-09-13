
namespace ProcessExplorer.components.impl
{
    class CertificationTable : SuperHeader
    {

        public CertificationTable(ProcessHandler processHandler, int startPoint, int size) : base(processHandler, ProcessHandler.ProcessComponent.CERTIFICATE_TABLE, 1, 3)
        {
            StartPoint = startPoint;
            EndPoint = StartPoint + size;

            Size = null;
            Desc = null;
        }

        public override void OpenForm(int row)
        {
            return; // No custom forms required here
        }
    }
}
