import React, { Component } from "react";
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
        <Text style={styles.mainText}>Main Page</Text>
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
    fontSize: 120,
    textAlign: 'center',
    margin: 10,
  }
});

export default Main;
