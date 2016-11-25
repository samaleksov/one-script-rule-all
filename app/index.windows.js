
import React, { Component } from 'react';
import { Provider } from 'react-redux';
import {
  Router
} from 'react-router-native';

import "./userAgent";

import routes from "./routing/routes";
import { configureStoreAndHistory } from './lib/configureStore';



import {
  AppRegistry
} from 'react-native';
const { store, history } = configureStoreAndHistory();
const renderedRoutes = routes();

class app extends Component {
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
