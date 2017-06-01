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
    currentTs?: string;
    totalDayWh?: number;
    peakW?: number;
    peakTs?: string;
    totalKwh?: number;
    mode?: number;
    fault?: string;
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
            return null;
        } else {
            return cells.map((cell, i) => {
                if (i === 0) {
                    // First column should be a time. If not, nullify the whole row (e.g. csv headers)
                    return (cell.indexOf(':') > 0) && cell;
                } else {
                    // Other columns are number
                    return Number(cell);
                }
            });
        }
    }).filter(row => row && row[0]);
    return { 
        colKeys, 
        rows
    };
}

function findPeak(csv: ICsv, colKey: string): any[] {
    var peakRow = csv.rows[0];
    var idx = csv.colKeys[colKey];
    csv.rows.forEach(row => {
        if (row[idx] > peakRow[idx]) {
            peakRow = row;
        }
    });
    return peakRow;
}

function decodeFault(fault: number): string {
    switch (fault) { 
        case 2048:
            return "Mancanza rete";
    }
    return fault.toString();
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

    var ret: IPvData = { currentW: 0 };
    if (data.rows.length > 1) {
        var lastSample = data.rows[data.rows.length - 1];
        ret.currentW = lastSample[data.colKeys['PowerW']]; 
        ret.currentTs = lastSample[data.colKeys['TimeStamp']] + ' ' + csv.replace('.csv', ''); 
        ret.totalDayWh = lastSample[data.colKeys['EnergyTodayWh']]; 
        ret.totalKwh = lastSample[data.colKeys['TotalEnergyKWh']]; 
        ret.mode = lastSample[data.colKeys['Mode']];
        ret.fault = decodeFault(lastSample[data.colKeys['Fault']]);

        // Find the peak power
        var peakPow = findPeak(data, 'PowerW');
        ret.peakW = peakPow[data.colKeys['PowerW']];
        ret.peakTs = peakPow[data.colKeys['TimeStamp']];
    }
    return ret;
}

function renderPage(pvData: IPvData): string {

    var firstLine: string;
    var firstLineStyle: string;
    switch (pvData.mode) {
        case 0:
            firstLine = 'OFF';
            firstLineStyle = `style="color: gray"`;
            break;
        case 1:
            firstLine = `Potenza attuale: ${pvData.currentW}W`;
            break;
        case 2:
            firstLine = `ERRORE: ${pvData.fault}`;
            firstLineStyle = `style="color: red"`;
            break;
        default:
            firstLine = `Errore: modalità sconosciuta: ${pvData.mode}`;
            firstLineStyle = `style="color: darkred"`;
            break;
    }

    return `
<html>
<head>
  <title>Solar &#9728</title>
  <meta name="viewport" content="width=device-width, initial-scale=1">
</head>
<body>
  <p ${firstLineStyle}> ${firstLine} </p>
  <p style="visibility: ${pvData.totalDayWh ? 'visible' : 'hidden'}"> Energia totale oggi: <b>${pvData.totalDayWh / 1000}kWh</b> </p>
  <p style="visibility: ${pvData.peakW ? 'visible' : 'hidden'}"> Picco di ${pvData.peakW}W alle ${pvData.peakTs} </p>
  <br/>
  <p style="visibility: ${pvData.totalKwh ? 'visible' : 'hidden'}"> Energia totale generata: ${pvData.totalKwh}kWh </p>
  <p style="bottom: 0; position: absolute; font-size: 0.8rem; visibility: ${pvData.currentTs ? 'visible' : 'hidden'}"> 
    Aggiornato a: ${pvData.currentTs} 
  </p>
</body>
</html>
`; 
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