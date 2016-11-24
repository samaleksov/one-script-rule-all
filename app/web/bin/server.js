#!/usr/bin/env node
var fs = require('fs');

var path = require('path');

var babelrc = fs.readFileSync('./.babelrc');
var config;
var rootDir = path.resolve(__dirname, '..');

try {
  config = JSON.parse(babelrc);
  config.plugins.push([ path.join(rootDir, "./lib/requireRewrite"), { "replaceFunc": "./web/lib/replaceModule.js" }]);
  config.plugins.push([ "babel-plugin-webpack-alias", { "config": "./web/webpack.config.js" } ]);
  config.plugins.push(["transform-assets", {
                "extensions": ["svg", "png", "jpg"],
                "name": "[name].[ext]?[sha512:hash:base64:7]",
              }]);

} catch (err) {
  console.error('==>     ERROR: Error parsing your .babelrc.');
  console.error(err);
}
require('babel-register')(config);
require('../server');
