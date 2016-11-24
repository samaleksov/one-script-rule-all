import React, { Component } from 'react';
import ReactDOM from 'react-dom';
import { Router } from 'react-router';
import { AppContainer } from 'react-hot-loader';
import { Provider } from 'react-redux';


import { configureStoreAndHistory } from '../lib/configureStore';

import routes from "../routing/routes";

const { store, history } = configureStoreAndHistory();


export default class App extends Component {
  render() {
    return (
      <AppContainer>
        <Provider store={store}>
          <Router history={history} routes={routes} />
        </Provider>
      </AppContainer>
    );
  }
}
