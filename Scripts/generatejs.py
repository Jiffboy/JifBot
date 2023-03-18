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
    json += '","description":"' + cmd[2].replace('\n', '\\n').replace('"', r'\"') + '"'
        
    json += "},"

json = json[:-1] + "]"

file = open("backup/database.js", "w", encoding="utf-8")

json += "\nvar jifBotChangelog = ["
cursor.execute('SELECT * FROM ChangeLog ORDER BY Date DESC')
changes = cursor.fetchall()

for change in changes:
    json += '{"date":"' + change[0]
    json += '","change":"' + change[1].replace('"', r'\"') + '"},'
json = json[:-1] + "]"

file.write(json)

file.close()
cursor.close()
connection.close()