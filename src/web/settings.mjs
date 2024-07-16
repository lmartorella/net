import path from 'path';
import fs from 'fs';
import { fileURLToPath } from 'url';
const __filename = fileURLToPath(import.meta.url);
export const __dirname = path.dirname(__filename);

export const binDir = path.join(__dirname, '../../target/bin');
export const etcDir = path.join(__dirname, '../../target/etc');

export const webLogsFile = path.join(etcDir, 'webServer.log');
let webServerCfgFile = path.join(etcDir, 'server/webCfg.json');

if (!fs.existsSync(webServerCfgFile)) {
    fs.writeFileSync(webServerCfgFile, JSON.stringify({
        username: "user",
        password: "pa$$word"
    }, null, 3));
}

export const websSettings = JSON.parse(fs.readFileSync(webServerCfgFile, 'utf8'));

export const logger = (msg) => {
    msg = new Date().toISOString() + " " + msg;
    fs.appendFileSync(webLogsFile, msg + '\n');
}

export const processesMd = {
    server: {
        processName: "Home.Notification",
        killTopic: "server/kill",
        frameworkDir: 'net8.0',
        debug: true
    },
    garden: {
        processName: "Home.Garden",
        killTopic: "garden/kill",
        frameworkDir: 'net8.0',
        debug: true
    },
    solar: {
        processName: "Home.Solar",
        killTopic: "solar/kill",
        frameworkDir: 'net7.0'
    },
};
