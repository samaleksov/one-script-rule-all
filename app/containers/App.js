import React, { PropTypes } from 'react';

import {
  AppRegistry,
  StyleSheet,
  Text,
  View
} from 'react-native';

import Image from "../lib/Image";

const image = require('../assets/tech_talks_logo.png');


class App extends React.Component {

  componentDidMount = () => {
  }
  componentWillUnmount = () => {

  }
  render () {

    return (
			<View style={styles.container}>
        <Image style={styles.logo} source={image} /> 
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
    maxWidth: 279,
    maxHeight: 74
  }
});


App.propTypes = {
  children: PropTypes.node
};

export default App;
