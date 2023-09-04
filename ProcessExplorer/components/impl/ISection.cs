using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessExplorer.components.impl
{
    interface ISection
    {
        public SuperHeader.SectionTypes GetSectionType();
    }
}
