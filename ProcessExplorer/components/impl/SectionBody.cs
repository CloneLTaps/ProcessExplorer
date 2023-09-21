using System;

namespace ProcessExplorer.components.impl
{
    class SectionBody : PluginInterface.SuperHeader
    {

        public SectionBody(int startingPoint, int endPoint, string sectionType) : base("section body", (int)Math.Ceiling((endPoint - startingPoint) / 16.0), 3)
        {
            Component = sectionType;
            StartPoint = startingPoint;
            EndPoint = endPoint;

            Size = null;
            Desc = null;
        }

        public override void OpenForm(int row, PluginInterface.DataStorage dataStorage)
        {
            return; // No custom forms required here
        }

    }
}
