prepare:
	dotnet restore 
	dotnet dev-certs https --trust


run_resolver:
	dotnet run --project ./Portare.Resolver/Portare.Resolver.csproj --launch-profile https

run_data:
	dotnet run --project ./Portare.Data/Portare.Data.csproj --launch-profile https
	
run_web:
	dotnet run --project ./Portare.Web/Portare.Web.csproj --launch-profile https