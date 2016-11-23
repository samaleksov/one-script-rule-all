import React, { PropTypes } from 'react';

import {
  AppRegistry,
  StyleSheet,
  Text,
  View
} from 'react-native';

class App extends React.Component {

  componentDidMount = () => {
  }
  componentWillUnmount = () => {

  }
  render () {

    return (
			<View style={styles.container}>
				<Text>Main app containerrrrrrr</Text>
        {this.props.children}
			</View>
    )
  }
}


const styles = StyleSheet.create({
  container: {
    flex: 1
  }
});


App.propTypes = {
  children: PropTypes.node
};

export default App;
