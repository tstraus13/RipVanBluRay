const ripHubClient = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/rip")
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

async function startHubConnection() {
    try {
        await ripHubClient.start();
        console.log("SignalR Connected.");
    } catch (err) {
        console.log(err);
        setTimeout(startHubConnection, 5000);
    }
};

ripHubClient.onclose(async () => {
    await startHubConnection();
});

ripHubClient.on("discDriveUpdate", (drive) => {
    
    let previousDiscPresent = $('#' + drive.id + '-disc-present').text();
    let type = 'None';
    
    switch (drive.discMedia) {
        case 0:
            type = 'Audio';
            break;
        case 1:
            type = 'DVD';
            break;
        case 2:
            type = 'BluRay';
        default:
            break;
    }

    $('#' + drive.id + '-status').text(drive.discPresent ? 'Ripping' : 'Waiting');
    $('#' + drive.id + '-disc-present').text(drive.discPresent ? 'Yes' : 'No');
    $('#' + drive.id + '-disc-media').text(type);
    $('#' + drive.id + '-label').text(drive.label === undefined || drive.label === null || drive.label.trim() === '' ? 'N/A' : drive.label);
    $('#' + drive.id + '-disc-size').text(drive.sizeGigaBytes <= 1 ? drive.sizeMegaBytes + ' MB' : drive.sizeGigaBytes + ' GB');
    $('#' + drive.id + '-rip-size').text(drive.currentRipSizeGigaBytes <= 1 ? drive.currentRipSizeMegaBytes + ' MB' : drive.currentRipSizeGigaBytes + ' GB');
    
    let logLines = drive.currentLogFileContents.split(/(?:\r\n|\r|\n)/g);
    let logHtml = '';
    
    logLines.forEach(element => element.trim() === '' || element === undefined ? '' : logHtml += '<span>' + element + '</span>');
    
    $('.modal-body').html(logHtml);
    $('.modal-title').text(drive.id + ' Log File');
    
    if (previousDiscPresent === 'No' && drive.discPresent) {
        CD_START(drive.id);
    }

    if (previousDiscPresent === 'Yes' && !drive.discPresent) {
        CD_END(drive.id);
    } 
    
});

// Start the connection.
startHubConnection();