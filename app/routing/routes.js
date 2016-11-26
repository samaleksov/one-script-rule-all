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
import SlidesSam from '../components/SlidesSam';
import SlidesFinal from '../components/SlidesFinal';
import SlidesTweet from '../components/SlidesTweet';
import SlidesUnicorns from '../components/SlidesUnicorns';
import SlidesAndroid from '../components/SlidesAndroid';
import SlidesIOS from '../components/SlidesIOS';
import SlidesWindows from '../components/SlidesWindows';
import SlidesCordova from '../components/SlidesCordova';
import SlidesXamarin from '../components/SlidesXamarin';

export default function () {
    return (
        <StackRoute path="app" component={App}>
          <Route path="/" component={Welcome} />
          <Route path="/main" component={Main} />
          <Route path="/slides" component={Slides}>
            <Route path="/slides/main" component={SlidesMain} />
            <Route path="/slides/secondary" component={SlidesSecondary} />
            <Route path="/slides/sam" component={SlidesSam} />
            <Route path="/slides/tweet" component={SlidesTweet} />
            <Route path="/slides/android" component={SlidesAndroid} />
            <Route path="/slides/ios" component={SlidesIOS} />
            <Route path="/slides/windows" component={SlidesWindows} />
            <Route path="/slides/xamarin" component={SlidesXamarin} />
            <Route path="/slides/cordova" component={SlidesCordova} />
            <Route path="/slides/unicorns" component={SlidesUnicorns} />
            <Route path="/slides/thankyou" component={SlidesFinal} />
          </Route>
        </StackRoute>)
}
