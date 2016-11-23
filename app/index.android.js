/**
 * Sample React Native App
 * https://github.com/facebook/react-native
 * @flow
 */

import React, { Component } from 'react';

import { Provider } from 'react-redux';

import { configureStoreAndHistory } from './lib/configureStore';

import Welcome from './components/Welcome';
import Main from './components/Main';
import App from './containers/App';

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
        <Router style={styles.container} history={history} addressBar>
          <StackRoute path="app" component={App}>
            <Route path="/" component={Welcome} />
            <Route path="/main" component={Main} />
          </StackRoute>
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
