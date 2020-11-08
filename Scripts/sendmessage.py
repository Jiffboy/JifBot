import sys
import json
import discord
import sqlite3

channelId = sys.argv[1]
message   = sys.argv[2]

connection = sqlite3.connect("../bin/Debug/netcoreapp3.1/Database/BotBase.db")
cursor = connection.cursor()
cursor.execute("SELECT Token FROM Configuration WHERE Name = 'Live'")
token = cursor.fetchone()[0]
cursor.close()
connection.close()

client = discord.Client()

@client.event
async def on_ready():
    print("'" + channelId + "'")
    channel = client.get_channel(int(channelId))
    try:
        await channel.send(message)
        await sys.exit()
    except:
        await sys.exit()

client.run(token)
