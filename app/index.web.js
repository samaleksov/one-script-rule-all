import {
  AppRegistry,
} from 'react-native';

import App from "./containers/Root";

AppRegistry.registerComponent('App', () => App);
AppRegistry.runApplication('App', { rootTag: document.getElementById('react-root') });

if (module.hot) {
  module.hot.accept('./containers/Root', () => {
    const App = require('./containers/Root').default;

    AppRegistry.registerComponent('App', () => App);
    AppRegistry.runApplication('App', { rootTag: document.getElementById('react-root') });

  });
}
