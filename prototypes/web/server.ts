import * as express from 'express';
import * as passport from 'passport';
import * as passportLocal from 'passport-local';
import { setTimeout } from 'timers';
import { getPvData, getPvChart } from './solar';
import { gardenCfg } from './settings';
import { sendToServer } from './garden';

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
app.use(express.json());
app.use(require('morgan')('combined'));
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

app.post('/r/gardenStart', ensureLoggedIn(), (req, res) => {
    let immediate = req.body;
    if (!Array.isArray(immediate) || !immediate.every(v => typeof v === "number") || immediate.every(v => v <= 0)) {
        // Do nothing
        res.status(500);
        res.send("Request incompatible");
        console.log("r/gardenStart: incompatible request: " + JSON.stringify(req.body));
        return;
    }

    sendToServer(JSON.stringify({ command: "setImmediate", immediate }) + '\r\n', resp => {
        res.send(resp);
    })
});

app.post('/r/gardenStop', ensureLoggedIn(), (req, res) => {
    sendToServer(JSON.stringify({ command: "stop" }) + '\r\n', resp => {
        res.send(resp);
    })
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
