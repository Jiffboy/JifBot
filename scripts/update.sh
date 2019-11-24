sudo systemctl stop jifbot.service
mkdir -p backup
cp ../bin/Debug/netcoreapp3.0/references/* backup/
cd ..
git pull
dotnet build 
cp scripts/backup/* bin/Debug/netcoreapp3.0/references/
sudo systemctl start jifbot.service
