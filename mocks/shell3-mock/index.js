import express from 'express';
import mqtt from "mqtt";

const mqttClient = mqtt.connect("mqtt://localhost", { will: { topic: "shelly3-mock/status/sys", payload: Buffer.alloc(0), }});

mqttClient.publish("shelly3-mock/status/sys", JSON.stringify({ mac: "123456" }), { retain: true });
mqttClient.publish("shelly3-mock/status/switch:0", JSON.stringify({ id: 0, output: false }), { retain: true });
mqttClient.publish("shelly3-mock/status/switch:1", JSON.stringify({ id: 1, output: false }), { retain: true });
mqttClient.publish("shelly3-mock/status/switch:2", JSON.stringify({ id: 2, output: false }), { retain: true });

const expressApp = express();
expressApp.use(express.json()); 

const scriptsMd = [
  {
    id: 0,
    name: "first",
    enable: false,
    running: false
  }
];

const scriptCode = [
  "<empty>"
];

const createScript = name => {
  if (scriptsMd.some(script => script.name === name)) {
    throw new Error("Script with the same name already exists");
  }
  const script = {
    id: scriptsMd.length,
    name,
    enable: false,
    running: false
  };
  scriptsMd.push(script);
  return script;
};

expressApp.get('/rpc/Script.List', function (req, res) {
  res.send(JSON.stringify({ scripts: scriptsMd }));
});

expressApp.post('/rpc/Script.Create', function (req, res) {
  const { name } = req.body;
  if (!name) {
    res.status(500);
    res.statusMessage = "Missing name";
  } else {
    try {
      const { id } = createScript(name);
      res.send(JSON.stringify({ id }));
    } catch (err) { 
      res.status(500);
      res.statusMessage = `EXC: ${err.message}`;
    }
  }
});

const setScriptCode = (id, code, append) => {
  if (id >= scriptCode.length) {
    throw new Error("ID not found");
  }
  if (append) {
    scriptCode[id] = (scriptCode[id] || "") + code;
  } else {
    scriptCode[id] = code;
  }
  return getScriptCode(id);
}

const getScriptCode = (id) => {
  if (id >= scriptCode.length) {
    throw new Error("ID not found");
  }
  return { data: scriptCode[id] };
}

expressApp.post('/rpc/Script.PutCode', function (req, res) {
  const { id, code, append } = req.body;
  if (!id || !code) {
    res.status(500);
    res.statusMessage = "Missing args";
  } else {
    try {
      const { data } = setScriptCode(id, code, append);
      res.send(JSON.stringify({ len: data.length }));
    } catch (err) { 
      res.status(500);
      res.statusMessage = `EXC: ${err.message}`;
    }
  }
});

expressApp.post('/rpc/Script.GetCode', function (req, res) {
  const { id, offset, len } = req.body;
  if (!id) {
    res.status(500);
    res.statusMessage = "Missing args";
  } else if (!!offset || !!len) {
    res.status(500);
    res.statusMessage = "Not supported";
  } else {
    try {
      const { data } = getScriptCode(id);
      return { data, left: 0 };
    } catch (err) { 
      res.status(500);
      res.statusMessage = `EXC: ${err.message}`;
    }
  }
});

expressApp.listen(3000);
console.log("Shelly 3 mock server started. MQTT connection to localhost, web server exposed at port 3000");
