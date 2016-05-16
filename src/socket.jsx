var socket = (function() {
	var s = null;
	var callbacks = {};
	var emits = [];

	function onopen(event) {
		sendEmits();
	}

	function onmessage(event) {
		const text = event.data;
		try {
			const data = JSON.parse(text);
			if (data.action) {
				for (let x in callbacks[data.action]) {
					const action = callbacks[data.action][x];
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
		s = null;
		setTimeout(function () {
			connect();
		}, 5000);
	}

	function connect() {
		s = new WebSocket('ws://localhost');
		s.onopen = onopen;
		s.onmessage = onmessage;
		s.onerror = onerror;
	}

	connect();

	function sendEmits() {
		if (s == null || s.readyState === 0) return;
		for (let emit of emits) {
			s.send(JSON.stringify(emit));
		}
		emits = [];
	}

	function emit(action, contents) {
		emits.push({ action: action, contents: contents });
		sendEmits();
	}

	return {
		on: function(name, cb) {
			if (!callbacks[name]) callbacks[name] = [];
			callbacks[name].push(cb);
		},
		once: function(name, cb) {
			if (!callbacks[name]) callbacks[name] = [];
			cb.once = true;
			callbacks[name].push(cb);
		},
		off: function(name, cb) {
			const index = callbacks[name].indexOf(cb);
			if (index > -1) {
				callbacks[name].splice(index, 1);
			}
		},
		emit: emit
	}
})();