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
sudo systemctl start jifbot.service

# get the latest verions of the website
cd ../../Vertigeux.github.io/javascript
git pull

# wait for Jif bot to generate commands.js
echo "Waiting for commands.js to exist..."
until [ -f ../../JifBot/bin/Debug/netcoreapp3.1/commands.js ]
do
	sleep 1
done

# if the generated version of commands.js is different, update website
if [ -n "$(cmp commands.js ../../JifBot/bin/Debug/netcoreapp3.1/commands.js)" ]
then
    cp ../../JifBot/bin/Debug/netcoreapp3.1/commands.js commands.js
    git add commands.js
    git commit -m"Automatic command update from Jif Bot"
    git push
    rm ../../JifBot/bin/Debug/netcoreapp3.1/commands.js
fi
