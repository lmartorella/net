import fs from 'fs';
import path from 'path';
import { etcDir, logger } from '../../src/web/settings.mjs';
import { jsonRemoteCall } from '../../src/web/mqtt.mjs';

const gardenCsvFile = path.join(etcDir, 'DB/GARDEN/garden.csv');

export function register(app, privileged) {
    app.get('/svc/checkLogin', privileged(), (_req, res) => {
        res.status(200).send("OK");
    });

    app.get('/svc/gardenStatus', async (_req, res) => {
        jsonRemoteCall(res, "garden/getStatus");
    });
    
    app.post('/svc/gardenStart', privileged(), async (req, res) => {
        let immediate = req.body;
        if (typeof immediate !== "object" || immediate.time <= 0) {
            // Do nothing
            res.status(500);
            res.send("Request incompatible");
            logger("r/gardenStart: incompatible request: " + JSON.stringify(req.body));
            return;
        }
        jsonRemoteCall(res, "garden/setImmediate", { immediate });
    });
    
    app.post('/svc/gardenStop', privileged(), async (_req, res) => {
        jsonRemoteCall(res, "garden/stop");
    });
    
    app.get('/svc/gardenCfg', privileged(), async (_req, res) => {
        jsonRemoteCall(res, "garden/getConfiguration");
    });
    
    app.put('/svc/gardenCfg', privileged(), async (req, res) => {
        if ((req.headers["content-type"] || '').indexOf("application/json") === 0) {
            jsonRemoteCall(res, "garden/setConfiguration", { config: req.body });
        } else {
            res.sendStatus(500);
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
}
