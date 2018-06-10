import * as fs from 'fs';
import * as path from 'path';
import * as express from 'express';
import * as passport from 'passport';
import * as compression from 'compression';
import * as passportLocal from 'passport-local';
import { setTimeout } from 'timers';
import { getPvData, getPvChart } from './solar';
import { gardenCfg, logsFolder } from './settings';
import ProcessManager from './procMan';

let pm = new ProcessManager(path.join(__dirname, 'bin'), path.join(__dirname, 'etc'), 'Home.Server.App.exe');

passport.use(new passportLocal.Strategy((username: string, password: string, done: (error: any, user?: any) => void) => { 
    if (username !== 'USER' || password != 'PASSWORD') {
        return done("Bad credentials");
    }
    return done(null, username);
}));

function validateUser(user) {
    return (user === 'USER');
}

// Configure Passport authenticated session persistence.
// In order to restore authentication state across HTTP requests, Passport needs
// to serialize users into and deserialize users out of the session.  The
// typical implementation of this is as simple as supplying the user ID when
// serializing, and querying the user record by ID from the database when
// deserializing.
passport.serializeUser<string, string>((user, cb) => {
    cb(null, user);
});

passport.deserializeUser(function(id, cb) {
    if (!validateUser(id)) {
        return cb('Not logged in');
    }
    cb(null, id);
});

const loginPagePath = '/login';
function ensureLoggedIn() {
    return function(req, res, next) {
      if (!validateUser(req.session && req.session.passport && req.session.passport.user)) {
        if (req.session) {
          req.session.returnTo = req.originalUrl || req.url;
        }
        return res.redirect(401, loginPagePath); // Unauth
      }
      next();
    }
}

var app = express();
app.use((express as any).json());
app.use(compression());
app.use(require('cookie-parser')());
app.use(require('body-parser').urlencoded({ extended: true }));
app.use(require('express-session')({ secret: 'keyboard cat', resave: false, saveUninitialized: false }));

app.set("view engine", "ejs");
app.set('views', __dirname);

app.get('/', (req, res) => {
    res.redirect('/app/index.html');
});

app.get('/r/imm', (req, res) => {
    var pvData = getPvData();
    if (pvData.error) {
        res.send({ error: pvData.error });
    } else {
        res.send(pvData);
    }
});

app.get('/r/powToday', (req, res) => {
    setTimeout(() => {
        res.send(getPvChart(req.query && Number(req.query.day)));
    }, 1000);
});

app.get('/r/gardenCfg', ensureLoggedIn(), (req, res) => {
    res.send(gardenCfg);
});

app.post('/r/gardenStart', ensureLoggedIn(), async (req, res) => {
    let immediate = req.body;
    if (!Array.isArray(immediate) || !immediate.every(v => typeof v === "number") || immediate.every(v => v <= 0)) {
        // Do nothing
        res.status(500);
        res.send("Request incompatible");
        console.log("r/gardenStart: incompatible request: " + JSON.stringify(req.body));
        return;
    }

    let resp = await pm.sendMessage({ command: "garden.setImmediate", immediate });
    res.send(resp);
});

app.post('/r/gardenStop', ensureLoggedIn(), async (req, res) => {
    let resp = await pm.sendMessage({ command: "garden.stop" });
    res.send(resp);
});

app.get('/r/logs', ensureLoggedIn(), (req, res) => {
    // Stream log file
    res.setHeader("Content-Type", "text/plain");
    if (fs.existsSync(path.join(logsFolder, "log.txt"))) {
        fs.createReadStream(path.join(logsFolder, "log.txt")).pipe(res);
    } else {
        res.sendStatus(404);
    }
});

app.get('/r/restart', ensureLoggedIn(), async (req, res) => {
    await pm.restart();
    res.send("Restarted");
});

app.use('/app', express.static('app'));
app.use('/lib/angular', express.static('node_modules/angular'));

app.use(passport.initialize());
app.use(passport.session());

app.get(loginPagePath, (req, res) => res.render('login'));
app.post('/login', passport.authenticate('local', { successRedirect: '/', failureRedirect: loginPagePath }));
app.get('/logout', (req, res) => {
    req.logout();
    res.redirect(loginPagePath);
});

app.listen(80, () => {
  console.log('Webserver started at port 80');
})

pm.start();
