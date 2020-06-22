const WebpackShellPlugin = require('webpack-shell-plugin');
const path = require('path');
var webpack = require('webpack');

module.exports = {
    plugins: [
        new WebpackShellPlugin({
            onBuildEnd: ['dotnet fsi ./generateBundle.fsx'],
            dev: false
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
                        // or node_modules/packangeName
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
    }
};
