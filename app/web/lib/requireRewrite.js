'use strict';

Object.defineProperty(exports, "__esModule", {
    value: true
});

var _path = require('path');
var _fs = require("fs");
function getReplaceFunc() {


	return function (originalPath, callingFileName, options) {

			if(originalPath.indexOf('node_modules') === -1 && originalPath.startsWith('.'))
			{
				const withExtension = originalPath.endsWith('.js') ? originalPath : originalPath + '.js';
				const platformRequest = withExtension.replace(/\.js$/, '.web.js');
				try {
						var onlyPath = _path.dirname(callingFileName);
						var testName = _path.join(onlyPath, platformRequest);
						_fs.accessSync(testName, _fs.F_OK);
						return platformRequest;
				} catch (e) {
				}
			}
			return originalPath;
	};

}

exports.default = function (_ref2, a, b) {
    var t = _ref2.types;

    var cachedReplaceFunction = void 0;

    function mapModule(source, file, state) {
        var opts = state.opts;

        if (!cachedReplaceFunction) {
            cachedReplaceFunction = getReplaceFunc(opts);
        }
        var replace = cachedReplaceFunction;
        var result = replace(source, file, opts);

        if (result !== source) {
            return result;
        } else {
            return;
        }
    }

    function transformRequireCall(nodePath, state) {
        if (!t.isIdentifier(nodePath.node.callee, { name: 'require' }) && !(t.isMemberExpression(nodePath.node.callee) && t.isIdentifier(nodePath.node.callee.object, { name: 'require' }))) {
            return;
        }

        var moduleArg = nodePath.node.arguments[0];
        if (moduleArg && moduleArg.type === 'StringLiteral') {
            var modulePath = mapModule(moduleArg.value, state.file.opts.filename, state);
            if (modulePath) {
                nodePath.replaceWith(t.callExpression(nodePath.node.callee, [t.stringLiteral(modulePath)]));
            }
        }
    }

    function transformImportCall(nodePath, state) {
        var moduleArg = nodePath.node.source;
        if (moduleArg && moduleArg.type === 'StringLiteral') {
            var modulePath = mapModule(moduleArg.value, state.file.opts.filename, state);
            if (modulePath) {
                nodePath.replaceWith(t.importDeclaration(nodePath.node.specifiers, t.stringLiteral(modulePath)));
            }
        }
    }

    return {
        visitor: {
            CallExpression: {
                exit: function exit(nodePath, state) {
                    return transformRequireCall(nodePath, state);
                }
            },
            ImportDeclaration: {
                exit: function exit(nodePath, state) {
                    return transformImportCall(nodePath, state);
                }
            }
        }
    };
};
