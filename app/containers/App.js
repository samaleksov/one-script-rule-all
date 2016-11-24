import React, { PropTypes } from 'react';

import {
  AppRegistry,
  StyleSheet,
  Text,
  Image,
  View
} from 'react-native';


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
				<Text>Main app container</Text>
        {this.props.children}
			</View>
    )
  }
}


const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "black"
  },
  logo: {
    width: 279,
    height: 74
  }
});


App.propTypes = {
  children: PropTypes.node
};

export default App;
