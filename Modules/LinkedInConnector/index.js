
function setup(){
	if((!state.connector || state.connector.OAuth.Failed) && config && config.client_id && config.client_secret){ 
		state.connector = APIConnector({
			oauth: {
				state: {
					client_id: config.client_id,
					client_secret: config.client_secret,
					redirect_uri: 'http://localhost/authorize/linkedin',
					secret_code: randomString(21)
				},
				steps: [
					CreateOAuthRedirect('https://www.linkedin.com/uas/oauth2/authorization?client_id={client_id}&redirect_uri={redirect_uri}&response_type=code&state={secret_code}'),
					GetOAuthResponse('/authorize/linkedin?code={code}&state={response_code}'),
					CreateOAuthCompareState('response_code', 'secret_code'),
					CreateOAuthPost('https://www.linkedin.com/uas/oauth2/accessToken', {
						grant_type: 'authorization_code',
						code: '{code}',
						redirect_uri: '{redirect_uri}',
						client_id: '{client_id}',
						client_secret: '{client_secret}'
					})
				],
				eventNames: {
					success: 'linkedin_auth_success',
					fail: 'linkedin_auth_fail'
				}
			},
			headers: {
				'x-li-format': 'json',
				'Content-Type': 'application/json'
			},
			methods: [{
				oauth2_access_token: 'Bearer {auth.access_token}',
				name: 'load_profile',
				url: 'https://api.linkedin.com/v1/people/~'
			}]
		});
	} else {
		return;
	}

	if(config.auth && new Date(config.auth.expires_at) > new Date()) {
		console.log('reloading linkedin, but we\'re already authed');
		emit('linkedin_auth_success', this);
		return;
	} else {
		state.connector.OAuth.connect();
	}
}

function init(){
	setup();
}

function update(){
	setup();
}

on('linkedin_auth_fail', function(data){
	console.log('LinkedIn Auth failed at step', data.step);
	console.log(data.reason)
});

on('linkedin_auth_success', function(data){
	console.log('LinkedIn Auth succeeded!');
	if(data){
		config.auth = {
			access_token: data.state.access_token,
			expires_at: Date.now() + parseInt(data.state.expires_in)
		}
	}
	
	state.connector.execute('load_profile', function(result){
		console.log(result);
	});
});