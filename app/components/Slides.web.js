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
    this.socket = io(WSURL, {jsonp: false});
  }
  goToSlide = (name) => {
    return () => {
      this.currentSlide = name;
      this.props.dispatch(push(name));
      this.socket.emit('navigate', name);
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
      this.socket.emit('navigate', this.currentSlide);
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
      this.socket.emit('navigate', this.currentSlide);
    }
  }
  keyDown = (event) => {

    let handled = false;
    if(event.keyCode == 27)
    {
      handled = true;
    }
    if(event.keyCode == 34 || event.keyCode == 39)
    {
      this.goToNextSlide()();
      handled = true;
    }
    else if(event.keyCode == 33 || event.keyCode == 37)
    {
      this.goToPrevSlide()();
      handled = true;
    }
    if(handled)
    {
      event.preventDefault();
      return false;
    }
    return true;
  }
  componentDidMount () {
    window.document.addEventListener("keydown", this.keyDown, false);
  }
	componentWillMount () {
		const routing = this.props.location.pathname;

    if(routing == null)
    {
      this.currentSlide = ""
    }
    else if(routing.length > 1)
    {
      const route = routing[1];
      this.currentSlide = route
    }
	}
  componentWillUnmount () {
    window.document.removeEventListener("keydown", this.keyDown, false);
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
