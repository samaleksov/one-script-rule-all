import React, { Component } from "react";

import { connect } from 'react-redux';

import {
  AppRegistry,
  StyleSheet,
  Text,
  View
} from 'react-native';

class Main  extends Component {
  render () {
    return (
      <View style={styles.container}>
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
  mainText: {
    fontSize: 20,
    textAlign: 'center',
    margin: 10,
  }
});

export default connect()(Main);
