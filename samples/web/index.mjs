
import * as garden from './garden.mjs';
import path from 'path';
import express from 'express';
import { fileURLToPath } from 'url';
import { register as solarRegister } from "../../solar/src/ui/lib/webserver/index.mjs";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export function register(app, privileged, csvFolder) {
    // Redirect to SPA
    app.get('/', (_req, res) => {
        res.redirect('/app/index.html');
    });
    app.use('/app', express.static(path.join(__dirname, '../../target/webapp')));

    garden.register(app, privileged);
    solarRegister(app, csvFolder);
};
