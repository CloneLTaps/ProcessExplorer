using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Collections.Generic;

namespace ProcessExplorer
{
    static class Program
    {
        private static List<PluginInterface.IPlugin> loadedPlugins = new List<PluginInterface.IPlugin>();

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            /*var pluginFolder = @"Path\to\Your\Plugin\Folder";
            var pluginFiles = Directory.GetFiles(pluginFolder, "*.dll");

            foreach (var pluginFile in pluginFiles)
            {
                var assembly = Assembly.LoadFrom(pluginFile);
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(PluginInterface.IPlugin).IsAssignableFrom(type))
                    {
                        var plugin = (PluginInterface.IPlugin)Activator.CreateInstance(type);
                        // Initialize and store the loaded plugins
                        plugin.Initialize();
                        loadedPlugins.Add(plugin);
                    }
                }
            }*/

            foreach (var plugin in loadedPlugins)
            {
                plugin.Execute();
            }


            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());

            Console.WriteLine("Starting HexEditor");
        }

    }
}
