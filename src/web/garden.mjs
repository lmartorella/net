import { jsonRestRemoteCall } from '../../src/web/mqtt.mjs';

export function register(app, privileged) {
    app.get('/svc/checkLogin', privileged(), (_req, res) => {
        res.status(200).send("OK");
    });

    app.get('/svc/gardenStatus', async (_req, res) => {
        jsonRestRemoteCall(res, "garden/getStatus");
    });
    
    app.get('/svc/gardenCfg', privileged(), (_req, res) => {
        jsonRestRemoteCall(res, "garden/getConfiguration", { });
    });
        
    app.put('/svc/gardenCfg', privileged(), async (req, res) => {
        if ((req.headers["content-type"] || '').startsWith("application/json")) {
            jsonRestRemoteCall(res, "garden/setConfiguration", { config: req.body });
        } else {
            res.status(500);
            res.statusMessage = "Invalid content type";
            res.send("Invalid content type");
        }
    });
}
