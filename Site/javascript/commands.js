var selectedButton = "All"

function loadButtons(){
    var json = JSON.parse(JSON.stringify(jifBotCommands));
    var found = []

    var button = document.createElement('button')
    button.innerHTML = "All"
    button.id = "All"
    button.onclick = function(){
        loadCommands("All")
    }

    document.getElementById("commandButtons").appendChild(button)

    for(var i = 0; i < json.length; i++){
        if(json[i].category == "Hidden"){
            continue;
        }
        else if(!found.includes(json[i].category)){
            var cat = json[i].category
            var button = document.createElement('button')
            found.push(cat)
            button.innerHTML = cat
            button.id = cat
            button.onclick = function(){
                
                loadCommands(event.srcElement.innerHTML)
            }
            document.getElementById("commandButtons").appendChild(button)
        }
    }
}

function loadCommands(category){
    document.getElementById(selectedButton).style.boxShadow = ""
    document.getElementById(category).style.boxShadow = "0 3px #ffa200";
    selectedButton = category
    var json = JSON.parse(JSON.stringify(jifBotCommands));
    document.getElementById("commands").innerHTML = ""
    var command = document.createElement('th')
    var description = document.createElement('th')
    command.innerHTML = "Command"
    description.innerHTML = "Description"
    document.getElementById("commands").appendChild(command)
    document.getElementById("commands").appendChild(description)
    for(var i = 0; i < json.length; i++){
        if(json[i].category == "Hidden"){
            continue;
        }
        else if(json[i].category == category || category == "All")
        {
            var row = document.createElement('tr')
            var command = document.createElement('td')
            var description = document.createElement('td')
            command.innerHTML = json[i].command
            var alias = ""
            if("alias" in json[i]){
				alias = "<br>" + json[i].alias.replaceAll(",","<br>");
			}
            command.innerHTML +='<span style="color:#ffe199;">' + alias + "</span>"
            description.innerHTML = json[i].description.replaceAll("\n","<br>").replaceAll("```","<br>");
            description.innerHTML += "<br>" + '<span style="color:#ffefcc;">' + json[i].usage.replaceAll("\n","<br>").replaceAll(",","<br>") + "</span>"
            row.appendChild(command)
            row.appendChild(description)
            document.getElementById("commands").appendChild(row)
        }
    }
}

window.onload = function() {
    loadButtons();
    loadCommands("All");
}
