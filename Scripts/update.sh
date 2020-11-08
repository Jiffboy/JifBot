sudo systemctl stop jifbot.service
mkdir -p backup
cp ../bin/Debug/netcoreapp3.1/Database/* backup/
cd ..
git pull
dotnet build 
cd scripts
read -p "Make database updates and press enter to continue..."
cp backup/* ../bin/Debug/netcoreapp3.1/Database/
sudo systemctl start jifbot.service

