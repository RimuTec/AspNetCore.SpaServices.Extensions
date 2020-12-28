import { hot } from 'react-hot-loader/root';
import * as React from 'react';
import Tomatoes from './tomatoes';

function App() {
    return (
        <div>
            <h2>This is our React application. Last edit was at 1641</h2>
            <p><Tomatoes /></p>
        </div>
    )
}

export default hot(App);
