import path from 'path';
import express from 'express';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export function register(app) {
    // Redirect to SPA
    app.get('/', (_req, res) => {
        res.redirect('/app/index.html');
    });
    app.use('/app', express.static(path.join(__dirname, '../webapp')));
};
