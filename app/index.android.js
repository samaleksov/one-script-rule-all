/**
 * Sample React Native App
 * https://github.com/facebook/react-native
 * @flow
 */

import React, { Component } from 'react';

import Welcome from './components/Welcome';

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


import routes from "./routing/routes";


import {
  AppRegistry,
  StyleSheet,
  Text,
  View
} from 'react-native';

const { store, history } = configureStoreAndHistory();

export default class app extends Component {
  render() {
    return (
      <Provider store={store}>
        <Router style={styles.container} history={history} routes={routes} addressBar>
        </Router>
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
