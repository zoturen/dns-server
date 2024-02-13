use tokio::net::UdpSocket;
use crate::dns_packet::DnsPacket;
use crate::resolver::Resolver;

pub struct UdpServer {
    socket: UdpSocket,
    resolver: Resolver,
}

impl UdpServer {
    pub async fn new(address: &str, port: u16, grpc_address: &str) -> Result<UdpServer, Box<dyn std::error::Error>> {
        let socket = UdpSocket::bind(format!("{}:{}", address, port)).await?;
        let resolver = Resolver::new(grpc_address.to_string()).await;
        Ok(UdpServer {
            socket,
            resolver,
        })
    }

    pub async fn start(&mut self) -> Result<(), Box<dyn std::error::Error>> {
        loop {
            let mut buf = [0u8; 1024];
            let (amt, src) = self.socket.recv_from(&mut buf).await?;
            let dns_packet = DnsPacket::from_bytes(&mut buf[..amt]);
            let response = self.handle_request(dns_packet).await;
            self.socket.send_to(&response, &src).await?;
        }
    }

    async fn handle_request(&mut self, dns_packet: DnsPacket) -> Vec<u8> {
        let resolver_results = self.resolver.resolve_answers(&dns_packet).await;
        resolver_results.to_bytes()
        
    }
}