import React, { Component } from 'react';

import { Provider } from 'react-redux';
import {
  AppRegistry
} from 'react-native';
import "./userAgent";

import { configureStoreAndHistory } from './lib/configureStore';

import {
  Header,
  Link,
  Route,
  Router,
  StackRoute,
  withRouter
} from 'react-router-native';


import routes from "./routing/routes";

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
