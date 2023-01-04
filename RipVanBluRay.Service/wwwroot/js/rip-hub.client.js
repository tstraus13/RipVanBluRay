const ripHubClient = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/rip")
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

async function start() {
    try {
        await ripHubClient.start();
        console.log("SignalR Connected.");
    } catch (err) {
        console.log(err);
        setTimeout(start, 5000);
    }
};

ripHubClient.onclose(async () => {
    await start();
});

// Start the connection.
start();