import React from "react";

import { StyleSheet } from "react-native";

export default class Image extends React.Component {

	render () {
		const style = StyleSheet.flatten([this.props.style]);
		return (
			<img style={style} src={this.props.source} />
		)
	}
}
