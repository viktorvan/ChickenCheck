module.exports = {
    jsEntry: './src/ChickenCheck.Client/build/App.js',
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
};
