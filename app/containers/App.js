import React, { PropTypes } from 'react';

import { connect } from 'react-redux';
import { push, pop } from 'react-router-redux';

import { slides, WSURL } from "../constants";
import io from 'socket.io-client/socket.io';

import {
  AppRegistry,
  StyleSheet,
  TouchableHighlight,
  Text, Platform,
  View
} from 'react-native';

import Image from "../lib/Image";
import Button from '../lib/Button';

const image = require('../assets/tech_talks_logo.png');

class App extends React.Component {
  goHome = () => {
    this.props.dispatch(push('/'));
  }
  goToSlide = (name) => {
    return () => {
      this.props.dispatch(push(name));
    }
  }
  componentWillMount = () => {
    this.socket = io(WSURL, {
      jsonp: false, transports: ['websocket']});
    this.socket.on('navigate', (data) => { this.goToSlide(data)() });
  }
  componentWillUnmount = () => {
    this.socket.removeAllListeners("navigate");
  }
  render () {

    return (
			<View style={styles.container}>
        <TouchableHighlight style={styles.touchableBanner} underlayColor="transparent"  onPress={this.goHome}>
          <View style={styles.header}>
            <Image style={styles.logo} source={image} />
          </View>
        </TouchableHighlight>
        {this.props.children}
			</View>
    )
  }
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "white"
  },
  header: {
    backgroundColor: "black",
    padding: 10
  },
  logo: {
    maxWidth: 200,
    maxHeight: 50
  }
});
styles.touchableBanner = Platform.OS === "web" ? { outline: "none" } : undefined;

App.propTypes = {
  children: PropTypes.node
};

export default connect()(App);
