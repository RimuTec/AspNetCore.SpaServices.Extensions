const path = require('path');
const { CleanWebpackPlugin } = require('clean-webpack-plugin');
const HtmlWebPackPlugin = require('html-webpack-plugin');
const isDevelopment = process.env.NODE_ENV !== 'production';

console.log("Using NODE_ENV = " + process.env.NODE_ENV);

module.exports = {
    mode: isDevelopment ? 'development' : 'production',
    module: {
        rules: [
            {
                test: /\.(t|j)sx?$/,
                loader: 'babel-loader',
                exclude: /node_modules/
            },
            {
                test: /\.html$/,
                use: [
                    {
                        loader: 'html-loader',
                        options: {
                            minimize: !isDevelopment
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
