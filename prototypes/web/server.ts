import * as express from 'express';
import * as path from 'path';
import * as fs from 'fs';

var app = express();
var args = process.argv.slice(2);

var csvFolder = args[0];
if (!csvFolder) {
    throw new Error('Missing CSV folder argument');
}
if (!fs.existsSync(csvFolder) || !fs.readdirSync(csvFolder)) {
    throw new Error('CSV folder not accessible: ' + csvFolder);
}

interface IPvData {
    error?: string;
    currentW?: number;
    totalDayW?: number;
}

interface ICsv {
    rows: any[][];
    colKeys: { [key: string]: number };
}

function parseCsv(path: string): ICsv {
    var content = fs.readFileSync(path, 'utf8');
    var colKeys: { [key: string]: number } = { };
    var rows = content.split('\n').map((line, idx) => {
        var cells = line.split(',');
        if (idx === 0) {
            // Decode keys
            colKeys = cells.reduce((acc, k, i) => {
                acc[k] = i;
                return acc;
            }, colKeys);
        } else {
            return cells.map(cell => {
                if (cell.indexOf(':') > 0) {
                    return new Date(cell);
                } else {
                    return Number(cell);
                }
            });
        }
    });
    return { 
        colKeys, 
        rows
    };
}

function getPvData(): IPvData {
    // Get the latest CSV in the disk
    var files = fs.readdirSync(csvFolder);
    if (files.length === 0) {
        return { error: 'No files found' };
    }

    // Sort it by date
    files = files.sort();

    // Take the last one
    var csv = files[files.length - 1];

    // Now parse it
    var data = parseCsv(path.join(csvFolder, csv));

    var ret: IPvData = { currentW: 0, totalDayW: 0 };
    if (data.rows.length > 1) {
        var lastSample = data.rows[data.rows.length - 1];
        ret.currentW = lastSample[data.colKeys['PowerW']]; 
        ret.totalDayW = lastSample[data.colKeys['EnergyTodayW']]; 
    }
    return ret;
}

function renderPage(pvData: IPvData): string {
    return `Current power: ${pvData.currentW}W, total power today: ${pvData.totalDayW / 1000}kW`; 
}

app.get('/', (req, res) => {
    var pvData = getPvData();
    if (pvData.error) {
        res.send('ERROR: ' + pvData.error);
    } else {
        res.send(renderPage(pvData));
    }
});

app.listen(80, () => {
  console.log('Webserver started at port 80');
});