using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Collections.Generic;

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
            List<PluginInterface.IPlugin> loadedPlugins = new List<PluginInterface.IPlugin>();

            var pluginFolder = "Plugins";
            var pluginFiles = Directory.GetFiles(pluginFolder, "*.dll");

            foreach (var pluginFile in pluginFiles)
            {
                var assembly = Assembly.LoadFrom(pluginFile);
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(PluginInterface.IPlugin).IsAssignableFrom(type) && !type.IsInterface)
                    {
                        var plugin = (PluginInterface.IPlugin)Activator.CreateInstance(type);
                        loadedPlugins.Add(plugin);
                        Console.WriteLine("Plugin: " + type.Name);
                    }
                }
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(loadedPlugins));

            Console.WriteLine("Starting HexEditor");
        }

    }
}
