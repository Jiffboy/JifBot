# stop the bot
sudo systemctl stop jifbot.service

# back up the database
mkdir -p backup
cp $JIFBOT_DB backup/BotBase.db

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
cp backup/BotBase.db $JIFBOT_DB

# update the changelog
python3 updateChangelog.py

# start JifBot
sudo systemctl start jifbot.service