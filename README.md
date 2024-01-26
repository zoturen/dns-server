# DNS server


## Usage

1. ` cd Server && dotnet run ` This starts the server and listens to Udp port 13000 and exposes a empty Web api for now.
2. `nslookup -q=mx -port=13000 test.com localhost` Yeah we can reply with a MX record.


## Whats inside

1. Not much.
2. A, CNAME, MX. These are serializable from a object to an answer. 
