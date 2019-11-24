sudo systemctl stop jifbot.service
mkdir -p backup
cp ../bin/Debug/netcoreapp3.0/references/* backup/
cd ..
git pull
dotnet build 
sudo systemctl start jifbot.service
