import React from "react";

import { StyleSheet } from "react-native";

export default class Button extends React.Component {

	render () {
		const style = StyleSheet.flatten([this.props.style]);
		return (
			<button style={ style } onClick={this.props.onPress}>{ this.props.title }</button>
		)
	}
}
