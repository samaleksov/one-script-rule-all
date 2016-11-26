import App from '../containers/App';
import Main from '../components/Main';
import Welcome from '../components/Welcome';
import Slides from '../components/Slides';
import SlidesMain from '../components/SlidesMain';
import SlidesSecondary from '../components/SlidesSecondary';
import SlidesSam from '../components/SlidesSam';
import SlidesFinal from '../components/SlidesFinal';
import SlidesUnicorns from '../components/SlidesUnicorns';
import SlidesTweet from '../components/SlidesTweet';
import SlidesAndroid from '../components/SlidesAndroid';
import SlidesIOS from '../components/SlidesIOS';
import SlidesWindows from '../components/SlidesWindows';
import SlidesCordova from '../components/SlidesCordova';
import SlidesXamarin from '../components/SlidesXamarin';

export default {
  component: App,
  childRoutes: [
    {
      path: '/',
      component: Welcome,
    },
    {
      path: '/main',
      component: Main,
    },
    {
      path: '/slides',
      component: Slides,
      childRoutes: [
        {
          path: '/slides/main',
          component: SlidesMain,
        },
        {
          path: '/slides/secondary',
          component: SlidesSecondary,
        },
        {
          path: '/slides/sam',
          component: SlidesSam,
        },
        {
          path: '/slides/tweet',
          component: SlidesTweet,
        },
        {
          path: '/slides/thankyou',
          component: SlidesFinal,
        },
        {
          path: '/slides/unicorns',
          component: SlidesUnicorns,
        },
        {
          path: '/slides/android',
          component: SlidesAndroid,
        },
        {
          path: '/slides/ios',
          component: SlidesIOS,
        },
        {
          path: '/slides/windows',
          component: SlidesWindows,
        },
        {
          path: '/slides/cordova',
          component: SlidesCordova,
        },
        {
          path: '/slides/xamarin',
          component: SlidesXamarin,
        }
      ]
    }
  ]
};
