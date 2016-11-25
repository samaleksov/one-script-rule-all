import React from 'react';

import {
  Route,
  StackRoute
} from 'react-router-native';

import Welcome from '../components/Welcome';
import Main from '../components/Main';
import App from '../containers/App';
import Slides from '../components/Slides';
import SlidesMain from '../components/SlidesMain';
import SlidesSecondary from '../components/SlidesSecondary';

export default function () {
    return (
        <StackRoute path="app" component={App}>
          <Route path="/" component={Welcome} />
          <Route path="/main" component={Main} />
          <Route path="/slides" component={Slides}>
            <Route path="/slides/main" component={SlidesMain} />
            <Route path="/slides/secondary" component={SlidesSecondary} />
          </Route>
        </StackRoute>)
}
