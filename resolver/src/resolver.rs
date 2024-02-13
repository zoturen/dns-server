use tonic::transport::Channel;
use tonic_types::StatusExt;
use crate::datazone;
use crate::datazone::data_zone_client::DataZoneClient;
use crate::dns_packet::{DnsPacket, Header, Question, Answer};
use crate::datazone::GrpcStatusResponse;
use crate::zone::{RecordSet, Record, RecordType, RecordClass, RCode};

pub(crate) struct Resolver {
    data_zone_client: DataZoneClient<Channel>,
}

impl Resolver {
    pub(crate) async fn new(grpc_data_addr: String) -> Resolver {
        Resolver {
            data_zone_client: DataZoneClient::connect(grpc_data_addr).await.unwrap_or_else(|err| {
                panic!("Failed to connect to data zone: {}", err)

            }),
        }
    }
    pub(crate) async fn resolve_answers(&mut self, query_packet: &DnsPacket) -> DnsPacket {

        let mut answers: Vec<Answer> = vec![];
        let mut rcode: RCode = RCode::Refused;
        for question in query_packet.questions.iter() {
            let question_name = &question.qname;
            let question_type = question.qtype;
            let record_type = RecordType::from(question_type);
            let record_set_result = Self::resolve_record_set(self, record_type, question_name.to_string()).await;
            match record_set_result {
                Ok((record_set, mut code)) => {
                    match code {
                        RCode::NoError => {
                            answers = Answer::from_record_set(question.get_name_offset(), record_set);
                            rcode = RCode::NoError;
                        }
                        RCode::FormErr => {
                            rcode = RCode::FormErr;
                        }
                        RCode::ServFail => {
                            rcode = RCode::ServFail;
                        }
                        RCode::NXDomain => {
                            rcode = RCode::NXDomain;
                        }
                        RCode::NotImp => {
                            rcode = RCode::NotImp;
                        }
                        RCode::Refused => {
                            println!("Refused");
                            rcode = RCode::Refused;
                        }
                    }
                },
                Err(err) => {
                    println!("Error resolving record set: {}", err);
                }
            }
        }
       
        let questions = query_packet.questions.clone();
        
        let mut answer_packet = DnsPacket {
            header: Header {
                id: query_packet.header.id,
                qr: true,
                opcode: 0,
                aa: false,
                tc: false,
                rd: false,
                ra: false,
                z: 0,
                rcode: RCode::into(rcode),
                qdcount: query_packet.header.qdcount,
                ancount: answers.len() as u16,
                nscount: 0,
                arcount: 0,
            },
            questions,
            answers,
            authorities: vec![],
            additionals: vec![],
        };
        answer_packet
    }

    async fn resolve_record_set(&mut self, record_type: RecordType, name: String) -> Result<(RecordSet, RCode), String> {
        let mut name = name;
        if !name.ends_with(".") { 
            name.push_str(".");
        }
        let request = tonic::Request::new(datazone::GetRecordSetByNameRequest {
            record_set_name: name,
            record_type: record_type as i32,
        });
        

        let mut client = DataZoneClient::connect("https://localhost:7049").await.map_err(|err| format!("Failed to connect to data zone: {}", err))?;
        let grpc_response = match client.get_record_set_by_name(request).await {
            Ok(response) => response,
            Err(status) => {
                println!(" Error status received. Extracting error details...\n");

                let err_details = status.get_error_details();

                if let Some(bad_request) = err_details.bad_request() {
                    // Handle bad_request details
                    println!(" {:?}", bad_request);
                }
                if let Some(help) = err_details.help() {
                    // Handle help details
                    println!(" {:?}", help);
                }
                if let Some(localized_message) = err_details.localized_message() {
                    // Handle localized_message details
                    println!(" {:?}", localized_message);
                }

                println!();
                return Err(format!("Failed to get record set: {}", status));
            }
        };


        let response = grpc_response.into_inner();
        let rcode: RCode;
        let mut record_set: RecordSet = RecordSet {
            name: "".to_string(),
            record_type: RecordType::A,
            record_class: RecordClass::IN,
            ttl: 0,
            records: vec![],
        };
        match response.status.unwrap() {
            GrpcStatusResponse { message, status } => {
                match status {
                    0 => {
                        let response = response.record_set.unwrap();
                        rcode = RCode::NoError;
                        record_set = RecordSet {
                            name: response.name,
                            record_type: RecordType::from(response.record_type),
                            record_class: RecordClass::from(response.record_class),
                            ttl: response.ttl,
                            records: response.content.iter().map(|content| Record {
                                content: content.content.clone(),
                                is_disabled: content.is_disabled,
                            }).collect()
                        };
                    },
                    1 => rcode = RCode::FormErr,
                    2 => rcode = RCode::NXDomain,
                    _ => rcode = RCode::ServFail,
                }
            }
        };

        Ok((record_set, rcode))
    }
}