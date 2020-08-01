// Template for webpack.config.js in Fable projects
// Find latest version in https://github.com/fable-compiler/webpack-config-template

// In most cases, you'll only need to edit the CONFIG object (after dependencies)
// See below if you need better fine-tuning of Webpack options

// Dependencies. Also required: core-js, fable-loader, fable-compiler, @babel/core,
// @babel/preset-env, babel-loader, sass, sass-loader, css-loader, style-loader, file-loader, resolve-url-loader
var path = require('path');
var webpack = require('webpack');
var CopyWebpackPlugin = require('copy-webpack-plugin');
var MiniCssExtractPlugin = require('mini-css-extract-plugin');
var ManifestPlugin = require('webpack-manifest-plugin');
var WebpackShellPluginNext = require('webpack-shell-plugin-next')

var CONFIG = {
    // The tags to include the generated JS and CSS will be automatically injected in the HTML template
    // See https://github.com/jantimon/html-webpack-plugin
    indexHtmlTemplate: './src/ChickenCheck.Client/index.html',
    fsharpEntry: './src/ChickenCheck.Client/ChickenCheck.Client.fsproj',
    cssEntry: './src/ChickenCheck.Client/scss/main.scss',
    outputDir: 'output/server/public',
    assetsDir: './src/ChickenCheck.Client/public',
    // Use babel-preset-env to generate JS compatible with most-used browsers.
    // More info at https://babeljs.io/docs/en/next/babel-preset-env.html
    babel: {
        presets: [
            ['@babel/preset-env', {
                modules: false,
                // This adds polyfills when needed. Requires core-js dependency.
                // See https://babeljs.io/docs/en/babel-preset-env#usebuiltins
                // Note that you still need to add custom polyfills if necessary (e.g. whatwg-fetch)
                useBuiltIns: 'usage',
                corejs: 3
            }]
        ],
    }
}

var isProduction = !process.argv.find(v => v.indexOf('--mode=development') !== -1);
console.log('Bundling for ' + (isProduction ? 'production' : 'development') + '...');

var commonPlugins = [
    new CopyWebpackPlugin({ 
        patterns: [
            { 
                from: resolve(CONFIG.assetsDir), 
            }
        ]
    }),
    new ManifestPlugin(),
    new WebpackShellPluginNext({
        onBuildEnd:{
            scripts: ['echo "Generating Bundle.fsx"', 'dotnet fsi ./generateBundle.fsx'],
            blocking: false,
            parallel: true
        }
    })
];

module.exports = {
    entry: {
        app: [resolve(CONFIG.fsharpEntry), resolve(CONFIG.cssEntry)]
    }, 
    // Add a hash to the output file name in production
    // to prevent browser caching if code changes
    output: isProduction ? {
        path: resolve(CONFIG.outputDir),
        filename: 'ChickenCheck.[name].[contenthash].js',
        libraryTarget: 'umd',
        library: ['ChickenCheck', '[name]']
    } : {
        path: resolve(CONFIG.outputDir),
        filename: 'ChickenCheck.[name].js',
        libraryTarget: 'umd',
        library: ['ChickenCheck', '[name]'],
        devtoolNamespace: "wp"
    },
    devtool: isProduction ? 'source-map' : 'eval-source-map',
    optimization: {
        moduleIds: 'hashed',
        runtimeChunk: "single",
        splitChunks: {
            chunks: 'all',
            maxInitialRequests: Infinity,
            minSize: 0,
            cacheGroups: {
                vendor: {
                    test: /[\\/]node_modules[\\/]/,
                    name(module) {
                        // get the name. E.g. node_modules/packageName/not/this/part.js
                        // or node_modules/packageName
                        const packageName = module.context.match(/[\\/]node_modules[\\/](.*?)([\\/]|$)/)[1];
                        // npm package names are URL-safe, but some servers don't like @ symbols
                        return `vendor.${packageName.replace('@', '')}`;
                    }
                }
            }
        },
    },
    // Besides the HtmlPlugin, we use the following plugins:
    // PRODUCTION
    //      - MiniCssExtractPlugin: Extracts CSS from bundle to a different file
    //          To minify CSS, see https://github.com/webpack-contrib/mini-css-extract-plugin#minimizing-for-production    
    //      - CopyWebpackPlugin: Copies static assets to output directory
    // DEVELOPMENT
    //      - HotModuleReplacementPlugin: Enables hot reloading when code changes without refreshing
    plugins: isProduction ?
        commonPlugins.concat([
            new MiniCssExtractPlugin({ filename: 'style.[contenthash].css' }),
        ])
        : commonPlugins,
    resolve: {
        // See https://github.com/fable-compiler/Fable/issues/1490
        symlinks: false
    },
    // - fable-loader: transforms F# into JS
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
                    isProduction
                        ? MiniCssExtractPlugin.loader
                        : 'style-loader',
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
};

function resolve(filePath) {
    return path.isAbsolute(filePath) ? filePath : path.join(__dirname, filePath);
}
