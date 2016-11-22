/**
 * Sample React Native App
 * https://github.com/facebook/react-native
 * @flow
 */

import React, { Component } from 'react';
import {
  AppRegistry,
  StyleSheet,
  Text
} from 'react-native';

export default class Platform extends Component {
  render() {
    return (
      <Text style={styles.platform}>
				I can render very well on Android.
      </Text>
    );
  }
}

const styles = StyleSheet.create({
  platform: {
    fontSize: 20,
    textAlign: 'center',
    margin: 10,
  }
});

AppRegistry.registerComponent('Platform', () => Platform);
