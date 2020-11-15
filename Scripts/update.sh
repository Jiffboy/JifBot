# stop the bot
sudo systemctl stop jifbot.service

# back up the database
mkdir -p backup
cp ../bin/Debug/netcoreapp3.1/Database/* backup/

# build any new changes
cd ..
git pull
dotnet build 

# allow manual changes to the database and start the bot
cd Scripts
read -p "Make database updates and press enter to continue..."
cp backup/* ../bin/Debug/netcoreapp3.1/Database/
rm ../bin/Debug/netcoreapp3.1/commands.js
sudo systemctl start jifbot.service

# get the latest verions of the website
cd ../../Vertigeux.github.io/javascript
git pull

# generate js file for website
python3 generatejs.py

# if the generated version of commands.js is different, update website
if [ -n "$(cmp commands.js ../../JifBot/Scripts/backup/commands.js)" ]
then
    cp ../../JifBot/bin/Debug/netcoreapp3.1/commands.js commands.js
    git add commands.js
    git commit -m"Automatic command update from Jif Bot"
    git push
fi
