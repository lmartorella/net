import mqtt from "mqtt";

const mqttClient = mqtt.connect({ host: "127.0.0.1", protocolVersion: 5 });

mqttClient.subscribe("notification_test/ack");
mqttClient.on("message", (topic, payload, packet) => {
  if (topic === "notification_test/ack") {
    if (packet.properties.correlationData.toString() !== "x") {
       console.error("Wrong correlation data received");
    }
    console.log("Ack: " + payload.toString());
  }
});

mqttClient.publish("notification/send_mail", JSON.stringify({
  title: "Test title",
  body: "Test body",
  isAdminReport: true
}), { 
  properties: {
    responseTopic: "notification_test/ack",
    correlationData: Buffer.from("x")
  }
});
