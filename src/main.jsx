class Main extends React.Component {
	constructor() {
		super();
		this.state = {
			guis: {
				error: null
			},
			activeTab: 0
		}
	}
	componentWillMount() {
		socket.emit("load_modules");
		socket.once("load_modules_response", modules => {
			modules.modules.forEach(m => {
				for (let x of m.registeredEvents) {
					if (x === "gui") {
						this.loadGui(m);
						break;
					}
				}
			});
		});

		socket.on('state_changed', this.stateChanged.bind(this));
	}

	stateChanged(args) {
		let hasChanges = false;
		const guis = JSON.parse(JSON.stringify(this.state.guis));
		for (let x in guis) {
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

	loadGui(module) {
		socket.emit('emit', { module: module.name, event: 'gui' });
		socket.once("emit_response", r => {

			var guis = JSON.parse(JSON.stringify(this.state.guis));
			guis[r.module.name] = {
				module: r.module,
				view: r.result
			};
			this.setState({
				guis: guis
			});
		});
	}

	renderTab(key, index) {
		return <li key={index} className={this.state.activeTab === index ? "active":""}>
			<a href="#" onClick={this.tabClicked.bind(this, key, index)}>{key}</a>
		</li>;
	}

	tabClicked(key, index, e) {
		if (e && e.preventDefault) e.preventDefault();
		this.setState({ activeTab: index });
	}

	doSetState(newObj) {
		const moduleName = Object.keys(this.state.guis)[this.state.activeTab];
		const gui = this.state.guis[moduleName];
		for (let x in newObj) {
			gui.module.state[x] = newObj[x];
		}
		this.setState({
			guis: this.state.guis
		});
	}

	render() {
		let Page = null;
		if (this.state.guis && Object.keys(this.state.guis)[this.state.activeTab]) {
			const guiName = Object.keys(this.state.guis)[this.state.activeTab];
			const gui = this.state.guis[guiName];
			if (gui == null) {
				if (guiName == "error")
					Page = <ErrorView />;
			} else 
				Page = <Detail {...gui} setState={this.doSetState.bind(this)} state={gui.module.state} />;
		}
		return <div>
			<ul className="nav nav-tabs">
				{Object.keys(this.state.guis).map(this.renderTab.bind(this))}
			</ul>
			{Page}
		</div>;
	}
}

class ErrorView extends React.Component {
	constructor() {
		super();
		this.state = { errors: [] };
	}
	componentWillMount() {
		console.log("Component will mount");
		const request = new XMLHttpRequest();
		request.onload = r => {
			this.setState({
				errors: JSON.parse(r.target.responseText).rows
			});
		};
		request.open('GET', '/api/error', true);
		request.send(null);
	}

	renderErrorDefinition(data, key, index) {
		let value = data[key];
		if (typeof(value) === "object") {
			value = <dl className="dl-horizontal">{Object.keys(value).map(this.renderErrorDefinition.bind(this, value))}</dl>;
		}
		return [
			<dt key={index + '_1'}>{key}</dt>,
			<dd key={index + '_2'}>{value}</dd>
		];
	}

	renderErrorRow(error) {
		return <tr key={error.id}>
			<td>{new Date(error.value.time).toGMTString()}</td>
			<td>{error.value.error}</td>
			<td>
				<dl className="dl-horizontal">
					{Object.keys(error.value)
						   .filter(v => v != '_id' && v != 'error' && v != 'time' && v != '_rev')
						   .map(this.renderErrorDefinition.bind(this, error.value))}
				</dl>
			</td>
		</tr>
	}

	render() {
		return <div>
			<h2>Error overview</h2>
			<table className="table table-condensed table-striped table-hover">
				<thead>
					<tr>
						<th>Time</th>
						<th>Message</th>
						<th>Additional data</th>
					</tr>
				</thead>
				<tbody>
					{this.state.errors.map(this.renderErrorRow.bind(this))}
				</tbody>
			</table>
		</div>;
	}
}

class Detail extends React.Component {
	renderViewItem(item, index) {
		if (Array.isArray(item)) {
			return <div key={index}>{item.map(this.renderViewItem.bind(this))}</div>;
		}
		switch(item.type) {
			case 'text':
				if(item.data)
					return <span key={index }>{this.getStateValue(item.data)}</span>;
				return <span key={index }>{item.text}</span>;
			case 'row':
				return <div>{this.renderViewItem(item.children, 0)}</div>;
			case 'input':
				return <input type="text" value={this.getStateValue(item.boundField)} onChange={this.inputChanged.bind(this, item) } />;
			case 'list':
				return <ul>
					{(this.getStateValue(item.data) || []).map(this.renderListItem.bind(this, item))}
				</ul>;
			case 'button':
				return <input type="button" className="btn btn-default" value={item.text} onClick={this.buttonClicked.bind(this, item)} />
		}
		return <div><i key={index}>Unknown type: {item.type}</i></div>;
	}

	getStateValue(field) {
		if (!this.props.state) return null;
		if (field == '*') return this.props.state;
		return this.props.state[field];
	}

	renderListItem(item, row, index) {
		return <li key={index}>
			<Detail module={this.props.module} state={row} view={item.display} setState={this.stateChanged.bind(this, item, index) } />
		</li>;
	}

	stateChanged(item, index, newValue) {
		console.log('stateChanged', item, index);
	}

	inputChanged(item, e) {
		const obj = {};
		obj[item.boundField] = e.target.value;
		this.props.setState(obj);
	}

	buttonClicked(item, e) {
		if (e && e.preventDefault) e.preventDefault();
		console.log(item, 'clicked');
		socket.emit('emit', {
			module: this.props.module.name,
			event: item.clickEvent,
			arguments: [this.props.module.state]
		});
	}

	render() {
		return <div>
			{this.renderViewItem(this.props.view, 0)}
		</div>
	}
}

var element = document.createElement("div");
ReactDOM.render(<Main />, element);
document.body.appendChild(element);