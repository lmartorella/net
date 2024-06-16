import express from 'express';
import mqtt from "mqtt";

const topicRoot = "shelly3-mock";

const mqttClient = mqtt.connect({ host: "127.0.0.1", will: { topic: `${topicRoot}/status/sys`, payload: Buffer.alloc(0), }});

mqttClient.publish(`${topicRoot}/status/sys`, JSON.stringify({ mac: "123456" }), { retain: true });

const outputs = [false, false, false];
const publishOutput = id => {
  mqttClient.publish(`${topicRoot}/status/switch:${id}`, JSON.stringify({ id, output: outputs[id] }), { retain: true });
};

for (let id = 0; id < 3; id++) {
  publishOutput(id);
}

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
      console.log(`Set script ${id} with:`);
      console.log(JSON.stringify(JSON.parse(data), null, "  "));
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

console.log("Press 1, 2 and 3 to simulate switch of the output, space to resend state, and q to quit");

const stdin = process.stdin;
stdin.setRawMode( true );
stdin.resume();
stdin.setEncoding('utf8');
stdin.on('data', key => {
  // ctrl-c ( end of text )
  if (key === '\u0003' || key === 'q') {
    process.exit();
  }
  switch (key) {
    case '1': switchOut(0); break;
    case '2': switchOut(1); break;
    case '3': switchOut(2); break;
    case ' ': resendState(); break;
  }
});

const switchOut = id => {
  outputs[id] = !outputs[id];
  publishOutput(id);
  console.log(`Output ${id} switched to ${outputs[id]}`);
};

const resendState = id => {
  for (let id = 0; id < 3; id++) {
    console.log(`Resend output ${id}: to ${outputs[id]}`);
    publishOutput(id);
  }
};
