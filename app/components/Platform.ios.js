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
				I can renderz very well on iOS...
      </Text>
    );
  }
}

const styles = StyleSheet.create({
  platform: {
    fontSize: 22,
    textAlign: 'center',
    margin: 10,
  }
});

AppRegistry.registerComponent('Platform', () => Platform);
