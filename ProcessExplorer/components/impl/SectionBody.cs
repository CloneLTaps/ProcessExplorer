
namespace ProcessExplorer.components.impl
{
    class SectionBody : SuperHeader
    {

        public SectionBody(ProcessHandler processHandler, int startingPoint, int endPoint, string sectionType) : base(processHandler, "section body", 1, 3)
        {
            Component = sectionType;
            StartPoint = startingPoint;
            EndPoint = endPoint;
        }

        public override void OpenForm(int row)
        {
            return; // No custom forms required here
        }

    }
}
