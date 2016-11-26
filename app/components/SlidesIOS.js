import React, { Component } from "react";

import { connect } from 'react-redux';

import {
  AppRegistry,
  StyleSheet,
  Text,
  View,
  Platform
} from 'react-native';


import Image from "../lib/Image";
const image = require('../assets/ios.png');

class SlidesIOS  extends Component {
  render () {
    return (
      <View style={styles.container}>
        <View style={styles.imageContainer}>
          <Image style={styles.image} source={image} />
        </View>
        <Text style={styles.text}>NSVeryLongMethodNamesBecauseWeHaveNoOverloading</Text>
      </View>
    );
  }
}

const styles = StyleSheet.create({
  imageContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: 'white',
  },
  container: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: 'white',
  },
  image: {
    flexGrow: 1
  },
  text: {
    position: "absolute",
    backgroundColor: "black",
    padding: 10,
    top: 20,
    left: -100,
    fontSize: 40,
    fontWeight: "bold",
    color: Platform.OS === "windows" ? "black" : "white"
  }
});

export default connect()(SlidesIOS);
