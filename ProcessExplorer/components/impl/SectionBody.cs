using System;
using PluginInterface;

namespace ProcessExplorer.components.impl
{
    public class SectionBody : SuperHeader
    {
        public SectionBody(uint startingPoint, uint endPoint, string sectionType) : base("section body", (int)Math.Ceiling((endPoint - startingPoint) / 16.0), 3)
        {
            Component = sectionType;
            StartPoint = startingPoint;
            EndPoint = endPoint;

            Console.WriteLine($"Section Body:{sectionType} Start:{startingPoint} End:{endPoint}");

            Size = null;
            Desc = null;
        }

        public override void OpenForm(int row, DataStorage dataStorage)
        {
            return; // No custom forms required here
        }

    }
}
