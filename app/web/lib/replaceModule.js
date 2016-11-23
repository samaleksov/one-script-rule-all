const fs = require("fs");

export default function (originalPath, callingFileName, options) {
		if(originalPath.indexOf('node_modules') === -1)
		{
			const platformRequest = originalPath.replace(/\.js$/, '.web.js');

			try {
					fs.accessSync(platformRequest, fs.F_OK);
					return platformRequest;
			} catch (e) {
			}
		}
		return originalPath;
};
