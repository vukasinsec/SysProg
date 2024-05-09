using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebServer_DeezerAPI.Services
{
    public class JSONParser
    {
        public List<string> ExtractTitles(JObject jsonData)
        {
            List<string> titles = new List<string>();

            try
            {
                var dataArray = (JArray)jsonData["data"];

                foreach (var item in dataArray)
                {
                    string title = (string)item["title"];
                    titles.Add(title);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while extracting titles: " + ex.Message);
            }

            return titles;
        }


    }
}
