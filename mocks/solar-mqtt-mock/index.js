import mqtt from "mqtt";

const mqttClient = mqtt.connect({ host: "127.0.0.1" });

setInterval(() => {
  console.log("Publish update...");
  mqttClient.publish(`solar/data`, JSON.stringify({
    TimeStamp: `\/Date(${new Date().getTime()}+0200)\/`,
    EnergyTodayWh: 4380,
    Fault: 0,
    GridCurrentA: 10.79,
    GridFrequencyHz: 50,
    GridVoltageV: 235,
    HomeUsageCurrentA: 0,
    InverterState: {
      FaultCode: "",
      OperatingState: 2
    },
    InverterStateStr: "",
    PowerW: 2530,
    String1CurrentA: 2.84,
    String1VoltageV: 310.1,
    String2CurrentA: 7.23,
    String2VoltageV: 240.4,
    TotalEnergyKWh: 5618.5
  }));

  mqttClient.publish(`solar/state`, "Online");
}, 5000);

