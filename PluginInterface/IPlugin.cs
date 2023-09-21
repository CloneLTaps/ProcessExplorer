using System;
using System.Collections.Generic;

namespace PluginInterface
{
    public interface IPlugin
    {
        /// <summary> This will return true if this plugin supports this file type. </summary>
        public bool Initialized(DataStorage data);

        /// <summary> This is called when data has been edited which means we can now recalculate data such as checksums. </summary>
        public void ReclaculateHeaders(int rowChanged, DataStorage data);
        public Dictionary<string, SuperHeader> RecieveCompnents();

        public MyTreeNode RecieveTreeNodes();

        public void Cleanup();

    }
}
