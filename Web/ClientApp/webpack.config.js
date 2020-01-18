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
        contentBase: './dist',
        headers: {
            // TODO: we may not need these after all:
            "Access-Control-Allow-Origin": "*",
            "Access-Control-Allow-Methods": "GET, POST, PUT, DELETE, PATCH, OPTIONS",
            "Access-Control-Allow-Headers": "X-Requested-With, content-type, Authorization"
        },
        //host: '0.0.0.0',
        //port: 55555,
        //sockPort: 44341,
        //publicPath: '/'
        //contentBase: './dist',
        //hot: true // Only in development so perhaps use cli option?
        //public: 'localhost:'
    },
    resolve: {
        extensions: ['.js', '.jsx', '.ts', '.tsx']
    }
    ,mode: isDevelopment ? 'development' : 'production'
    ,output: {
        filename: isDevelopment ? '[name].js' : '[name].[hash].js',
        //publicPath: '/'
        crossOriginLoading: "anonymous",
    },
    plugins: [
        new CleanWebpackPlugin(),
        new HtmlWebPackPlugin({
            template: './src/index.html',
            filename: './index.html'
        })
    ]
};
