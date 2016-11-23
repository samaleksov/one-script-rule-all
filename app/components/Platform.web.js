/**
 * Sample React Native App
 * https://github.com/facebook/react-native
 * @flow
 */

import React, { Component } from 'react';

import { connect } from 'react-redux';

import { push } from 'react-router-redux';

import Button from '../lib/Button';

import {
  AppRegistry,
  StyleSheet,
  View,
  Text
} from 'react-native';

class Platform extends Component {
  goToMain = () => {
    this.props.dispatch(push('/main'));
  }
  render() {
    return (
      <View>
        <Text style={styles.platform}>
  				I can render very well on the web
        </Text>
        <Button onPress={ this.goToMain } title="GoToMain" />
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
