const { CleanWebpackPlugin } = require('clean-webpack-plugin');
const HtmlWebPackPlugin = require('html-webpack-plugin');
const isDevelopment = process.env.NODE_ENV !== 'production';

console.log("Using NODE_ENV = " + process.env.NODE_ENV);

module.exports = {
    mode: isDevelopment ? 'development' : 'production',
    module: {
        rules: [
            {
                // Documentation for babel-loader at https://webpack.js.org/loaders/babel-loader/
                test: /\.(t|j)sx?$/,
                loader: 'babel-loader',
                exclude: /node_modules/
            },
            {
                // Documentation for html-loader at https://webpack.js.org/loaders/html-loader/
                test: /\.html$/,
                use: [
                    {
                        loader: 'html-loader',
                        options: {
                            minimize: !isDevelopment
                        }
                    }
                ]
            },
            // Use url-loader for images as we're on webpack 4. See comment by Carmine Tambascia 
            // on the following answer on StackOverflow:
            // https://stackoverflow.com/a/39999421/411428
            {
                // Documentation for url-loader at https://webpack.js.org/loaders/url-loader/
                test: /\.(jpe?g|png|gif|svg)$/i,
                use: [
                    {
                        loader: 'url-loader',
                        options: {
                            limit: false
                        }
                    }
                ]
            }
        ]
    },
    resolve: {
        extensions: ['.js', '.jsx', '.ts', '.tsx']
    },
    output: {
        filename: isDevelopment ? '[name].js' : '[name].[hash].js'
    },
    plugins: [
        new CleanWebpackPlugin(),
        new HtmlWebPackPlugin({
            // HtmlWebPackPlugin configuration see https://github.com/jantimon/html-webpack-plugin#usage
            template: './src/index.html', // template to use
            filename: 'index.html' // name to use for created file
        })
    ]
};
