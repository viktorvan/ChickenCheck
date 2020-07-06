var ManifestPlugin = require('webpack-manifest-plugin');
var CopyWebpackPlugin = require('copy-webpack-plugin');
const WebpackShellPluginNext = require('webpack-shell-plugin-next');
const path = require('path');
var webpack = require('webpack');
const CONFIG = require('./webpack.CONFIG.js');

module.exports = {
    mode: 'production',
    plugins: [
        new ManifestPlugin(),
        new CopyWebpackPlugin([{ from: resolve(CONFIG.assetsDir) }]),
        new WebpackShellPluginNext({
            onBuildEnd:{
                scripts: ['echo "Generating Bundle.fsx"', 'dotnet fsi ./generateBundle.fsx'],
                blocking: false,
                parallel: true
            }
            })
    ],
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
    resolve: {
        // See https://github.com/fable-compiler/Fable/issues/1490
        symlinks: false
    },

    devtool: 'source-map', // remove eval
};

function resolve(filePath) {
    return path.isAbsolute(filePath) ? filePath : path.join(__dirname, filePath);
}
