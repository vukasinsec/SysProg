using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
        private readonly ReaderWriterLockSlim _cacheLock;
        private readonly ReaderWriterLockSlim _fileLock;

        public string Url { get; }

        public WebServer(string url, int cacheCapacity)
        {
            _listener = new HttpListener();
            Url = url;
            _listener.Prefixes.Add(Url);
            _jsonParser = new JSONParser();
            _cache = new LRUCache<string, string>(cacheCapacity);
            _cacheLock = new ReaderWriterLockSlim();
            _fileLock = new ReaderWriterLockSlim();
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
                _cacheLock.EnterReadLock();
                try
                {
                    if (_cache.TryGetValue(apiUrl, out responseData))
                    {
                        Console.WriteLine("[!] Podaci su pribavljeni iz keša!");
                    }
                    else
                    {
                        _cacheLock.ExitReadLock(); // Release the read lock before attempting a write lock
                        responseData = GetDataFromApi(apiUrl);
                        _cacheLock.EnterWriteLock();
                        try
                        {
                            _cache.AddOrUpdate(apiUrl, responseData);
                        }
                        finally
                        {
                            _cacheLock.ExitWriteLock();
                        }
                        Console.WriteLine("[!] Podaci su pribavljeni iz API-ja!");
                    }
                }
                finally
                {
                    if (_cacheLock.IsReadLockHeld)
                        _cacheLock.ExitReadLock();
                }


                JObject jsonData = JObject.Parse(responseData);
                List<string> titles = _jsonParser.ExtractTitles(jsonData);

                WriteDataToFile(titles);

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
            catch (TimeoutException ex)
            {
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                using (StreamWriter writer = new StreamWriter(response.OutputStream))
                {
                    writer.Write($"Timeout exception occurred: {ex.Message}");
                }
            }
            catch (HttpRequestException ex)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                using (StreamWriter writer = new StreamWriter(response.OutputStream))
                {
                    writer.Write($"HTTP request exception occurred: {ex.Message}");
                }
            }
            catch (ArgumentException ex)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                using (StreamWriter writer = new StreamWriter(response.OutputStream))
                {
                    writer.Write($"Invalid request: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                using (StreamWriter writer = new StreamWriter(response.OutputStream))
                {
                    writer.Write($"An error occurred: {ex.Message}");
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

        private void WriteDataToFile(List<string> titles)
        {
            _fileLock.EnterWriteLock();
            try
            {
                string filePath = @"C:\Users\Windows10\Desktop\GitHub\SysProg\izlaz.txt";
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine($"Retrieved at: {DateTime.Now}");
                    foreach (var title in titles)
                    {
                        writer.WriteLine(title);
                    }
                    writer.WriteLine("=======================================");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while writing to the file: {ex.Message}");
            }
            finally
            {
                _fileLock.ExitWriteLock();
            }
        }


    }
}
