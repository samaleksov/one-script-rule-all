import React, { Component } from 'react';

import { connect } from 'react-redux';
import { push } from 'react-router-redux';
import {
  AppRegistry,
  StyleSheet,
  TouchableHighlight,
  Text,
  View,
  ScrollView
} from 'react-native';

import Platform from './Platform';

import { slides, colors, titles } from "../constants";

class Welcome extends Component {
  goToPage = (page) => {
    return () => {
      this.props.dispatch(push(page));
    }
  }
  render() {
    return (
      <ScrollView style={styles.scroll}>
        <View style={styles.container}>
          {
            slides.map((slide, index) => {
              return (
                <View key={slide}  style={StyleSheet.flatten([styles.tile, {backgroundColor: colors[index]}])}>
                  <TouchableHighlight  onPress={ this.goToPage(slide) }>
                    <View style={styles.textContainer}>
                      <Text style={styles.tileTitle}>{titles[index]}</Text>
                    </View>
                  </TouchableHighlight>
                </View>
              );
            })
          }
        </View>

        <Platform style={styles.platform}/>
      </ScrollView>
    );
  }
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    flexDirection: "row",
    flexWrap: "wrap",
    backgroundColor: 'white'
  },
  scroll: {
    flex: 1,
    backgroundColor: "brown"
  },
  textContainer: {
    flexGrow:1,
    flexDirection: "row",
    flexWrap: "wrap",
    backgroundColor: 'transparent',
    minHeight: 128,
    minWidth: 128
  },
  tile: {
    flex: 1,
    flexGrow: 1,
    minHeight: 128,
    minWidth: 128,
    justifyContent: 'center',
    alignItems: 'center',
  },
  platform: {
    flexGrow: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: colors[0],
    width: 128,
    height: 128
  },
  tileTitle: {
    fontSize: 20,
    color: "white",
    textAlign: 'center',
    margin: 10,
  }
});
styles.touchableBanner = Platform.OS === "web" ? { flexGrow:1, outline: "none" } : {flexGrow:1};
export default connect()(Welcome);
