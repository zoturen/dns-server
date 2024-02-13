

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let proto_path = "../proto/data_zone.proto";
    tonic_build::compile_protos(proto_path)?;
    Ok(())
}