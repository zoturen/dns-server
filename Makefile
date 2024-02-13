prepare:
	@which cargo > /dev/null 2>&1 || (echo "cargo not found, please install it" && exit 1)
	@which dotnet > /dev/null 2>&1 || (echo "dotnet not found, please install it" && exit 1)
	dotnet restore 
	dotnet dev-certs https --trust


run_resolver:
	cd resolver && cargo run --bin resolver

run_data:
	dotnet run --project ./Portare.Data/Portare.Data.csproj --launch-profile https
	
run_web:
	dotnet run --project ./Portare.Web/Portare.Web.csproj --launch-profile https