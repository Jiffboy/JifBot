import sqlite3
import time

json = "var jifBotCommands = ["

connection = sqlite3.connect("../bin/Debug/netcoreapp3.1/Database/BotBase.db")
cursor = connection.cursor()

cursor.execute('SELECT Value FROM Variable WHERE Name = "lastCmdUpdateTime"')
lastUpdate = cursor.fetchone()
currUpdate = lastUpdate
print("Waiting for Commands to be populated...")
while(currUpdate == lastUpdate):
    cursor.execute('SELECT Value FROM Variable WHERE Name = "lastCmdUpdateTime"')
    currUpdate = cursor.fetchone()
    time.sleep(0.5)


cursor.execute("SELECT * FROM Command")
commands = cursor.fetchall()

for cmd in commands:
    json += '{"command":"' + cmd[0]
    json += '","category":"' + cmd[1]
    json += '","usage":"' + cmd[2].replace('\n', '\\n').replace('"', r'\"')
    json += '","description":"' + cmd[3].replace('\n', '\\n').replace('"', r'\"') + '"'
    
    cursor.execute("SELECT Alias FROM CommandAlias WHERE Command = '" + cmd[0] + "'")
    alias = cursor.fetchall()
    
    if len(alias) > 0:
        json += ',"alias":"'
        for ali in alias:
            json += ali[0] + ", "
        json = json[:-2] + r'"'
        
    json += "},"

json = json[:-1] + "]"

file = open("backup/commands.js", "w", encoding="utf-8")
file.write(json)

file.close()
cursor.close()
connection.close()