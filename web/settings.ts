import * as fs from 'fs';
import * as path from 'path';

let binDir = path.join(__dirname, 'bin');
let etcDir = path.join(__dirname, 'etc');

let csvFolder = path.join(etcDir, 'DB', 'SAMIL');
if (!fs.existsSync(csvFolder) || !fs.readdirSync(csvFolder)) {
    throw new Error('CSV folder not accessible: ' + csvFolder);
}

let logsFile = path.join(etcDir, 'log.txt');
let gardenCfgFile = path.join(etcDir, 'Server', 'gardenCfg.json');

export { csvFolder, binDir, etcDir, logsFile, gardenCfgFile };

