import mqtt from "mqtt";

const mqttClient = mqtt.connect({ host: "127.0.0.1" });

setInterval(() => {
  console.log("Publish update...");
  mqttClient.publish(`currentSensor/data`, (Math.random() * 10 + 20).toString());

  mqttClient.publish(`currentSensor/state`, "Online");
}, 5000);

