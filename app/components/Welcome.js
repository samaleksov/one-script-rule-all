import React, { Component } from 'react';

import { connect } from 'react-redux';

import {
  AppRegistry,
  StyleSheet,
  Text,
  View,
  ScrollView
} from 'react-native';

import Platform from './Platform';

class Welcome extends Component {
  render() {
    return (
      <ScrollView>
        <View style={styles.container}>
          <View style={styles.tile}>
            <Text style={styles.welcome}>
              Slide 1
            </Text>
          </View>
          <View style={styles.tile2}>
            <Text style={styles.welcome}>
    					Slide 2
            </Text>
          </View>
          <View style={styles.tile3}>
            <Text style={styles.welcome}>
    					Slide 3
            </Text>
          </View>
          <View style={styles.tile4}>
            <Text style={styles.welcome}>
              Slide 4
            </Text>
          </View>
          <View style={styles.tile5}>
            <Text style={styles.welcome}>
              Slide 5
            </Text>
          </View>
          <View style={styles.tile6}>
            <Text style={styles.welcome}>
              Slide 6
            </Text>
          </View>
          <View style={styles.tile7}>
            <Text style={styles.welcome}>
              Slide 7
            </Text>
          </View>
          <View style={styles.tile8}>
            <Text style={styles.welcome}>
              Slide 8
            </Text>
          </View>
          <View style={styles.tile9}>
            <Text style={styles.welcome}>
              Slide 9
            </Text>
          </View>
          <View style={styles.tile10}>
            <Text style={styles.welcome}>
              Slide 10
            </Text>
          </View>
          <View style={styles.tile11}>
            <Text style={styles.welcome}>
              Slide 10
            </Text>
          </View>
          <View style={styles.tile12}>
            <Text style={styles.welcome}>
              Slide 10
            </Text>
          </View>
          <View style={styles.tile13}>
            <Text style={styles.welcome}>
              Slide 10
            </Text>
          </View>
          <View style={styles.tile14}>
            <Text style={styles.welcome}>
              Slide 10
            </Text>
          </View>
          <View style={styles.tile15}>
            <Text style={styles.welcome}>
              Slide 10
            </Text>
          </View>
          <Platform style={styles.platform}/>
        </View>
      </ScrollView>
    );
  }
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    flexDirection: "row",
    flexWrap: "wrap",
    backgroundColor: 'white',
  },
  tile: {
    flexGrow: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#3a8cbe',
    width: 150,
    height: 150
  },
  tile2: {
    flexGrow: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#8c8c8c',
    width: 150,
    height: 150
  },
  tile3: {
    flexGrow: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#ffae0d',
    width: 150,
    height: 150
  },
  tile4: {
    flexGrow: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#5896ed',
    width: 150,
    height: 150
  },
  tile5: {
    flexGrow: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#f283e2',
    width: 150,
    height: 150
  },
  tile6: {
    flexGrow: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#f4413d',
    width: 150,
    height: 150
  },
  tile7: {
    flexGrow: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#d1e224',
    width: 150,
    height: 150
  },
  tile8: {
    flexGrow: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#a99b78',
    width: 150,
    height: 150
  },
  tile9: {
    flexGrow: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#e4e4e4',
    width: 150,
    height: 150
  },
  tile10: {
    flexGrow: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#2a9470',
    width: 150,
    height: 150
  },
  tile11: {
    flexGrow: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#ffd835',
    width: 150,
    height: 150
  },
  tile12: {
    flexGrow: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#4cbce2',
    width: 150,
    height: 150
  },
  tile13: {
    flexGrow: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#40ae4d',
    width: 150,
    height: 150
  },
  tile14: {
    flexGrow: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#f84f28',
    width: 150,
    height: 150
  },
  tile15: {
    flexGrow: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#9fc613',
    width: 150,
    height: 150
  },
  platform: {
    flexGrow: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#636363',
    width: 300,
    height: 150
  },
  welcome: {

    fontSize: 20,
    color: "white",
    textAlign: 'center',
    margin: 10,
  }
});

export default connect()(Welcome);
