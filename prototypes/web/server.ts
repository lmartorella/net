import * as express from "express";

var app = express();

app.get('/', (req, res) => {
  res.send('Hello World!');
});

app.listen(80, () => {
  console.log('Webserver started at port 80');
});