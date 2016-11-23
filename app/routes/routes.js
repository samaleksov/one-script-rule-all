import App from '../containers/App';
import Main from '../components/Main';

export default {
  component: App,
  childRoutes: [
    {
      path: '/',
      getComponent(location, cb) {
				cb(null, Main);
      }
    }
  ]
};
