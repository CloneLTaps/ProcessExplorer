
namespace ProcessExplorer.components.impl
{
    class SectionBody : PluginInterface.SuperHeader
    {

        public SectionBody(int startingPoint, int endPoint, string sectionType) : base("section body", 1, 3)
        {
            Component = sectionType;
            StartPoint = startingPoint;
            EndPoint = endPoint;
        }

        public override void OpenForm(int row, PluginInterface.DataStorage dataStorage)
        {
            return; // No custom forms required here
        }

    }
}
