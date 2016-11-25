import React, { PropTypes } from 'react';

import { connect } from 'react-redux';
import { push, pop } from 'react-router-redux';

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
  componentDidMount = () => {
  }
  componentWillUnmount = () => {
  }
  render () {

    return (
			<View style={styles.container}>
        <TouchableHighlight style={styles.touchableBanner} underlayColor="transparent"  onPress={this.goHome}>
          <View>
            <Image style={styles.logo} source={image} />
          </View>
        </TouchableHighlight>
				<Text>Main app containers</Text>
        {this.props.children}
			</View>
    )
  }
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "grey"
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
