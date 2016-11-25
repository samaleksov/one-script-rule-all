import React, { Component } from "react";

import { connect } from 'react-redux';
import { push } from 'react-router-redux';
import { slides, WSURL } from "../constants";
import io from 'socket.io-client/socket.io';

import {
  AppRegistry,
  StyleSheet,
  Text,
  View
} from 'react-native';

class Slides  extends Component {
  constructor(props) {
    super(props);
  }
  goToSlide = (name) => {
    return () => {
      this.currentSlide = name;
      this.props.dispatch(push(name));
      // this.socket.emit('navigate', name);
    }
  }
  goToNextSlide = () => {
    const currentIndex = slides.indexOf(this.currentSlide);
    let nextIndex = 0
    if(currentIndex >= 0 && currentIndex + 1 <= slides.length - 1)
    nextIndex = currentIndex + 1

    return () => {
      this.currentSlide = slides[nextIndex];
      this.props.dispatch(push(this.currentSlide));
      // this.socket.emit('navigate', this.currentSlide);
    }
  }
  goToPrevSlide = () => {
    const currentIndex = slides.indexOf(this.currentSlide);
    let nextIndex = slides.length - 1
    if(currentIndex >= 0 && currentIndex - 1 >= 0)
    nextIndex = currentIndex - 1

    return () => {
      this.currentSlide = slides[nextIndex]
      this.props.dispatch(push(this.currentSlide));
      // this.socket.emit('navigate', this.currentSlide);
    }
  }
  render () {
    return (
      <View style={styles.container}>
        { this.props.children }
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

export default connect()(Slides);
