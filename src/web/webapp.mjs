import path from 'path';
import express from 'express';
import { binDir } from './settings.mjs';

export function register(app) {
    // Redirect to SPA
    app.get('/', (_req, res) => {
        res.redirect('/app/index.html');
    });
    app.use('/app', express.static(path.join(binDir, 'webapp')));
};
