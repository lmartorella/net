import express from 'express';
import mqtt from "mqtt";

const mqttClient = mqtt.connect("mqtt://localhost", { will: { topic: "shelly3-mock/status/sys", payload: Buffer.alloc(0), }});
mqttClient.publish("shelly3-mock/status/sys", JSON.stringify({ mac: "123456" }), { retain: true });

const expressApp = express();

expressApp.get('/rpc/Script.List', function (req, res) {
  res.send(JSON.stringify({
    scripts: [
      {
        id: 1,
        name: "one"
      }
    ]
  }));
});

expressApp.listen(3000);
console.log("Shelly 3 mock server started. MQTT connection to localhost, web server exposed at port 3000");
