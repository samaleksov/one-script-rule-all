/**
 * Sample React Native App
 * https://github.com/facebook/react-native
 * @flow
 */

import React, { Component } from 'react';
import { Router } from 'react-router';

import Welcome from './components/Welcome';

import { configureStoreAndHistory } from './lib/configureStore';

import { Provider } from 'react-redux';

import routes from "./routing/routes";

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
        <Router history={history} routes={routes} />
      </Provider>
    );
  }
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#F5FCFF',
  },
});

AppRegistry.registerComponent('app', () => app);
AppRegistry.runApplication('app', { rootTag: document.getElementById('react-root') });
