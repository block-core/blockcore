var contents = require('fs').readFileSync('./src/Directory.Build.props', 'utf8');
var version = contents.substring(contents.indexOf('<Version>') + 9, contents.indexOf('</Version>'));
console.log(version);