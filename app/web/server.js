import express from 'express';
import { createServer } from 'http';
import path from 'path';

import webpack from 'webpack';
import webpackDevMiddleware from 'webpack-dev-middleware';
import webpackHotMiddleware from 'webpack-hot-middleware';
import { Provider } from 'react-redux';

import ReactNative, { AppRegistry } from 'react-native'
import React from 'react';
import { renderToString } from 'react-dom/server';
import { createLocation } from 'history';
import { RouterContext, Router, match } from 'react-router';
import routes from '../routing/routes.js';
import { configureStoreAndHistory } from '../lib/configureStore';
import createHistory from 'react-router/lib/createMemoryHistory';

const config = require('./webpack.config');

const compiler = webpack(config);

const app = express();

app.use((req, res, next) => {

  const location = createLocation(req.originalUrl);

  match({ routes, location }, (error, redirectLocation, renderProps) => {
    if (redirectLocation) return res.redirect(redirectLocation.pathname);
    if (error) return next(error.message);
    if (renderProps == null) return next(error);


		const memoryHistory = createHistory(req.originalUrl);
		const { store, history } = configureStoreAndHistory(memoryHistory);
		const refreshedRoutes = require('../routing/routes').default;

		const App = (props) => { return (<Provider store={store}>
          <Router history={memoryHistory} routes={refreshedRoutes} />
			</Provider>) };

		AppRegistry.registerComponent('App', () => App)

		const { element, stylesheet } = AppRegistry.getApplication('App', { ...renderProps });

    const markup = renderToString(element);
		const style = stylesheet.props.dangerouslySetInnerHTML.__html;
    const html = [
      `<!DOCTYPE html>
			<style id="__react-native-style">${style}</style>
			<meta charset="utf-8">
			<title>Presenter</title>
			<meta name="viewport" content="width=device-width, initial-scale=1">
			<div id="react-root">${markup}</div>
			<script src="/bundle.js"></script>`,
    ].join('');
    res.setHeader('Content-Type', 'text/html');
    return res.send(html);
  });
});

app.use(webpackDevMiddleware(compiler, {
  stats: {
    colors: true,
  },
}));

app.use(webpackHotMiddleware(compiler));

app.use(express.static(path.join(__dirname, 'public')));
const server = createServer(app);
var io = require('socket.io')(server);

io.sockets.on('connection', function (socket) {
  socket.on('navigate', function (data) {
    socket.broadcast.emit('navigate', data);
  });
});

server.listen(3001);
