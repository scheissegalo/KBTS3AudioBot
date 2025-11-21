const path = require('path');
const HtmlWebpackPlugin = require('html-webpack-plugin')
const CopyPlugin = require('copy-webpack-plugin');
const VueLoaderPlugin = require('vue-loader/lib/plugin')

module.exports = {
	entry: './src/ts/Main.ts',
	module: {
		rules: [
			{
				test: /\.tsx?$/i,
				use: [{
					loader: 'ts-loader',
					options: {
						appendTsSuffixTo: [/\.vue$/]
					}
				}],
				exclude: /node_modules/
			},
			{
				test: /\.less$/i,
				use: [
					'style-loader',
					'css-loader',
					'less-loader',
				],
			},
			{
				test: /\.svg$/i,
				loader: 'svg-inline-loader'
			},
			{
				test: /\.vue$/i,
				loader: 'vue-loader'
			},
			{
				test: /\.css$/i,
				use: [
					'vue-style-loader',
					'css-loader',
				],
			},
			{
				test: /\.(jpe?g|png|gif|eot|woff|ttf|woff2)$/i,
				type: 'asset/resource',
				generator: {
					filename: '[path][name][ext]'
				}
			}
		]
	},
	resolve: {
		extensions: ['.tsx', '.ts', '.js', '.vue'],
		alias: {
			'vue': 'vue/dist/vue.esm.js'
		}
	},
	output: {
		filename: 'bundle.js',
		path: path.resolve(__dirname, 'dist'),
		clean: true
	},
	plugins: [
		new HtmlWebpackPlugin({
			title: 'North Industries - TS3AudioBot',
			template: 'src/html/index.html'
		}),
		new CopyPlugin({
			patterns: [
				{ 
					from: 'src/html', 
					to: '.',
					globOptions: {
						ignore: ['**/index.html']
					}
				},
			]
		}),
		new VueLoaderPlugin()
	],
	devServer: {
		allowedHosts: 'all',
		headers: {
			"Access-Control-Allow-Origin": "*",
			"Access-Control-Allow-Methods": "GET",
			"Access-Control-Allow-Headers": "X-Requested-With, Content-Type, Authorization"
		}
	}
};
