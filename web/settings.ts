import * as fs from 'fs';
import * as path from 'path';

let binDir = path.join(__dirname, 'bin');
let etcDir = path.join(__dirname, 'etc');

let csvFolder = path.join(etcDir, 'DB', 'SAMIL');
if (!fs.existsSync(csvFolder) || !fs.readdirSync(csvFolder)) {
    throw new Error('CSV folder not accessible: ' + csvFolder);
}

let gardenCfgPath = path.join(etcDir, 'Server', 'gardenCfg.json');
let gardenCfg: string;
if (!fs.existsSync(gardenCfgPath) || !(gardenCfg = fs.readFileSync(gardenCfgPath, 'utf8'))) {
    throw new Error('Garden configuration file not accessible: ' + gardenCfgPath);
}

export { csvFolder, gardenCfg, binDir, etcDir };

