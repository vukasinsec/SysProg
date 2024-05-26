using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebServer_DeezerAPI.Services
{
    public class WebServer
    {
        private readonly HttpListener _listener;
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
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine($"Server je pokrenut na {Url}");

            while (true)
            {
                ThreadPool.QueueUserWorkItem(async (state) => await ProcessRequest((HttpListenerContext)state), _listener.GetContext());
            }
        }

        private void Listen()
        {
            try
            {
                while (_listener.IsListening)
                {
                    ThreadPool.QueueUserWorkItem(async (state) => await ProcessRequest((HttpListenerContext)state), _listener.GetContext());
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

        private async Task ProcessRequest(HttpListenerContext context)
        {
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
                        responseData = await GetDataFromApi(apiUrl);
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

                await WriteDataToFile(titles);

                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentType = "application/json";

                using (StreamWriter writer = new StreamWriter(response.OutputStream))
                {
                    foreach (var title in titles)
                    {
                        await writer.WriteLineAsync(title);
                    }
                }
            }
            catch (TimeoutException ex)
            {
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                using (StreamWriter writer = new StreamWriter(response.OutputStream))
                {
                    await writer.WriteAsync($"Timeout exception occurred: {ex.Message}");
                }
            }
            catch (HttpRequestException ex)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                using (StreamWriter writer = new StreamWriter(response.OutputStream))
                {
                    await writer.WriteAsync($"HTTP request exception occurred: {ex.Message}");
                }
            }
            catch (ArgumentException ex)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                using (StreamWriter writer = new StreamWriter(response.OutputStream))
                {
                    await writer.WriteAsync($"Invalid request: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                using (StreamWriter writer = new StreamWriter(response.OutputStream))
                {
                    await writer.WriteAsync($"An error occurred: {ex.Message}");
                }
            }
            finally
            {
                response.Close();
            }
        }

        private async Task<string> GetDataFromApi(string apiUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception("Navedena pesma ne postoji.");
                }
            }
        }

        private async Task WriteDataToFile(List<string> titles)
        {
            _fileLock.EnterWriteLock();
            try
            {
                string filePath = @"C:\Users\Windows10\Desktop\GitHub\SysProg\izlaz.txt";
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    await writer.WriteLineAsync($"Retrieved at: {DateTime.Now}");
                    foreach (var title in titles)
                    {
                        await writer.WriteLineAsync(title);
                    }
                    await writer.WriteLineAsync("=======================================");
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