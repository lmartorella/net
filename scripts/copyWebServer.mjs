import fs from "fs";
import path from "path";
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
export const __dirname = path.dirname(__filename);

const src = path.join(__dirname, "../src/web");
const files = fs.readdirSync(src);
for (const file of files) {
    if (fs.lstatSync(path.join(src, file)).isFile()) {
        fs.cpSync(path.join(src, file), path.join(__dirname, "../target/bin/web", file));
    }
}
