use crate::zone::{RecordClass, RecordSet, RecordType};

#[derive(Debug)]
pub(crate) struct Header {
    // https://www.rfc-editor.org/rfc/rfc6895.html#page-3
    pub(crate) id: u16,
    pub(crate) qr: bool, // Query/Response
    pub(crate) opcode: u8, // Operation Code
    pub(crate) aa: bool, // Authoritative DnsAnswer
    pub(crate) tc: bool, // TrunCation
    pub(crate) rd: bool, // Recursion Desired
    pub(crate) ra: bool, // Recursion Available
    pub(crate) z: u8, // Reserved
    pub(crate) rcode: u8, // Response Code
    pub(crate) qdcount: u16, // DnsQuestion Count
    pub(crate) ancount: u16, // DnsAnswer Count
    pub(crate) nscount: u16, // DnsAuthority Count
    pub(crate) arcount: u16, // DnsAdditional Count
}

impl Header {
    fn to_bytes(&self) -> Vec<u8> {
        vec![
            (self.id >> 8) as u8,
            self.id as u8,
            (self.qr as u8) << 7 | self.opcode << 3 | (self.aa as u8) << 2 | (self.tc as u8) << 1 | self.rd as u8,
            (self.ra as u8) << 7 | self.z << 4 | self.rcode,
            (self.qdcount >> 8) as u8,
            self.qdcount as u8,
            (self.ancount >> 8) as u8,
            self.ancount as u8,
            (self.nscount >> 8) as u8,
            self.nscount as u8,
            (self.arcount >> 8) as u8,
            self.arcount as u8,
        ]
    }

    fn from_bytes(bytes: &[u8]) -> Header {
        Header {
            id: (bytes[0] as u16) << 8 | bytes[1] as u16,
            qr: (bytes[2] & 0b10000000) != 0,
            opcode: bytes[2] & 0b01111000,
            aa: (bytes[2] & 0b00000100) != 0,
            tc: (bytes[2] & 0b00000010) != 0,
            rd: (bytes[2] & 0b00000001) != 0,
            ra: (bytes[3] & 0b10000000) != 0,
            z: (bytes[3] & 0b01110000) >> 4,
            rcode: bytes[3] & 0b00001111,
            qdcount: (bytes[4] as u16) << 8 | bytes[5] as u16,
            ancount: (bytes[6] as u16) << 8 | bytes[7] as u16,
            nscount: (bytes[8] as u16) << 8 | bytes[9] as u16,
            arcount: (bytes[10] as u16) << 8 | bytes[11] as u16,
        }
    }
}

#[derive(Debug, Clone)]
pub(crate) struct Question {
    pub(crate) qname: String,
    // Domain Name
    pub(crate) qtype: u16,
    // Query Type
    pub(crate) qclass: u16, // Query Class
}

impl Question {
    pub  fn get_name_offset(&self) -> u16 {
        0xC000 | (self.qname.len() as u16 + 1)
    }
    fn to_bytes(&self) -> Vec<u8> {
        let mut bytes = Vec::new();
        for label in self.qname.split('.') {
            bytes.push(label.len() as u8);
            bytes.extend(label.as_bytes());
        }
        bytes.push(0);
        bytes.extend(&[
            (self.qtype >> 8) as u8,
            self.qtype as u8,
            (self.qclass >> 8) as u8,
            self.qclass as u8,
        ]);
        bytes
    }

    fn from_bytes(bytes: &[u8]) -> Question {
        let mut qname = String::new();
        let mut i = 0;
        loop {
            let len = bytes[i] as usize;
            if len == 0 {
                break;
            }
            if i > 0 {
                qname.push('.');
            }
            qname.push_str(std::str::from_utf8(&bytes[i + 1..i + 1 + len]).unwrap());
            i += len + 1;
        }
        let qtype = (bytes[i + 1] as u16) << 8 | bytes[i + 2] as u16;
        let qclass = (bytes[i + 3] as u16) << 8 | bytes[i + 4] as u16;
        Question {
            qname,
            qtype,
            qclass,
        }
    }
}


#[derive(Debug, Clone)]
pub(crate) struct Answer {
    pub(crate) name_ptr: u16, // Domain Name
    pub(crate) atype: u16, // DnsAnswer Type
    pub(crate) aclass: u16, // DnsAnswer Class
    pub(crate) ttl: u32, // Time to Live
    pub(crate) rdlength: u16, // Resource Data Length
    pub(crate) rdata: Vec<u8>, // Resource Data
}

impl Answer {
    pub(crate) fn from_record_set(name_offset: u16, record_set: RecordSet) -> Vec<Answer> {
        let mut answers = Vec::new();

        for record in record_set.records {
            let rdata = match record_set.record_type {
                RecordType::A => {
                    let mut rdata = Vec::new();
                    for octet in record.content.split('.') {
                        rdata.push(octet.parse::<u8>().unwrap());
                    }
                    rdata
                }
                RecordType::AAAA => {
                    let mut rdata = Vec::new();
                    for hextet in record.content.split(':') {
                        let hextet = u16::from_str_radix(hextet, 16).unwrap();
                        rdata.push((hextet >> 8) as u8);
                        rdata.push(hextet as u8);
                    }
                    rdata
                },
                RecordType::CNAME => {
                    let mut rdata = Vec::new();
                    rdata.extend(label_to_bytes(&record.content));
                    rdata
                },
                RecordType::MX => {
                    let mut rdata = Vec::new();
                    let mut content = record.content.split(' ');
                    let preference = content.next().unwrap().parse::<u16>().unwrap();
                    rdata.push((preference >> 8) as u8);
                    rdata.push(preference as u8);
                    let exchange = content.next().unwrap();
                    rdata.extend(label_to_bytes(exchange));
                    rdata
                },
                _ => Vec::new(),
            };
            
            fn label_to_bytes(label: &str) -> Vec<u8> {
                let mut bytes = Vec::new();
                for label in label.split('.') {
                    bytes.push(label.len() as u8);
                    bytes.extend(label.as_bytes());
                }
                bytes
            }

            let answer = Answer {
                name_ptr: name_offset,
                atype: RecordType::into(record_set.record_type.clone()),
                aclass: RecordClass::into(record_set.record_class.clone()),
                ttl: record_set.ttl,
                rdlength: rdata.len() as u16,
                rdata,
            };

            answers.push(answer);
        }
        
        answers
    }
    fn to_bytes(&self) -> Vec<u8> {
        let mut bytes = Vec::new();
        bytes.extend(&[
            (self.name_ptr >> 8) as u8,
            self.name_ptr as u8,
            (self.atype >> 8) as u8,
            self.atype as u8,
            (self.aclass >> 8) as u8,
            self.aclass as u8,
            (self.ttl >> 24) as u8,
            (self.ttl >> 16) as u8,
            (self.ttl >> 8) as u8,
            self.ttl as u8,
            (self.rdlength >> 8) as u8,
            self.rdlength as u8,
        ]);
        bytes.extend(&self.rdata);
        bytes
    }
}


#[derive(Debug)]
pub(crate) struct DnsPacket {
    pub(crate) header: Header,
    pub(crate) questions: Vec<Question>, // Dns pretty much never has more than one question
    pub(crate) answers: Vec<Answer>, // So this should pretty much never have more than one answer
    pub(crate) authorities: Vec<Answer>,
    pub(crate) additionals: Vec<Answer>,
}

impl DnsPacket {
    pub(crate) fn from_bytes(bytes: &mut [u8]) -> DnsPacket {
        let header = Header::from_bytes(&bytes[0..12]);

        let mut questions = Vec::new();

        if header.qdcount == 1 {
            questions.push(Question::from_bytes(&bytes[12..]));
        }

        else {
            let mut i = 12;
            for _ in 0..header.qdcount {
                let question = Question::from_bytes(&bytes[i..]);
                i += question.qname.len() + 5;  // 5 = 2 (qtype) + 2 (qclass) + 1 (0)
                questions.push(question);
            }
        }

        let mut answers = Vec::new();
        let mut authorities = Vec::new();
        let mut additionals = Vec::new();


        DnsPacket {
            header,
            questions,
            answers,
            authorities,
            additionals,
        }
    }
    
    pub(crate) fn to_bytes(&self) -> Vec<u8> {
        let mut bytes = Vec::new();
        bytes.extend(&self.header.to_bytes());
        for question in &self.questions {
            bytes.extend(&question.to_bytes());
        }
        for answer in &self.answers {
            bytes.extend(&answer.to_bytes());
        }
        for authority in &self.authorities {
            bytes.extend(&authority.to_bytes());
        }
        for additional in &self.additionals {
            bytes.extend(&additional.to_bytes());
        }
        bytes
    }
}