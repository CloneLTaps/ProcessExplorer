using System;
using System.Globalization;

namespace ProcessExplorer.components.impl
{
    class OptionalHeaderDataDirectories : SuperHeader
    {
        public OptionalHeaderDataDirectories(ProcessHandler processHandler, int startingPoint) : base(processHandler, 8, 3, true)
        {

        }

        public override void OpenForm(int row)
        {
            return;
        }
    }
}
