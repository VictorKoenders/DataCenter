function init(){
	connect();
}

function update(){
	connect();
}

function connect(){
	if(!state.connections){
		state.connections = {};
	}
	config.servers.forEach(function(server){
		var connection = state.connections[server.host];
		if(!connection){
			connection = TcpConnection(server.host, server.port);
			connection.seperator = '\r\n';
			connection.context = server;
			
			state.connections[server.host] = connection;
			
			connection.connect();
		}
	});
}

on('tcp_connect', function(){
	console.log('Connection get!');
	
	this.write('NICK ' + this.context.name);
	this.write('USER ' + this.context.name + ' ' + this.context.name + ' ' + this.context.host + ' :' + this.context.name);
});

on('tcp_message', function(message){
	if(message.substring(0, 6) == 'PING :'){
		this.write('PONG :' + message.substring(6));
		return;
	}
	
	var split = message.split(' ');
	if(split[3]){
		for(var i = 4; i < split.length; i++){
			split[3] = split[3] + ' ' + split[i];
		}
		split.splice(4, split.length - 4);
	}
	
	if(split[1] == '376'){
		this.context.channels.forEach(function(channel){
			this.write('JOIN ' + channel);
		}.bind(this));
	}
	
	if(split[1] == 'PRIVMSG'){
		database.Save('irc_privmsg', {
			user: split[0],
			time: new Date(),
			channel: split[2],
			message: split[3].substring(1)
		}, function(){});
		
		emit('irc_privmsg', this, {
			user: split[0],
			time: new Date(),
			channel: split[2],
			message: split[3].substring(1)
		});
		return;
	}
	
	console.log(JSON.stringify(split));
});


