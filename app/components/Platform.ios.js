import React, { Component } from 'react';

import { connect } from 'react-redux';

import {
  AppRegistry,
  StyleSheet,
  Text
} from 'react-native';

class Platform extends Component {
  render() {
    return (
      <Text style={styles.platform}>
				I can render very well on iOS
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


export default connect()(Platform);
