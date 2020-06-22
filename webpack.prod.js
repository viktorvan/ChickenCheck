var path = require('path');
const merge = require('webpack-merge');
const common = require('./webpack.common.js');
var MiniCssExtractPlugin = require('mini-css-extract-plugin');

const CONFIG = require('./webpack.CONFIG.js');

console.log('Bundling for production');

module.exports = merge(common, {
    mode: 'production',

    // In production bundle styles together
    // with the code because the MiniCssExtractPlugin will extract the
    // CSS in a separate files.
    // It is important to have the app as the last entry in the array, when it is exposed as a library output.
    entry: {
        app: [resolve(CONFIG.cssEntry), resolve(CONFIG.fsharpEntry)]
    },

    // Add a hash to the output file name in production
    // to prevent browser caching if code changes
    output: {
        path: resolve(CONFIG.outputDir),
        filename: 'ChickenCheck.[name].[contenthash].js',
        libraryTarget: 'umd',
        library: ['ChickenCheck', '[name]']
    },

    plugins: [
        new MiniCssExtractPlugin({ filename: 'style.[contenthash].css' }),
    ],

    // - babel-loader: transforms JS to old syntax (compatible with old browsers)
    // - sass-loaders: transforms SASS/SCSS into JS
    // - file-loader: Moves files referenced in the code (fonts, images) into output folder
    module: {
        rules: [
            {
                test: /\.fs(x|proj)?$/,
                use: {
                    loader: 'fable-loader',
                    options: {
                        babel: CONFIG.babel
                    }
                }
            },
            {
                test: /\.js$/,
                exclude: /node_modules/,
                use: {
                    loader: 'babel-loader',
                    options: CONFIG.babel
                },
            },
            {
                test: /\.(sass|scss|css)$/,
                use: [
                    MiniCssExtractPlugin.loader,
                    'css-loader',
                    {
                        loader: 'resolve-url-loader',
                    },
                    {
                        loader: 'sass-loader',
                        options: { implementation: require('sass') }
                    }
                ],
            },
            {
                test: /\.(png|jpg|jpeg|gif|svg|woff|woff2|ttf|eot)(\?.*)?$/,
                use: ['file-loader']
            }
        ]
    }
});

function resolve(filePath) {
    return path.isAbsolute(filePath) ? filePath : path.join(__dirname, filePath);
}
