import sys
import json
import discord
import sqlite3

json = "var jifBotCommands = ["

connection = sqlite3.connect("../bin/Debug/netcoreapp3.1/Database/BotBase.db")
cursor = connection.cursor()

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