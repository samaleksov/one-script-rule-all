import App from '../containers/App';
import Main from '../components/Main';
import Welcome from '../components/Welcome';
import Slides from '../components/Slides';
import SlidesMain from '../components/SlidesMain';
import SlidesSecondary from '../components/SlidesSecondary';

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
        }
      ]
    }
  ]
};
