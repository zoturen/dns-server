pub mod dns_packet;
pub mod udp_server;
pub mod resolver;
pub mod datazone {
    tonic::include_proto!("datazone"); 
}

pub mod zone;