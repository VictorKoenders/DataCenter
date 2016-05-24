'use strict';

var socket = (function () {
	var s = null;
	var callbacks = {};
	var emits = [];

	function onopen(event) {
		sendEmits();
	}

	function onmessage(event) {
		var text = event.data;
		try {
			var data = JSON.parse(text);
			if (data.action) {
				for (var x in callbacks[data.action]) {
					var action = callbacks[data.action][x];
					action(data);
					if (action.once) {
						callbacks[data.action].splice(x, 1);
					}
				}
			}
		} catch (ex) {
			console.log(ex.message, ex.stack);
			console.log(text);
		}
	}

	function onerror() {
		console.log('error', arguments);
	}

    var connectingTimeout = null;

	function onclose() {
	    console.log('closing');
	    s = null;

	    if (connectingTimeout)
	        clearTimeout(connectingTimeout);

	    connectingTimeout = setTimeout(function () {
	        connect();
	    }, 5000);
    }

	function connect() {
		s = new WebSocket('ws://localhost');
		s.onopen = onopen;
	    s.onclose = onclose;
		s.onmessage = onmessage;
		s.onerror = onerror;
	}

	connect();

	function sendEmits() {
		if (s == null || s.readyState === 0) return;
		var _iteratorNormalCompletion = true;
		var _didIteratorError = false;
		var _iteratorError = undefined;

		try {
			for (var _iterator = emits[Symbol.iterator](), _step; !(_iteratorNormalCompletion = (_step = _iterator.next()).done); _iteratorNormalCompletion = true) {
				var _emit = _step.value;

				s.send(JSON.stringify(_emit));
			}
		} catch (err) {
			_didIteratorError = true;
			_iteratorError = err;
		} finally {
			try {
				if (!_iteratorNormalCompletion && _iterator['return']) {
					_iterator['return']();
				}
			} finally {
				if (_didIteratorError) {
					throw _iteratorError;
				}
			}
		}

		emits = [];
	}

	function emit(action, contents) {
		emits.push({ action: action, contents: contents });
		sendEmits();
	}

	return {
		on: function on(name, cb) {
			if (!callbacks[name]) callbacks[name] = [];
			callbacks[name].push(cb);
		},
		once: function once(name, cb) {
			if (!callbacks[name]) callbacks[name] = [];
			cb.once = true;
			callbacks[name].push(cb);
		},
		off: function off(name, cb) {
			var index = callbacks[name].indexOf(cb);
			if (index > -1) {
				callbacks[name].splice(index, 1);
			}
		},
		emit: emit
	};
})();

