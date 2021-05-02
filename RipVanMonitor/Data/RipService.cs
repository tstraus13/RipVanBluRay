using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using RipVanLibrary;

#if !RELEASE
using Microsoft.Extensions.Logging;
#endif

namespace RipVanMonitor.Data
{
    public class RipService
    {
        public List<Rip> Rips;

        private static HubConnection Connection;

        public RipService()
        {
            Init().Wait();
        }

        private async Task Init()
        {
#if RELEASE
            Connection = new HubConnectionBuilder()
                .WithUrl("")
                .WithAutomaticReconnect();
#else
            Connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:5001/hubs/ripping")
                .WithAutomaticReconnect()
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();

                    logging.SetMinimumLevel(LogLevel.Debug);
                })
                .Build();
#endif

            Connection.On<List<Rip>>("ReceiveRips", ReceiveRips);

            await Connection.StartAsync();
        }

        public async Task GetRips()
        {
            await Connection.InvokeAsync("GetRips");
        }

        private void ReceiveRips(List<Rip> rips)
        {
            Rips = rips;
        }
    }
}
