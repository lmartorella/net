import { jsonRestRemoteCall } from '../../src/web/mqtt.mjs';

export function register(app, privileged) {
    app.get('/svc/checkLogin', privileged(), (_req, res) => {
        res.status(200).send("OK");
    });

    app.get('/svc/gardenStatus', async (_req, res) => {
        jsonRestRemoteCall(res, "garden/getStatus");
    });
    
    // app.post('/svc/gardenStart', privileged(), async (req, res) => {
    //     let immediate = req.body;
    //     if (typeof immediate !== "object" || immediate.time <= 0) {
    //         // Do nothing
    //         res.status(500);
    //         res.send("Request incompatible");
    //         logger("r/gardenStart: incompatible request: " + JSON.stringify(req.body));
    //         return;
    //     }
    //     jsonRestRemoteCall(res, "garden/setImmediate", { immediate });
    // });
    
    // app.post('/svc/gardenStop', privileged(), async (_req, res) => {
    //     jsonRestRemoteCall(res, "garden/stop");
    // });
    
    app.get('/svc/gardenCfg', privileged(), (_req, res) => {
        jsonRestRemoteCall(res, "garden/getConfiguration", { });
    });
        
    app.put('/svc/gardenCfg', privileged(), async (req, res) => {
        if ((req.headers["content-type"] || '').startsWith("application/json")) {
            jsonRestRemoteCall(res, "garden/setConfiguration", { config: req.body });
        } else {
            res.status(500).send("Invalid content type");
        }
    });
}
