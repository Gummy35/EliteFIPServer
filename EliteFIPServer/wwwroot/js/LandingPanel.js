"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/gamedataupdatehub", { skipNegotiation: true, transport: signalR.HttpTransportType.WebSockets })
    .withAutomaticReconnect()
    .build();

const roundAccurately = (number, decimalPlaces) => Number(Math.round(number + "e" + decimalPlaces) + "e-" + decimalPlaces);

connection.on("DockingGrantedData", function (DockingGrantedData) {

    var data = JSON.parse(DockingGrantedData);
    if (data != null) {
        console.log(data);
        if (data.LandingPad != null) { document.getElementById("LandingPad").innerHTML = data.LandingPad };
        if (data.MarketId != null) { document.getElementById("MarketId").innerHTML = data.MarketId };
        if (data.StationName != null) { document.getElementById("StationName").innerHTML = data.StationName };
        if (data.StationType != null) { document.getElementById("StationType").innerHTML = data.StationType };        
    }
});

connection.start().catch(function (err) {
    return console.error(err.toString());
});

