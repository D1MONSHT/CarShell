using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace CarShell.Services
{
    public class LocalMapServer
    {
        private readonly HttpListener listener = new();
        private readonly string rootPath;
        private bool running;

        public string Url => "http://127.0.0.1:9696/";

        public LocalMapServer()
        {
            rootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Maps");
        }

        public void Start()
        {
            if (running || listener.IsListening)
                return;

            running = true;

            if (listener.Prefixes.Count == 0)
                listener.Prefixes.Add(Url);

            listener.Start();

            Task.Run(ListenLoop);
        }

        private async Task ListenLoop()
        {
            while (running)
            {
                try
                {
                    var context = await listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequest(context));
                }
                catch
                {
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            try
            {
                string relativePath = context.Request.Url!.AbsolutePath.TrimStart('/');

                if (string.IsNullOrWhiteSpace(relativePath))
                    relativePath = "map.html";

                string filePath = Path.Combine(rootPath, relativePath);

                if (!File.Exists(filePath))
                {
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                    return;
                }

                FileInfo fileInfo = new FileInfo(filePath);
                long totalLength = fileInfo.Length;

                context.Response.ContentType = GetContentType(filePath);
                context.Response.AddHeader("Accept-Ranges", "bytes");

                string? rangeHeader = context.Request.Headers["Range"];

                if (!string.IsNullOrEmpty(rangeHeader) && rangeHeader.StartsWith("bytes="))
                {
                    string[] range = rangeHeader.Replace("bytes=", "").Split('-');

                    long start = long.Parse(range[0]);
                    long end = string.IsNullOrEmpty(range[1])
                        ? totalLength - 1
                        : long.Parse(range[1]);

                    if (end >= totalLength)
                        end = totalLength - 1;

                    long length = end - start + 1;

                    context.Response.StatusCode = 206;
                    context.Response.ContentLength64 = length;
                    context.Response.AddHeader("Content-Range", $"bytes {start}-{end}/{totalLength}");

                    using FileStream fs = new FileStream(
                        filePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite
                    );

                    fs.Seek(start, SeekOrigin.Begin);

                    byte[] buffer = new byte[64 * 1024];
                    long remaining = length;

                    while (remaining > 0)
                    {
                        int read = fs.Read(
                            buffer,
                            0,
                            (int)Math.Min(buffer.Length, remaining)
                        );

                        if (read <= 0)
                            break;

                        context.Response.OutputStream.Write(buffer, 0, read);
                        remaining -= read;
                    }
                }
                else
                {
                    context.Response.StatusCode = 200;
                    context.Response.ContentLength64 = totalLength;

                    using FileStream fs = new FileStream(
                        filePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite
                    );

                    fs.CopyTo(context.Response.OutputStream);
                }

                context.Response.OutputStream.Close();
            }
            catch
            {
                try
                {
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                }
                catch
                {
                }
            }
        }

        private string GetContentType(string path)
        {
            return Path.GetExtension(path).ToLower() switch
            {
                ".html" => "text/html; charset=utf-8",
                ".js" => "application/javascript",
                ".css" => "text/css",
                ".json" => "application/json",
                ".pmtiles" => "application/octet-stream",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".svg" => "image/svg+xml",
                ".ico" => "image/x-icon",
                _ => "application/octet-stream"
            };
        }
    }
}