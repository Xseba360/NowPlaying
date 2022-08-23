using System.Net;
using System.Text;
using Windows.Media.Control;

namespace NowPlaying
{
    internal static class HttpServer
    {
        private static HttpListener? _listener;
        private const string Url = "http://localhost:8000/";
        private static string _html = File.ReadAllText("index.html");

        private static async Task HandleIncomingConnections()
        {
            var runServer = true;

            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (runServer)
            {
                // Will wait here until we hear from a connection
                var ctx = await _listener?.GetContextAsync()!;

                // Peel out the requests and response objects
                var req = ctx.Request;
                var resp = ctx.Response;

                // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                if ((req.HttpMethod == "POST") && (req.Url?.AbsolutePath == "/shutdown"))
                {
                    Console.WriteLine("Shutdown requested");
                    runServer = false;
                }

                var output = "";
                switch (req.Url?.AbsolutePath)
                {
                    case "/getInfo":
                        var sessionManager = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().GetAwaiter().GetResult();
                        var currentSession = sessionManager.GetCurrentSession();
                        if (currentSession != null)
                        {
                            var mediaProperties = currentSession.TryGetMediaPropertiesAsync().GetAwaiter().GetResult();
                            output = $"♫ {mediaProperties.Title} - {mediaProperties.Artist}";
                        }
                        var songData = Encoding.UTF8.GetBytes(output);
                        resp.ContentType = "text/plain";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = songData.LongLength;
                        // Write out to the response stream (asynchronously), then close it
                        await resp.OutputStream.WriteAsync(songData);
                        resp.Close();
                        break;
                    case "/index.html":
                    case "/":
                        var htmlData = Encoding.UTF8.GetBytes(_html);
                        resp.ContentType = "text/html";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = htmlData.LongLength;

                        // Write out to the response stream (asynchronously), then close it
                        await resp.OutputStream.WriteAsync(htmlData);
                        break;
                    default:
                        resp.Close();
                        break;
                }

            }
        }


        public static void Main(string[] args)
        {
            // Create a Http server and start listening for incoming connections
            _listener = new HttpListener();
            _listener.Prefixes.Add(Url);
            _listener.Start();
            Console.WriteLine("Listening for connections on {0}", Url);

            // Handle requests
            var listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            _listener.Close();
        }
    }
}


