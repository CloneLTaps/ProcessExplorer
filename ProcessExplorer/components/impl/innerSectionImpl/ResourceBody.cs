using PluginInterface;
using System;

namespace ProcessExplorer.components.impl.innerSectionImpl
{
    public class ResourceBody : SuperHeader
    {
        public ResourceBody(uint startingPoint, uint endPoint, string sectionType) : base(sectionType, (int)Math.Ceiling((endPoint - startingPoint) / 16.0), 3) 
        {
            Component = sectionType;
            StartPoint = startingPoint;
            EndPoint = endPoint;

            Console.WriteLine($"ResourceBody Type:{sectionType} Start:{startingPoint} End:{endPoint} Rows:{(int)Math.Ceiling((endPoint - startingPoint) / 16.0)}");

            Size = null;
            Desc = null;
        }

        public override void OpenForm(int row, DataStorage dataStorage)
        {
            return; // No custom forms required here
        }

    }
}
