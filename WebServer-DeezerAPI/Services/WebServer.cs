using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace WebServer_DeezerAPI.Services
{
    public class WebServer
    {
        private readonly HttpListener _listener;
        private readonly Thread _listenerThread;
        private readonly JSONParser _jsonParser;
        private readonly LRUCache<string, string> _cache;

        public string Url { get; }

        public WebServer(string url, int cacheCapacity)
        {
            _listener = new HttpListener();
            Url = url;
            _listener.Prefixes.Add(Url);
            _jsonParser = new JSONParser();
            _cache = new LRUCache<string, string>(cacheCapacity);
            _listenerThread = new Thread(Listen);
            _listenerThread.Start();
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine($"Server je pokrenut na {Url}");

            while (true)
            {
                ThreadPool.QueueUserWorkItem(ProcessRequest, _listener.GetContext());
            }
        }

        private void Listen()
        {
            try
            {
                while (_listener.IsListening)
                {
                    ThreadPool.QueueUserWorkItem(ProcessRequest, _listener.GetContext());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška u osluškivanju: {ex.Message}");
            }
        }


        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
            Console.WriteLine("Server je zaustavljen");
        }

        private void ProcessRequest(object state)
        {
            HttpListenerContext context = (HttpListenerContext)state;

            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                string query = Uri.EscapeDataString(request.QueryString["q"]);
                string apiUrl = $"https://api.deezer.com/search?q={query}";

                string responseData;
                if (_cache.TryGetValue(apiUrl, out responseData))
                {
                    Console.WriteLine("[!] Podaci su pribavljeni iz keša!");
                }
                else
                {
                    responseData = GetDataFromApi(apiUrl);
                    _cache.AddOrUpdate(apiUrl, responseData);
                    Console.WriteLine("[!] Podaci su pribavljeni iz API-ja!");
                }

                JObject jsonData = JObject.Parse(responseData);
                List<string> titles = _jsonParser.ExtractTitles(jsonData);

                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentType = "application/json";

                using (StreamWriter writer = new StreamWriter(response.OutputStream))
                {
                    foreach (var title in titles)
                    {
                        writer.WriteLine(title);
                    }
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                using (StreamWriter writer = new StreamWriter(response.OutputStream))
                {
                    writer.Write($"Došlo je do greške: {ex.Message}");
                }
            }
            finally
            {
                response.Close();
            }
        }

        private string GetDataFromApi(string apiUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = client.GetAsync(apiUrl).Result;
                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    throw new Exception("Navedena pesma ne postoji.");
                }
            }
        }
    }
}
