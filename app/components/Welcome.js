import React, { Component } from 'react';

import { connect } from 'react-redux';

import {
  AppRegistry,
  StyleSheet,
  Text,
  View
} from 'react-native';

import Platform from './Platform';

class Welcome extends Component {
  render() {
    return (
      <View style={styles.container}>
        <Text style={styles.welcome}>
					Welcome, I'm a shared component
        </Text>
        <Platform />
      </View>
    );
  }
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: 'white',
  },
  welcome: {
    fontSize: 40,
    textAlign: 'center',
    margin: 10,
  }
});

export default connect()(Welcome);
