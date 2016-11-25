import React, { Component } from 'react';
import {
  AppRegistry
} from 'react-native';
import "./userAgent";
import routes from "./routing/routes";


import {
  Router
} from 'react-router-native';

import { Provider } from 'react-redux';

import { configureStoreAndHistory } from './lib/configureStore';

const { store, history } = configureStoreAndHistory();
const renderedRoutes = routes();

export default class app extends Component {
  render() {
    return (
      <Provider store={store}>
        <Router history={history}>
            { renderedRoutes }
        </Router>
      </Provider>
    );
  }
}
AppRegistry.registerComponent('app', () => app);
