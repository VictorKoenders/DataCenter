function init(){
	setup();
}

function update(){
	setup();
}

function setup(){
	state.episode = 25;
	state.url = "www.google.com"
	HtmlChecker({
		url: "http://www.crunchyroll.com/food-wars-shokugeki-no-soma",
		selector: "a[contains(@class, 'episode')]",
		check: {
			element: CreateHtmlNodeDataFinder("span"),
			equals: "Episode 23"
		},
		responseData: [
			CreateAttributeDataFinder("href"),
			CreateHtmlNodeDataFinder("span")
		],
		interval: 3600,
		eventName: "new_foodwars_episode"
	})
}

on("new_foodwars_episode", function(data){
	console.log("New episode of Food Wars! " + data.responseData[1] + " at http://www.crunchyroll.com" + data.responseData[0]);
	state.episode = data.responseData[1];
	state.url = data.responseData[0];
})

on("gui", function(){
	return [{
		type: 'row',
		children: [{
			type: 'text',
			text: 'episode: ',
		}, {
			type: 'text',
			data: 'episode',
		}]
	}, {
		type: 'row',
		children: [{
			type: 'text',
			text: 'url: ',
		}, {
			type: 'text',
			data: 'url'
		}]
	}];
});