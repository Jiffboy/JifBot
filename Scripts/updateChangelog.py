import sqlite3
import os
from datetime import datetime

connection = sqlite3.connect(os.environ['JIFBOT_DB'])
cursor = connection.cursor()

today = datetime.today().strftime('%Y-%m-%d')
print("Type 'done' when done")
change = input("Enter change:")
while change != "done":
    typeinput = input("Enter type [((n)ew, (i)improved, (b)ug fix, (r)emoved]: ")
    type = ""
    if typeinput == "n":
        type = "New"
    elif typeinput == "i":
        type = "Improved"
    elif typeinput == "b":
        type = "Bug Fix"
    elif typeinput == "r":
        type = "Removed"
    cursor.execute("INSERT INTO ChangeLog(Date,Change,Type) VALUES('" + today + "','" + change + "','" + type + "')")
    connection.commit()
    change = input("Enter change:")