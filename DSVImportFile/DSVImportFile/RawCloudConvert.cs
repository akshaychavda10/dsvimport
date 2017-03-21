using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DSVImportFile
{
    public class RawCloudConvert
    {
        private string apiKey;

        public RawCloudConvert()
        {
            // this.apiKey = apiKey;
        }
        public RawCloudConvert(string apiKey)
        {
            this.apiKey = apiKey;
        }

        public async Task<string> Convert(string inputFormat, string outputFormat, string fileName, string data)
        {
            var url = await CreateProcess(inputFormat, outputFormat);
            return await Upload(url, fileName, data, outputFormat);
        }

        private async Task<string> CreateProcess(string inputFormat, string outputFormat)
        {
            var request = new
            {
                apikey = apiKey,
                inputformat = inputFormat,
                outputformat = outputFormat
            };

            var json = await PostJson("https://api.cloudconvert.com/process", request);

            dynamic obj = JObject.Parse(json);
            return "https:" + obj.url;
        }

        private static async Task<string> Upload(string url, string fileName,
            string data, string outputFormat)
        {
            var request = new
            {
                input = "base64",
                file = data,
                filename = fileName,
                outputformat = outputFormat,
                download = "inline",
                wait = "true"
            };

            return await PostJson(url, request);
        }


        private static async Task<string> PostJson(string url, object data)
        {

            var parameters = JsonConvert.SerializeObject(data);

            using (var wc = new WebClient())
            {
                try
                {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/json";
                    return wc.UploadString(url, "POST", parameters);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return "0";
                }

            }
        }




    }
}

