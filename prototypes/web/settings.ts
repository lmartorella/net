import * as fs from 'fs';
import * as path from 'path';

var args = process.argv.slice(2);

let homeFolder = args[0];
if (!homeFolder) {
    throw new Error('Missing home folder argument');
}

let csvFolder = path.join(homeFolder, 'DB', 'SAMIL');
if (!fs.existsSync(csvFolder) || !fs.readdirSync(csvFolder)) {
    throw new Error('CSV folder not accessible: ' + csvFolder);
}

let gardenCfgPath = path.join(homeFolder, 'Server', 'gardenCfg.json');
let gardenCfg: string;
if (!fs.existsSync(gardenCfgPath) || !(gardenCfg = fs.readFileSync(gardenCfgPath, 'utf8'))) {
    throw new Error('Garden configuration file not accessible: ' + gardenCfgPath);
}

export { csvFolder, gardenCfg };
