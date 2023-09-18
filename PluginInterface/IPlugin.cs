using System;
using System.Collections.Generic;

namespace PluginInterface
{
    public interface IPlugin
    {
        public string Name { get; }

        /// <summary> This will return true if this plugin supports this file type. </summary>
        public bool Initialized(DataStorage data);
        public Dictionary<string, SuperHeader> RecieveCompnents();

        public MyTreeNode RecieveTreeNodes();

        public void Cleanup();

    }
}
