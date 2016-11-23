import App from '../containers/App';
import Main from '../components/Main';
import Welcome from '../components/Welcome';

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
    }
  ]
};
