var path = require('path');
const merge = require('webpack-merge');
const common = require('./webpack.common.js');
const CONFIG = require('./webpack.CONFIG.js');

console.log('Bundling for development');

module.exports = merge(common, {
    mode: 'development',
    watch: true,

    // In development, split the JavaScript and CSS files in order to
    // have a faster HMR support. 
    entry: {
            app: [resolve(CONFIG.jsEntry)],
            style: [resolve(CONFIG.cssEntry)]
        },

    output: {
        path: resolve(CONFIG.outputDir),
        filename: '[name].js'
    },

    devtool: 'eval-source-map',

    // - babel-loader: transforms JS to old syntax (compatible with old browsers)
    // - sass-loaders: transforms SASS/SCSS into JS
    // - file-loader: Moves files referenced in the code (fonts, images) into output folder
    module: {
        rules: [
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
                    'style-loader',
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
