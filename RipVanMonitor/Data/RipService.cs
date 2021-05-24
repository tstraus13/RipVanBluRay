using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using RipVanLibrary;

namespace RipVanMonitor.Data
{
    public class RipService
    {
        private static readonly HttpClient client = new HttpClient();

        public async Task<IEnumerable<string>> GetDiscDrives()
        {
            ClearClient();

            var resp = client.GetStreamAsync("https://localhost:5001/Rip/DiscDrives");

            var drives = await JsonSerializer.DeserializeAsync<List<string>>(await resp);

            return drives;
        }

        private void ClearClient()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");
        }
    }
}
