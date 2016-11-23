import { createStore, combineReducers, applyMiddleware } from 'redux';
import { syncHistoryWithStore, routerReducer, routerMiddleware } from 'react-router-redux';

import reducers from '../reducers';
import history from './history';

export function configureStoreAndHistory (otherHistory) {
	const finalHistory = typeof otherHistory === "undefined" ? history : otherHistory;

	const middleware = routerMiddleware(finalHistory);
	const store = createStore(
		  combineReducers({
			    ...reducers,
			    routing: routerReducer
	  }),
	  applyMiddleware(middleware)
	);

	const enhancedHistory = syncHistoryWithStore(finalHistory, store);

	return { store, history: enhancedHistory };
}