"use strict";

var _extends = Object.assign || function (target) { for (var i = 1; i < arguments.length; i++) { var source = arguments[i]; for (var key in source) { if (Object.prototype.hasOwnProperty.call(source, key)) { target[key] = source[key]; } } } return target; };

var _createClass = (function () { function defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } } return function (Constructor, protoProps, staticProps) { if (protoProps) defineProperties(Constructor.prototype, protoProps); if (staticProps) defineProperties(Constructor, staticProps); return Constructor; }; })();

var _get = function get(_x, _x2, _x3) { var _again = true; _function: while (_again) { var object = _x, property = _x2, receiver = _x3; _again = false; if (object === null) object = Function.prototype; var desc = Object.getOwnPropertyDescriptor(object, property); if (desc === undefined) { var parent = Object.getPrototypeOf(object); if (parent === null) { return undefined; } else { _x = parent; _x2 = property; _x3 = receiver; _again = true; desc = parent = undefined; continue _function; } } else if ("value" in desc) { return desc.value; } else { var getter = desc.get; if (getter === undefined) { return undefined; } return getter.call(receiver); } } };

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

function _inherits(subClass, superClass) { if (typeof superClass !== "function" && superClass !== null) { throw new TypeError("Super expression must either be null or a function, not " + typeof superClass); } subClass.prototype = Object.create(superClass && superClass.prototype, { constructor: { value: subClass, enumerable: false, writable: true, configurable: true } }); if (superClass) Object.setPrototypeOf ? Object.setPrototypeOf(subClass, superClass) : subClass.__proto__ = superClass; }

var Main = (function (_React$Component) {
	_inherits(Main, _React$Component);

	function Main() {
		_classCallCheck(this, Main);

		_get(Object.getPrototypeOf(Main.prototype), "constructor", this).call(this);
		this.state = {
			guis: {
				error: null
			},
			activeTab: 0
		};
	}

	_createClass(Main, [{
		key: "componentWillMount",
		value: function componentWillMount() {
			var _this = this;

			socket.emit("load_modules");
			socket.once("load_modules_response", function (modules) {
				modules.modules.forEach(function (m) {
					var _iteratorNormalCompletion = true;
					var _didIteratorError = false;
					var _iteratorError = undefined;

					try {
						for (var _iterator = m.registeredEvents[Symbol.iterator](), _step; !(_iteratorNormalCompletion = (_step = _iterator.next()).done); _iteratorNormalCompletion = true) {
							var x = _step.value;

							if (x === "gui") {
								_this.loadGui(m);
								break;
							}
						}
					} catch (err) {
						_didIteratorError = true;
						_iteratorError = err;
					} finally {
						try {
							if (!_iteratorNormalCompletion && _iterator["return"]) {
								_iterator["return"]();
							}
						} finally {
							if (_didIteratorError) {
								throw _iteratorError;
							}
						}
					}
				});
			});

			socket.on('state_changed', this.stateChanged.bind(this));
		}
	}, {
		key: "stateChanged",
		value: function stateChanged(args) {
			var hasChanges = false;
			var guis = JSON.parse(JSON.stringify(this.state.guis));
			for (var x in guis) {
				if (x != args.module.name) continue;
				hasChanges = true;
				guis[x].module = args.module;
			}

			if (hasChanges) {
				this.setState({
					guis: guis
				});
			}
		}
	}, {
		key: "loadGui",
		value: function loadGui(module) {
			var _this2 = this;

			socket.emit('emit', { module: module.name, event: 'gui' });
			socket.once("emit_response", function (r) {

				var guis = JSON.parse(JSON.stringify(_this2.state.guis));
				guis[r.module.name] = {
					module: r.module,
					view: r.result
				};
				_this2.setState({
					guis: guis
				});
			});
		}
	}, {
		key: "renderTab",
		value: function renderTab(key, index) {
			return React.createElement(
				"li",
				{ key: index, className: this.state.activeTab === index ? "active" : "" },
				React.createElement(
					"a",
					{ href: "#", onClick: this.tabClicked.bind(this, key, index) },
					key
				)
			);
		}
	}, {
		key: "tabClicked",
		value: function tabClicked(key, index, e) {
			if (e && e.preventDefault) e.preventDefault();
			this.setState({ activeTab: index });
		}
	}, {
		key: "doSetState",
		value: function doSetState(newObj) {
			var moduleName = Object.keys(this.state.guis)[this.state.activeTab];
			var gui = this.state.guis[moduleName];
			for (var x in newObj) {
				gui.module.state[x] = newObj[x];
			}
			this.setState({
				guis: this.state.guis
			});
		}
	}, {
		key: "render",
		value: function render() {
			var Page = null;
			if (this.state.guis && Object.keys(this.state.guis)[this.state.activeTab]) {
				var guiName = Object.keys(this.state.guis)[this.state.activeTab];
				var gui = this.state.guis[guiName];
				if (gui == null) {
					if (guiName == "error") Page = React.createElement(ErrorView, null);
				} else Page = React.createElement(Detail, _extends({}, gui, { setState: this.doSetState.bind(this), state: gui.module.state }));
			}
			return React.createElement(
				"div",
				null,
				React.createElement(
					"ul",
					{ className: "nav nav-tabs" },
					Object.keys(this.state.guis).map(this.renderTab.bind(this))
				),
				Page
			);
		}
	}]);

	return Main;
})(React.Component);

var ErrorView = (function (_React$Component2) {
	_inherits(ErrorView, _React$Component2);

	function ErrorView() {
		_classCallCheck(this, ErrorView);

		_get(Object.getPrototypeOf(ErrorView.prototype), "constructor", this).call(this);
		this.state = { errors: [] };
	}

	_createClass(ErrorView, [{
		key: "componentWillMount",
		value: function componentWillMount() {
			var _this3 = this;

			console.log("Component will mount");
			var request = new XMLHttpRequest();
			request.onload = function (r) {
				_this3.setState({
					errors: JSON.parse(r.target.responseText).rows
				});
			};
			request.open('GET', '/api/error', true);
			request.send(null);
		}
	}, {
		key: "renderErrorDefinition",
		value: function renderErrorDefinition(data, key, index) {
			var value = data[key];
			if (typeof value === "object") {
				value = React.createElement(
					"dl",
					{ className: "dl-horizontal" },
					Object.keys(value).map(this.renderErrorDefinition.bind(this, value))
				);
			}
			return [React.createElement(
				"dt",
				{ key: index + '_1' },
				key
			), React.createElement(
				"dd",
				{ key: index + '_2' },
				value
			)];
		}
	}, {
		key: "renderErrorRow",
		value: function renderErrorRow(error) {
			return React.createElement(
				"tr",
				{ key: error.id },
				React.createElement(
					"td",
					null,
					new Date(error.value.time).toGMTString()
				),
				React.createElement(
					"td",
					null,
					error.value.error
				),
				React.createElement(
					"td",
					null,
					React.createElement(
						"dl",
						{ className: "dl-horizontal" },
						Object.keys(error.value).filter(function (v) {
							return v != '_id' && v != 'error' && v != 'time' && v != '_rev';
						}).map(this.renderErrorDefinition.bind(this, error.value))
					)
				)
			);
		}
	}, {
		key: "render",
		value: function render() {
			return React.createElement(
				"div",
				null,
				React.createElement(
					"h2",
					null,
					"Error overview"
				),
				React.createElement(
					"table",
					{ className: "table table-condensed table-striped table-hover" },
					React.createElement(
						"thead",
						null,
						React.createElement(
							"tr",
							null,
							React.createElement(
								"th",
								null,
								"Time"
							),
							React.createElement(
								"th",
								null,
								"Message"
							),
							React.createElement(
								"th",
								null,
								"Additional data"
							)
						)
					),
					React.createElement(
						"tbody",
						null,
						this.state.errors.map(this.renderErrorRow.bind(this))
					)
				)
			);
		}
	}]);

	return ErrorView;
})(React.Component);

var Detail = (function (_React$Component3) {
	_inherits(Detail, _React$Component3);

	function Detail() {
		_classCallCheck(this, Detail);

		_get(Object.getPrototypeOf(Detail.prototype), "constructor", this).apply(this, arguments);
	}

	_createClass(Detail, [{
		key: "renderViewItem",
		value: function renderViewItem(item, index) {
			if (Array.isArray(item)) {
				return React.createElement(
					"div",
					{ key: index },
					item.map(this.renderViewItem.bind(this))
				);
			}
			switch (item.type) {
				case 'text':
					if (item.data) return React.createElement(
						"span",
						{ key: index },
						this.getStateValue(item.data)
					);
					return React.createElement(
						"span",
						{ key: index },
						item.text
					);
				case 'row':
					return React.createElement(
						"div",
						null,
						this.renderViewItem(item.children, 0)
					);
				case 'input':
					return React.createElement("input", { type: "text", value: this.getStateValue(item.boundField), onChange: this.inputChanged.bind(this, item) });
				case 'list':
					return React.createElement(
						"ul",
						null,
						(this.getStateValue(item.data) || []).map(this.renderListItem.bind(this, item))
					);
				case 'button':
					return React.createElement("input", { type: "button", className: "btn btn-default", value: item.text, onClick: this.buttonClicked.bind(this, item) });
			}
			return React.createElement(
				"div",
				null,
				React.createElement(
					"i",
					{ key: index },
					"Unknown type: ",
					item.type
				)
			);
		}
	}, {
		key: "getStateValue",
		value: function getStateValue(field) {
			if (!this.props.state) return null;
			if (field == '*') return this.props.state;
			return this.props.state[field];
		}
	}, {
		key: "renderListItem",
		value: function renderListItem(item, row, index) {
			return React.createElement(
				"li",
				{ key: index },
				React.createElement(Detail, { module: this.props.module, state: row, view: item.display, setState: this.stateChanged.bind(this, item, index) })
			);
		}
	}, {
		key: "stateChanged",
		value: function stateChanged(item, index, newValue) {
			console.log('stateChanged', item, index);
		}
	}, {
		key: "inputChanged",
		value: function inputChanged(item, e) {
			var obj = {};
			obj[item.boundField] = e.target.value;
			this.props.setState(obj);
		}
	}, {
		key: "buttonClicked",
		value: function buttonClicked(item, e) {
			if (e && e.preventDefault) e.preventDefault();
			console.log(item, 'clicked');
			socket.emit('emit', {
				module: this.props.module.name,
				event: item.clickEvent,
				arguments: [this.props.module.state]
			});
		}
	}, {
		key: "render",
		value: function render() {
			return React.createElement(
				"div",
				null,
				this.renderViewItem(this.props.view, 0)
			);
		}
	}]);

	return Detail;
})(React.Component);

var element = document.createElement("div");
ReactDOM.render(React.createElement(Main, null), element);
document.body.appendChild(element);

