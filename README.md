# Tmds.Systemd.Tool
(Experimental) global tool for interacting with systemd

## Example

Install the tool:

```
dotnet tool uninstall -g Tmds.Systemd.Tool
dotnet tool install -g Tmds.Systemd.Tool --add-source https://www.myget.org/F/tmds/api/v3/index.json --version '0.1.0-*'
```

Create a web application
```
dotnet new web -o web
cd web
dotnet publish -c Release
```

Create a systemd service:
```
# system service:
sudo ~/.dotnet/tools/dotnet-systemd create-service --name webapp --execstart bin/Release/netcoreapp*/web.dll
# user service:
dotnet-systemd create-service --user --name webapp --execstart bin/Release/netcoreapp*/web.dll
```

For more options, run:
```
dotnet-systemd create-service --help
```
