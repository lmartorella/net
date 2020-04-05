const msbuild = new (require('msbuild'))();
const path = require('path');

msbuild.sourcePath = path.join(__dirname, '../home.server.sln');
msbuild.configuration = process.argv[2];
msbuild.config('version','16.0');
msbuild.build(); 
