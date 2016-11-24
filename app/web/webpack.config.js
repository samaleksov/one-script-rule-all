const path = require('path');
const webpack = require('webpack');

module.exports = {
  devServer: {
    contentBase: path.join(__dirname, 'dist')
  },
  entry: [
    "react-hot-loader/patch", path.join(__dirname, '../index.web.js'), 'webpack-hot-middleware/client'
  ],
  module: {
    loaders: [
      {
        test: /\.js$/,
        exclude: /node_modules/,
        loader: 'babel-loader'
      },
      {
        test: /\.(gif|jpe?g|png|svg)$/,
        loader: 'url-loader',
        query: { name: '[name].[hash:16].[ext]' }
      }
    ]
  },
  output: {
    path: path.join(__dirname, 'dist'),
    filename: 'bundle.js'
  },
  plugins: [
		new webpack.NormalModuleReplacementPlugin(/\.js$/, function(resource){
			if(resource.request.indexOf('node_modules') === -1)
			{
				const platformRequest = resource.request.replace(/\.js$/, '.web.js');

				try {
				    fs.accessSync(platformRequest, fs.F_OK);
						resource.request = platformRequest;
				} catch (e) { }
			}
		}),
    new webpack.HotModuleReplacementPlugin(),
    new webpack.DefinePlugin({
      'process.env.NODE_ENV': JSON.stringify(process.env.NODE_ENV || 'development')
    }),
    new webpack.optimize.DedupePlugin(),
    new webpack.optimize.OccurenceOrderPlugin()
  ],
  resolve: {
    alias: {
      'react-native': 'react-native-web'
    }
  },
  devtool: "source-map"
};
