import React from "react";

import { TouchableHighlight, View, Text } from "react-native";

export default class Button extends React.Component {

	render () {
		return (
			<button style={this.props.style} onClick={this.props.onPress}>{ this.props.title }</button>
		)
	}
}
