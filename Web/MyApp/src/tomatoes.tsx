import { hot } from 'react-hot-loader/root';
import * as React from 'react';

// Ignore if there is a red squiggly line under parts for the next line:
import someImg from './images/amirali-mirhashemian-cRNUvWM9l_I-unsplash.jpg';

function Tomatoes() {
    return <img src={someImg} alt="An image of some tomatoes should show here instead of this text 1303" width='100%' height='100%' />;
}

export default hot(Tomatoes);
