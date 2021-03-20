function loadChanges(){
    var json = JSON.parse(JSON.stringify(jifBotChangelog));
	let logs = new Map();
    for(var i = 0; i < json.length; i++){
		if (logs.has(json[i].date)){
			logs.set(json[i].date, logs.get(json[i].date) + '<br><span style="color:#ffe199;">> </span>' + json[i].change);
		}
		else{
			logs.set(json[i].date, '<span style="color:#ffe199;">> </span>' + json[i].change)
		}
    }
	
	logs.forEach(function(value, key){
		var container = document.createElement('div');
		container.id = "change";
		var date = document.createElement('h3');
		date.id = "changedate"
		var change = document.createElement('p');
		change.id = "changeinfo"
		var dateItems = key.split("-")
		date.innerHTML = dateItems[1] + "." + dateItems[2] + "." + dateItems[0];
		change.innerHTML = value;
		container.appendChild(date);
		container.appendChild(change);
		document.getElementById("changelog").appendChild(container);
	});
}

window.onload = function() {
    loadChanges();
}