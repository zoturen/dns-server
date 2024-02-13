use resolver::udp_server::UdpServer;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let address = "127.0.0.1";
    let port = 13000;

    let mut udp_server = UdpServer::new(address, port, "https://localhost:7049").await?;
    udp_server.start().await.expect("Could not start udp server.");
    
    Ok(())
}
