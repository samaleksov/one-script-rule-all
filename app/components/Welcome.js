import React, { Component } from 'react';

import { connect } from 'react-redux';
import { push } from 'react-router-redux';
import {
  AppRegistry,
  StyleSheet,
  TouchableHighlight,
  Text,
  View,
  ListView,
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
    const ds = new ListView.DataSource({rowHasChanged: (r1, r2) => r1 !== r2});
    const dataSource = ds.cloneWithRows(slides);

    return (
      <ListView contentContainerStyle={styles.list}
         dataSource={dataSource}
          renderRow={(slide, s, index) => {
              return (<View key={slide}  style={StyleSheet.flatten([{}, {backgroundColor: colors[index]}])}>
                <TouchableHighlight  onPress={ this.goToPage(slide) }>
                  <View>
                    <Text style={styles.tileTitle}>{titles[index]}</Text>
                  </View>
                </TouchableHighlight>
              </View>);
            }
          }
        >
      </ListView>
    );
  }
}

const styles = StyleSheet.create({
  scroll: {
    flexDirection: 'row',
    flexWrap: 'wrap'
  },
  tile: {
    width: 100
  },
  platform: {
    backgroundColor: colors[0],
    width: 100
  },
  tileTitle: {
    fontSize: 20,
    color: "white",
    textAlign: 'center',
    margin: 10,
  }
});
styles.touchableBanner = Platform.OS === "web" ? { outline: "none" } : { };
export default connect()(Welcome);
