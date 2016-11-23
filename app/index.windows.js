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

import routes from "./routing/routes";

const { store, history } = configureStoreAndHistory();

class app extends Component {
  render() {


    const HomeHeader = withRouter((props) => {
      const handleRightButtonPress = () => {
        props.router.push('/detail/gray');
      };

      return (
        <Header
          {...props}
          style={{ backgroundColor: '#26BBE5' }}
          title="Welcome"
          rightButtonText="Gray"
          onRightButtonPress={handleRightButtonPress}
        />
      );
    });


    const Home2Header = withRouter((props) => {
      const handleRightButtonPress = () => {
        props.router.push('/detail/gray');
      };

      return (
        <Header
          {...props}
          style={{ backgroundColor: '#26BBE5' }}
          title="Main"
          rightButtonText="Gray"
          onRightButtonPress={handleRightButtonPress}
        />
      );
    });

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
