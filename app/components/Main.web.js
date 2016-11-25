import React from "react"

import { connect } from 'react-redux';
import { StyleSheet, View } from 'react-native';

class Main  extends React.Component {
  render () {
    const textStyle = StyleSheet.flatten([styles.mainText]);

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
