/**
 * Sample React Native App
 * https://github.com/facebook/react-native
 * @flow
 */

import React, { Component } from 'react';

import Welcome from './components/Welcome';

import {
  AppRegistry,
  StyleSheet,
  View
} from 'react-native';

class app extends Component {
  render() {
    return (
      <View style={styles.container}>
        <Welcome />
      </View>
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
