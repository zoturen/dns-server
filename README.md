# DNS server


## Usage

1. `make prepare ` This restores the project and creates local dev certs. Also checks if cargo and dotnet is installed.
2. `make run_data` This starts the data layer server. The backend is Postgres.
3. `make run_web` This starts the web server and exposes a very simple API.
4. `make run_resolver` This starts the DNS resolver. It listens on port 13000 by default and only exposes a UDP port.
5.  Add some data to the database. This is currently done by the web server.
6. `nslookup -q=mx -port=13000 test.com localhost` This should return the MX record if the zone and record is in the database.


## Whats inside

Nothing much as of yet.\
I'm working on this as a side project when I got spare time from school.

