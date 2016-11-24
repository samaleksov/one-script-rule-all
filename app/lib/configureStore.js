import { createStore, combineReducers, applyMiddleware } from 'redux';
import { syncHistoryWithStore, routerReducer, routerMiddleware } from 'react-router-redux';
// import { composeWithDevTools } from 'remote-redux-devtools';

import reducers from '../reducers';
import history from './history';

export function configureStoreAndHistory (otherHistory) {
	const finalHistory = typeof otherHistory === "undefined" ? history : otherHistory;

	const middleware = routerMiddleware(finalHistory);
	// const composeEnhancers = composeWithDevTools({ realtime: true, port: 8000 });
	const store = createStore(
		  combineReducers({
			    ...reducers,
			    routing: routerReducer
	  }),
	  applyMiddleware(middleware)
	);

	if (module.hot) {
		  module.hot.accept('../reducers', () => {
		    const nextRootReducer = require('../reducers');
		    store.replaceReducer(nextRootReducer);
			});
	}

	const enhancedHistory = syncHistoryWithStore(finalHistory, store);

	return { store, history: enhancedHistory };
}
