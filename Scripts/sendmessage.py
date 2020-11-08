import sys
import json
import discord

channelId = sys.argv[1]
message   = sys.argv[2]


jsonFile = open("../bin/Debug/netcoreapp3.0/configuration/config.json", "r")
json = json.loads(jsonFile.read())
token = json["Token"]

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
