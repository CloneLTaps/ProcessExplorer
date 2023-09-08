
namespace ProcessExplorer.components.impl
{
    class SectionBody : SuperHeader
    {

        public SectionBody(ProcessHandler processHandler, int startingPoint, int endPoint, ProcessHandler.ProcessComponent sectionType) 
            : base(processHandler, ProcessHandler.ProcessComponent.SECTION_BODY, 1, 3, false)
        {
            Component = sectionType;
            StartPoint = startingPoint;
            EndPoint = endPoint;
            PopulateNonDescArrays();
        }

        public override void OpenForm(int row)
        {
            return; // No custom forms required here
        }

    }
}
