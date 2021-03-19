function loadChanges(){
    var json = JSON.parse(JSON.stringify(jifBotChangelog));
	
    for(var i = 0; i < json.length; i++){
		var container = document.createElement('div')
		container.id = "change"
		var date = document.createElement('h3')
		var change = document.createElement('p')
		date.innerHTML = json[i].date
		change.innerHTML = json[i].change
		container.appendChild(date)
		container.appendChild(change)
		document.getElementById("changelog").appendChild(container)
    }
}

window.onload = function() {
    loadChanges();
}