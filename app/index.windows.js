/**
 * Sample React Native App
 * https://github.com/facebook/react-native
 * @flow
 */

import React, { Component } from 'react';

import Welcome from './components/Welcome';
import Main from './components/Main';
import App from './containers/App';

import { Provider } from 'react-redux';

import { configureStoreAndHistory } from './lib/configureStore';

import {
  Header,
  Link,
  Route,
  Router,
  StackRoute,
  withRouter
} from 'react-router-native';

import {
  AppRegistry,
  StyleSheet,
  View
} from 'react-native';

const { store, history } = configureStoreAndHistory();

class app extends Component {
  render() {
    return (
      <Provider store={store}>
        <Router history={history} addressBar>
          <StackRoute path="app" component={App}>
            <Route path="/" component={Welcome} />
            <Route path="/main" component={Main} />
          </StackRoute>
        </Router>
      </Provider>
    );
  }
}


AppRegistry.registerComponent('app', () => app);
