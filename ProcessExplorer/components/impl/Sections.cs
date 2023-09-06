using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessExplorer.components.impl
{
    class Sections : SuperHeader
    {
        public Sections(ProcessHandler processHandler, int startPoint) : base(processHandler, 1, 3, false)
        {
            StartPoint = startPoint;
            EndPoint = processHandler.everything.EndPoint;
            PopulateNonDescArrays();
        }

        public override void OpenForm(int row)
        {
            return; // No custom forms required here
        }
    }
}
