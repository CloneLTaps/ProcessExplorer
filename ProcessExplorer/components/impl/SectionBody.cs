using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessExplorer.components.impl
{
    class SectionBody : SuperHeader, ISection
    {
        private readonly SectionTypes sectionType;

        public SectionBody(ProcessHandler processHandler, int startingPoint, int endPoint, SectionTypes sectionType) : base(processHandler, 1, 3, false)
        {
            this.sectionType = sectionType;
            StartPoint = startingPoint;
            EndPoint = endPoint;
            Console.WriteLine("Body SectionType:" + sectionType.ToString() + " StartingPoint:" + startingPoint + " EndingPoint:" + endPoint);
            PopulateNonDescArrays();
        }

        public override void OpenForm(int row)
        {
            return; // No custom forms required here
        }

        public SectionTypes GetSectionType()
        {
            return sectionType;
        }
    }
}
