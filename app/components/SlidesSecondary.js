import React, { Component } from "react";

import { connect } from 'react-redux';

import {
  AppRegistry,
  StyleSheet,
  Text,
  View
} from 'react-native';

import Image from "../lib/Image";
const image = require('../assets/thering2.jpg');

class SlidesSecondary  extends Component {
  render () {
    return (
      <View style={styles.container}>
        <Image style={styles.image} source={image} />
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
  image: {
    flexGrow: 1
  }
});

export default connect()(SlidesSecondary);
