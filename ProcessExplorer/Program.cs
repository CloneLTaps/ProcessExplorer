using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Collections.Generic;
using PluginInterface;

namespace ProcessExplorer
{
    static class Program
    {

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(LoadPlugins()));

            Console.WriteLine("Starting HexEditor");
        }

        public static Dictionary<string, IPlugin> LoadPlugins()
        {
            Dictionary<string, IPlugin> loadedPlugins = new Dictionary<string, IPlugin>();

            var pluginFolder = "Plugins";
            var pluginFiles = Directory.GetFiles(pluginFolder, "*.dll");

            foreach (var pluginFile in pluginFiles)
            {
                var assembly = Assembly.LoadFrom(pluginFile);
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsInterface)
                    {
                        var plugin = (IPlugin)Activator.CreateInstance(type);
                        loadedPlugins.Add(plugin.GetPluginsName(), plugin);
                    }
                }
            }
            return loadedPlugins;
        }

    }
}
