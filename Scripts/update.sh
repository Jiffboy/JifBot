# stop the bot
sudo systemctl stop jifbot.service

# back up the database
mkdir -p backup
cp ../bin/Debug/netcoreapp3.1/Database/* backup/

# update some environment variables just in case
export DOTNET_ROOT=$HOME/dotnet
export PATH=$PATH:$HOME/dotnet

# build any new changes
cd ..
git pull
dotnet build 

# allow manual changes to the database and start the bot
cd Scripts
read -p "Make database updates and press enter to continue..."
cp backup/* $JIFBOT_DB

# update the changelog
python3 updateChangelog.py

# start JifBot
sudo systemctl start jifbot.service

# generate js file for website
python3 generatejs.py

# get the latest verions of the website
#cd ../../Vertigeux.github.io/javascript
#git pull

# if the generated version of commands.js is different, update website
if [ -n "$(cmp ../Site/javascript/database.js ../../JifBot/Scripts/backup/database.js)" ]
then
    cp ../../JifBot/Scripts/backup/database.js ../Site/javascript/database.js
    #git ../Site/javascript/commands.js
    #git commit -m"Automatic command update from Jif Bot"
    #git push
fi
