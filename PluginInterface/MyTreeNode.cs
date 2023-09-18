using System;
using System.Collections.Generic;
using System.Text;

namespace PluginInterface
{
    /// <summary> Designed to be similar to the Form version of TreeNode for easy data transfering </summary>
    public class MyTreeNode
    {
        public string Data { get; set; }
        public List<MyTreeNode> Children { get; set; }

        public MyTreeNode(string data)
        {
            Data = data;
            Children = new List<MyTreeNode>();
        }
    }
}
