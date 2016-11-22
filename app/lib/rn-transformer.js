'use strict';

const babel = require('babel-core');
const fs = require('fs');


/**
 * This is your `.babelrc` equivalent.
 */
const babelRC = {
  presets: [require('babel-preset-react-native')],
  plugins: [
    // The following plugin will rewrite imports. Reimplementations of node
    // libraries such as `assert`, `buffer`, etc. will be picked up
    // automatically by the React Native packager.  All other built-in node
    // libraries get rewritten to their browserify counterpart.
    [require('babel-plugin-rewrite-require'), {
      aliases: {
        constants: 'constants-browserify',
        crypto: 'crypto-browserify',
        dns: 'node-libs-browser/mock/dns',
        domain: 'domain-browser',
        fs: 'node-libs-browser/mock/empty',
        http: 'http-browserify',
        https: 'https-browserify',
        net: 'node-libs-browser/mock/net',
        os: 'os-browserify/browser',
        path: 'path-browserify',
        querystring: 'querystring-es3',
        stream: 'stream-browserify',
        _stream_duplex: 'readable-stream/duplex',
        _stream_passthrough: 'readable-stream/passthrough',
        _stream_readable: 'readable-stream/readable',
        _stream_transform: 'readable-stream/transform',
        _stream_writable: 'readable-stream/writable',
        sys: 'util',
        timers: 'timers-browserify',
        tls: 'node-libs-browser/mock/tls',
        tty: 'tty-browserify',
        vm: 'vm-browserify',
        zlib: 'browserify-zlib',

        // You can add your own, much like webpack aliases:
        'corporate-lib': 'corporate-lib-react-native',
      },
      throwForNonStringLiteral: true,
    }],
  ],
};

function transform(src, filename, options) {
	let finalFileName = filename;

	if(filename.indexOf('node_modules') === -1)
	{
		const platformFileName = filename.replace(/\.js$/, `.${options.platform}.js`);

		try {
		    fs.accessSync(path, fs.F_OK);
				finalFileName = platformFileName;
		} catch (e) { }

	}

  const babelConfig = Object.assign({}, babelRC, {
    filename: finalFileName,
    sourceFileName: finalFileName,
  });
  const result = babel.transform(src, babelConfig);
  return {
    ast: result.ast,
    code: result.code,
    map: result.map,
    filename: finalFileName,
  };
}

module.exports = function(data, callback) {
  let result;
  try {
    result = transform(data.sourceCode, data.filename, data.options);
  } catch (e) {
    callback(e);
    return;
  }
  callback(null, result);
};
