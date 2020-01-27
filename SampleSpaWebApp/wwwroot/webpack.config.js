const isDevelopment = process.env.NODE_ENV !== 'production';
const HtmlWebPackPlugin = require('html-webpack-plugin');
const { CleanWebpackPlugin } = require("clean-webpack-plugin");

module.exports = {
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
                        options: { minimize: !isDevelopment }
                    }
                ]
            }
        ]
    },
    devServer: {
        contentBase: './wwwroot/dist'
    },
    resolve: {
        extensions: ['.js', '.jsx', '.ts', '.tsx']
    }
    ,mode: isDevelopment ? 'development' : 'production'
    , output: {
        filename: isDevelopment ? '[name].js' : '[name].[hash].js',
        crossOriginLoading: "anonymous"
    },
    plugins: [
        new CleanWebpackPlugin(),
        new HtmlWebPackPlugin({
            template: './src/index.html',
            filename: './index.html'
        })
    ]
};
