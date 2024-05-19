import sqlite3
import os
from datetime import datetime

connection = sqlite3.connect(os.environ['JIFBOT_DB'])
cursor = connection.cursor()

today = datetime.today().strftime('%Y-%m-%d')
print("Type 'done' when done")
change = input("Enter change:")
while change != "done":
    cursor.execute("INSERT INTO ChangeLog(Date,Change) VALUES('" + today + "','" + change + "')")
    connection.commit()
    change = input("Enter change:")