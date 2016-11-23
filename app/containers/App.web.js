import React, { PropTypes } from 'react';
import { withRouter } from 'react-router'

class App extends React.Component {

  componentDidMount = () => {
  }
  componentWillUnmount = () => {

  }
  render () {

    return (
			<div>
				<h1>Main app container</h1>
        {this.props.children}
			</div>		
    )
  }
}
App.propTypes = {
  children: PropTypes.node
};

export default withRouter(App);
