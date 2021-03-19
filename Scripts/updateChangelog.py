import sqlite3
from datetime import datetime

connection = sqlite3.connect("../bin/Debug/netcoreapp3.1/Database/BotBase.db")
cursor = connection.cursor()

today = datetime.today().strftime('%Y-%m-%d')
print("Type 'done' when done")
change = input("Enter change:")
while change != "done":
    cursor.execute("INSERT INTO ChangeLog(Date,Change) VALUES('" + today + "','" + change + "')")
    connection.commit()
    change = input("Enter change:")