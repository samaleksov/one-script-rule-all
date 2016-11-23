import App from '../containers/App';
import Main from '../components/Main';
import Welcome from '../components/Welcome';

console.log(App);
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
