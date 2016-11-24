import React, { Component } from 'react';
import {
  AppRegistry,
  StyleSheet,
  View,
  Text
} from 'react-native';

import { connect } from 'react-redux';

import { push } from 'react-router-redux';

import Button from '../lib/Button';

class Platform extends Component {
  goToMain = () => {
    this.props.dispatch(push('/main'));
  }
  render() {
    return (
      <View>
        <Text style={styles.platform}>
  				I can render very well on windows
        </Text>
        <Button onPress={this.goToMain} title="Go to main" accessibilityLabel="Learn more about this purple button" />
      </View>
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

export default connect()(Platform);
