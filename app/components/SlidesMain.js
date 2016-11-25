import React, { Component } from "react";

import { connect } from 'react-redux';

import {
  AppRegistry,
  StyleSheet,
  Text,
  View
} from 'react-native';


import Image from "../lib/Image";
const image = require('../assets/thering.jpg');

class SlidesMain  extends Component {
  render () {
    return (
      <View style={styles.container}>
        <View style={styles.imageContainer}>
          <Image style={styles.image} source={image} />
        </View>
        <Text style={styles.text}>One script to rule them all</Text>
      </View>
    );
  }
}

const styles = StyleSheet.create({
  imageContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#F5FCFF',
  },
  container: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#F5FCFF',
  },
  image: {
    flexGrow: 1
  },
  text: {
    position: "absolute",
    backgroundColor: "black",
    padding: 10,
    top: 150,
    left: 50,
    fontSize: 40,
    fontWeight: "bold",
    color: "white"
  }
});

export default connect()(SlidesMain);
