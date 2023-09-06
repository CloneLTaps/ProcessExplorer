
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
