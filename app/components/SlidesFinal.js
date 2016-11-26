import React, { Component } from "react";

import { connect } from 'react-redux';

import {
  AppRegistry,
  StyleSheet,
  Text,
  Platform,
  View
} from 'react-native';

class SlidesFinal  extends Component {
  render () {

    return (
      <View style={styles.container}>
        <Text style={styles.text}>Thank You!</Text>
        <Text style={styles.textLink}>https://github.com/samaleksov/one-script-rule-all</Text>
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
  text: {
    padding: 10,
    fontSize: 80,
    fontWeight: "bold",
    color: "black"
  },
  textLink: {
    backgroundColor: Platform.OS === "windows" ? "white" : "black",
    padding: 10,
    fontSize: 30,
    fontWeight: "bold",
    color: Platform.OS === "windows" ? "black" : "white"
  }
});
export default connect()(SlidesFinal);
