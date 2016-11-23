#!/usr/bin/env node
var fs = require('fs');

var path = require('path');

var babelrc = fs.readFileSync('./.babelrc');
var config;
var rootDir = path.resolve(__dirname, '..');
console.log(rootDir);
try {
  config = JSON.parse(babelrc);
  config.plugins.push([ path.join(rootDir, "./lib/requireRewrite"), { "replaceFunc": "./web/lib/replaceModule.js" }]);
  config.plugins.push([ "babel-plugin-webpack-alias", { "config": "./web/webpack.config.js" } ]);

} catch (err) {
  console.error('==>     ERROR: Error parsing your .babelrc.');
  console.error(err);
}

require('babel-register')(config);
require('../server');

/*

/**
 * Define isomorphic constants.
 */


/*
global.__CLIENT__ = false;
global.__SERVER__ = true;
global.__DISABLE_SSR__ = false;  // <----- DISABLES SERVER SIDE RENDERING FOR ERROR DEBUGGING
global.__DEVELOPMENT__ = process.env.NODE_ENV !== 'production';

if (__DEVELOPMENT__) {
  if (!require('piping')({
      hook: true,
      ignore: /(\/\.|~$|\.json|\.scss$)/i
    })) {
    return;
  }
}

// https://github.com/halt-hammerzeit/webpack-isomorphic-tools
var WebpackIsomorphicTools = require('webpack-isomorphic-tools');
global.webpackIsomorphicTools = new WebpackIsomorphicTools(require('../webpack-isomorphic-tools'))
  .server(rootDir, function() {
    require('../server');
});
*/
