import bodyParser from 'body-parser';
import compression from 'compression';
import cookieParser from 'cookie-parser';
import express from 'express';
import expressSession from 'express-session';
import fs from 'fs';
import passport from 'passport';
import passportLocal from 'passport-local';
import path from 'path';
import process from 'process';
import { webLogsFile, websSettings, __dirname, logger, processesMd } from './settings.mjs';
import * as samples from '../../samples/web/index.mjs';

const hasProcessManager = process.argv.indexOf("--no-proc-man") < 0;

passport.use(new passportLocal.Strategy((username, password, done) => { 
    if (username !== websSettings.username || password !== websSettings.password) {
        return done("Bad credentials");
    }
    return done(null, username);
}));

function validateUser(user) {
    return (user === websSettings.username);
}

// Configure Passport authenticated session persistence.
// In order to restore authentication state across HTTP requests, Passport needs
// to serialize users into and deserialize users out of the session.  The
// typical implementation of this is as simple as supplying the user ID when
// serializing, and querying the user record by ID from the database when
// deserializing.
passport.serializeUser((user, cb) => {
    cb(null, user);
});

passport.deserializeUser(function(id, cb) {
    if (!validateUser(id)) {
        return cb('Not logged in');
    }
    cb(null, id);
});

const ensureLoggedIn = () => {
    return function(req, res, next) {
      if (!validateUser(req.session && req.session.passport && req.session.passport.user)) {
        if (req.session) {
          req.session.returnTo = req.originalUrl || req.url;
        }
        return res.sendStatus(401); // Unauth
      }
      next();
    }
}

const app = express();
app.use(express.json());
app.use(compression());
app.use(cookieParser());
app.use(bodyParser.urlencoded({ extended: true }));
app.use(bodyParser.raw({ type: "application/octect-stream" }));
const secret = process.env["EXPRESS_SECRET"];
if (!secret) {
    throw new Error("You need to set the `EXPRESS_SECRET` environment variable");
}
app.use(expressSession({ secret, resave: false, saveUninitialized: false }));

// Register custom endpoints
samples.register(app, ensureLoggedIn, path.join(__dirname, "../../target/etc/Db/SOLAR"));

app.use(passport.initialize());
app.use(passport.session());

app.post('/login', passport.authenticate('local'), (req, res) => {
    res.status(200).send("OK");
});
app.get('/logout', (req, res) => {
    req.logout();
    res.sendStatus(401);
});

const processes = {
    web: {
        // no kill, start, etc..
        logFile: webLogsFile
    }
};

if (hasProcessManager) {
    app.get('/svc/logs/:id', ensureLoggedIn(), (req, res) => {
        const file = processes[req.params.id]?.logFile;
        if (file && fs.existsSync(file)) {
            // Stream log file
            res.setHeader("Content-Type", "text/plain");
            fs.createReadStream(file).pipe(res);
        } else {
            res.sendStatus(404);
        }
    });

    app.get('/svc/halt/:id', ensureLoggedIn(), async (req, res) => {
        const proc = processes[req.params.id];
        if (proc?.kill) {
            proc.kill(res);
        } else {
            res.sendStatus(404);
        }
    });

    app.get('/svc/start/:id', ensureLoggedIn(), async (req, res) => {
        const proc = processes[req.params.id];
        if (proc?.start) {
            proc.start(res);
        } else {
            res.sendStatus(404);
        }
    });

    app.get('/svc/restart/:id', ensureLoggedIn(), async (req, res) => {
        const proc = processes[req.params.id];
        if (proc?.restart) {
            proc.restart(res);
        } else {
            res.sendStatus(404);
        }
    });
}

app.listen(80, () => {
  logger('Webserver started at port 80');
})

if (hasProcessManager) {
    const runProcesses = async () => {
        const { ManagedProcess } = await import('./procMan.mjs');
        Object.keys(processesMd).forEach(procId => {
            processes[procId] = new ManagedProcess(processesMd[procId]);
            processes[procId].start();
        });
    };
    void runProcesses();
}
