on('gui', function(){
	return [{
		type: 'list',
		data: 'scrollback',
		anchors: 'fill',
		size: [-100, -20],
		display: [{
			type: 'row',
			children: [{
				type: 'text',
				data: '*'
			}]
		}]
	}, {
		type: 'input',
		boundField: 'message',
		anchors: 'bottom',
		size: [-100, 20]
	}, {
		type: 'list',
		data: 'users',
		anchors: 'right',
		size: [100, -20],
		display: [{
			type: 'row',
			children: [{
				type: 'text',
				data: '*'
			}]
		}]
	}, {
		type: 'button',
		text: 'send',
		anchors: ['bottom', 'right'],
		size: [ 100, 20 ],
		clickEvent: 'send_text'
	}]
});

function init(){
	setState();
}

function update(){
	setState();
}

function setState(){
	state.scrollback = config.scrollback || [];
	state.message = '';
	state.users = [ 'Person 1', 'Person 2' ];
}

on('send_text', function(newState){
	state.scrollback.Add(newState.message);
	config.scrollback = state.scrollback;
	console.log(state, config);
});