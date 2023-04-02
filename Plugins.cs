using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

namespace Server
{
    public interface IPlugin
    {
        string name { get; }
        string description { get; }
        void Boot();
        string OnRequest(string url, string page) => page;
        byte[] RawRequest(string url, string body, HttpListenerRequest request, HttpListenerResponse response) => null;
    }
    public static class Constants
    {
        public const string FolderName = "Plugins";
    }
    public class PluginLoader
    {
        public static List<IPlugin> Plugins { get; set; }
        public static void LoadPlugins()
        {
            Plugins = new List<IPlugin>();
            if (Directory.Exists(Constants.FolderName))
            {
                string[] files = Directory.GetFiles(Constants.FolderName);
                foreach (string file in files)
                {
                    if (file.EndsWith(".dll"))
                    {
                        Assembly.LoadFile(Path.GetFullPath(file));
                    }
                }
            }
            Type interfaceType = typeof(IPlugin);
            Type[] types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(p => interfaceType.IsAssignableFrom(p) && p.IsClass)
                .ToArray();
            foreach (Type type in types)
            {
                Plugins.Add((IPlugin)Activator.CreateInstance(type));
            }
        }
    }
}