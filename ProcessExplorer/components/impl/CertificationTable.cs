
using System;

namespace ProcessExplorer.components.impl
{
    class CertificationTable : PluginInterface.SuperHeader
    {
        public CertificationTable(int startPoint, int size) : base("certificate table", 1, 3)
        {
            StartPoint = startPoint;
            EndPoint = StartPoint + size;
            RowSize = (int) Math.Ceiling(size / 16.0);

            Size = null;
            Desc = null;
        }

        public override void OpenForm(int row, PluginInterface.DataStorage dataStorage)
        {
            return; // No custom forms required here
        }
    }
}
