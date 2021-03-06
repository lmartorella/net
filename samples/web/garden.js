const fs = require('fs');
const path = require('path');
const { etcDir } = require('../../src/web/settings');
const procMan = require('../../src/web/procMan');

const gardenCfgFile = path.join(etcDir, 'server/gardenCfg.json');
const gardenCsvFile = path.join(etcDir, 'DB/GARDEN/garden.csv');

function register(app, privileged) {
    app.get('/svc/gardenStatus', async (_req, res) => {
        res.send(await procMan.sendMessage("GardenWebRequest", { command: "garden.getStatus" }));
    });
    
    app.post('/svc/gardenStart', privileged(), async (req, res) => {
        let immediate = req.body;
        if (typeof immediate !== "object" || immediate.time <= 0) {
            // Do nothing
            res.status(500);
            res.send("Request incompatible");
            console.log("r/gardenStart: incompatible request: " + JSON.stringify(req.body));
            return;
        }
        res.send(await procMan.sendMessage("GardenWebRequest", { command: "garden.setImmediate", immediate }));
    });
    
    app.post('/svc/gardenStop', privileged(), async (_req, res) => {
        res.send(await procMan.sendMessage("GardenWebRequest", { command: "garden.stop" }));
    });
    
    app.get('/svc/gardenCfg', privileged(), async (_req, res) => {
        // Stream config file
        const stream = fs.existsSync(gardenCfgFile) && fs.createReadStream(gardenCfgFile);
        if (stream) {
            res.setHeader("Content-Type", "application/json");
            stream.pipe(res);
        } else {
            res.sendStatus(404);
        }
    });
    
    app.get('/svc/gardenCsv', privileged(), async (_req, res) => {
        // Stream csv file
        const stream = fs.existsSync(gardenCsvFile) && fs.createReadStream(gardenCsvFile);
        if (stream) {
            res.setHeader("Content-Type", "text/csv");
            stream.pipe(res);
        } else {
            res.sendStatus(404); 
        }
    });
    
    app.put('/svc/gardenCfg', privileged(), async (req, res) => {
        if (req.headers["content-type"] === "application/octect-stream") {
            // Stream back config as file
            fs.writeFileSync(gardenCfgFile, req.body);
            res.sendStatus(200);
        } else if ((req.headers["content-type"] || '').indexOf("application/json") === 0) {
            res.send(await procMan.sendMessage("GardenWebRequest", { command: "garden.setConfig", config: req.body }));
        } else {
            res.sendStatus(500);
        }
    });    

    // Init default gardenCfg.json file (before starting the native server)
    if (!fs.existsSync(gardenCfgFile)) {
        // Example of configuration (without programs)
        fs.writeFileSync(gardenCfgFile, JSON.stringify({
            "zones": ["Grass (North)", "Grass (South)", "Flowerbeds"]
        }, null, 3));
    } 
}

module.exports = { register };