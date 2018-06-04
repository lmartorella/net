import * as express from 'express';
import { setTimeout } from 'timers';
import { getPvData, getPvChart } from './solar';
import { gardenCfg } from './settings';
import { sendToServer } from './garden';

var app = express();

app.use(express.json());


app.get('/', (req, res) => {
    res.redirect('/app/index.html');
});
app.get('/r/imm', (req, res) => {
    var pvData = getPvData();
    if (pvData.error) {
        res.send({ error: pvData.error });
    } else {
        res.send(pvData);
    }
});
app.get('/r/powToday', (req, res) => {
    setTimeout(() => {
        res.send(getPvChart(req.query && Number(req.query.day)));
    }, 1000);
});

app.get('/r/gardenCfg', (req, res) => {
    res.send(gardenCfg);
});

app.post('/r/gardenStart', (req, res) => {
    let immediate = req.body;
    if (!Array.isArray(immediate) || !immediate.every(v => typeof v === "number") || immediate.every(v => v <= 0)) {
        // Do nothing
        res.status(500);
        res.send("Request incompatible");
        console.log("r/gardenStart: incompatible request: " + JSON.stringify(req.body));
        return;
    }

    sendToServer(JSON.stringify({ command: "setImmediate", immediate }) + '\r\n', resp => {
        res.send(resp);
    })
});

app.post('/r/gardenStop', (req, res) => {
    sendToServer(JSON.stringify({ command: "stop" }) + '\r\n', resp => {
        res.send(resp);
    })
});

app.use('/app', express.static('app'));

app.listen(80, () => {
  console.log('Webserver started at port 80');
});