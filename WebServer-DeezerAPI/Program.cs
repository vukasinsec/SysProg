using System;
using WebServer_DeezerAPI.Services;

namespace DeezerConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string url = "http://localhost:8080/";
            int cacheCapacity = 100;

            var server = new WebServer(url, cacheCapacity);
            server.Start();
            Console.WriteLine("[!] Server je pokrenut na portu 8080..");
            Console.WriteLine("[!] Pritisnite Enter za zaustavljanje servera..");
            Console.ReadLine();
            server.Stop();
        }
    }
}
