import mqtt from "mqtt";

const mqttClient = mqtt.connect({ host: "127.0.0.1", protocolVersion: 5 });

mqttClient.subscribe("notification/send_mail");
mqttClient.on("message", (_topic, payload, packet) => {
  const { title, body } = JSON.parse(payload.toString());
  console.log(`Send mail request: title '${title}'`);
  console.log(`Body: ${body}`);
  console.log();

  mqttClient.publish(packet.properties.responseTopic, "{}", { properties: { correlationData: packet.properties.correlationData } });
});
