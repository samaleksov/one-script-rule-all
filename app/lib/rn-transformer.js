'use strict';

const babel = require('babel-core');
const fs = require('fs');


/**
 * This is your `.babelrc` equivalent.
 */
const babelRC = {
  presets: [require('babel-preset-react-native')],
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
