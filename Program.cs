using System;
using System.IO;
using System.Text;
using System.Net;
using System.Threading.Tasks;

namespace Server
{
    class HttpServer
    {
        private static HttpListener listener;
        public static ushort port = 80;
        public static bool downloadFile = false;
        private static async Task HandleIncomingConnections()
        {
            bool runServer = true;
            while (runServer)
            {
				HttpListenerContext ctx = await listener.GetContextAsync();
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;
                var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                string body = reader.ReadToEnd();
                try
                {
                    bool nextCheck = true;
                    string path = Uri.UnescapeDataString(("html" + req.Url.AbsolutePath).Replace('/', Path.DirectorySeparatorChar));
                    Stream output = resp.OutputStream;
                    string page = string.Empty;
                    foreach (IPlugin plugin in PluginLoader.Plugins)
                    {
                        byte[] data = plugin.RawRequest(path, body, req, resp);
                        if (data != null)
                        {
                            byte[] buffer = data;
                            resp.ContentLength64 = buffer.Length;
                            output.Write(buffer, 0, buffer.Length);
                            output.Close();
                            nextCheck = false;
                        }
                    }
                    if (nextCheck && req.HttpMethod == "GET")
                    {
                        if (File.Exists(path))
                        {
                            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                            resp.ContentLength64 = stream.Length;
                            if (downloadFile)
                            {
                                resp.AddHeader("Content-Disposition", "attachment");
                            }
							stream.CopyTo(resp.OutputStream);
						}
                        else
                        {

							if (File.Exists(path + Path.DirectorySeparatorChar + "index.html"))
							{
								page = File.ReadAllText(path + Path.DirectorySeparatorChar + "index.html");
							}
							else if (File.Exists("404.html"))
							{
								page = File.ReadAllText("404.html");
							}
							else
							{
								resp.StatusCode = 404;
								page = "<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n    <meta charset=\"UTF-8\">\r\n    <meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n    <title>Page not found</title>\r\n</head>\r\n<body\r\n    style=\"background-color:#4F0000; color:lightgray; text-align: center; font-size: xx-large; font-family: system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif;\">\r\n    <h1>Page not found (404)</h1>\r\n    <p>Powered by Eternal (TTMC Corporation)</p>\r\n</body>\r\n</html>";
							}
                            foreach (IPlugin plugin in PluginLoader.Plugins)
							{
                                page = plugin.OnRequest(path, page);
                            }
							byte[] buffer = Encoding.UTF8.GetBytes(page);
							resp.ContentLength64 = buffer.Length;
							output.Write(buffer, 0, buffer.Length);
						}
						output.Close();
					}
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(e);
                }
            }
        }
        public static void Main(string[] args)
        {
            LoadConfig();
            FirstStart();
			Directory.CreateDirectory(Constants.FolderName);
            PluginLoader.LoadPlugins();
            foreach (IPlugin plugin in PluginLoader.Plugins)
            {
                plugin.Boot();
            }
            if (args.Length >= 1)
            {
                ushort.TryParse(args[0], out port);
            }
            string url = "http://*:"+port+"/";
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Listening for connections on {0}", url);
            Console.ForegroundColor = ConsoleColor.Gray;
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();
            listener.Close();
        }
        public static void FirstStart()
        {
            if (!Directory.Exists("html"))
            {
				Directory.CreateDirectory("html");
                File.WriteAllText("html" + Path.DirectorySeparatorChar + "index.html", "<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n    <meta charset=\"UTF-8\">\r\n    <meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\r\n    <title>Welcome</title>\r\n</head>\r\n<body\r\n    style=\"background-color:#1B1B1B; color:lightgray; text-align: center; font-size: xx-large; font-family: system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, 'Open Sans', 'Helvetica Neue', sans-serif;\">\r\n    <h1>Welcome on your webpage!</h1>\r\n    <p>Powered by Eternal (TTMC Corporation)</p>\r\n</body>\r\n</html>");
			}
        }
        public static void LoadConfig()
        {
            if (!File.Exists("config.cfg"))
            {
                CreateConfig();
            }
            string[] lines = File.ReadAllLines("config.cfg");
            foreach (string line in lines)
            {
                if (line.Contains('='))
                {
					string[] tmp = line.Replace(" ", "").Split('=');
					if (tmp[0].ToLower() == "port")
					{
                        ushort.TryParse(tmp[1], out port);
					}
                    if (tmp[0].ToLower() == "download")
                    {
                        bool.TryParse(tmp[1], out downloadFile);
                    }
				}
            }
        }
        public static void CreateConfig()
        {
            string[] lines = { "# Eternal v0.1", "# Created by TTMC Corporation", "port = 80" };
            File.WriteAllLines("config.cfg", lines);
        }
    }
}