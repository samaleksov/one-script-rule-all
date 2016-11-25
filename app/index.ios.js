import React, { Component } from 'react';

import Welcome from './components/Welcome';
import App from './containers/App';
import Main from './components/Main';

import {
  AppRegistry,
  StyleSheet,
  Text,
  View
} from 'react-native';

import {
  Header,
  Link,
  Route,
  Router,
  StackRoute,
  withRouter
} from 'react-router-native';

import { Provider } from 'react-redux';

import { configureStoreAndHistory } from './lib/configureStore';

const { store, history } = configureStoreAndHistory();

export default class app extends Component {
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

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#F5FCFF',
  },
  instructions: {
    textAlign: 'center',
    color: '#333333',
    marginBottom: 5,
  },
});

AppRegistry.registerComponent('app', () => app);
