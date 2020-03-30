const msbuild = new (require('msbuild'))();
const path = require('path');

msbuild.sourcePath = path.join(__dirname, '../home.server.sln');
msbuild.configuration = 'Release';
msbuild.config('version','16.0');
msbuild.build(); 
