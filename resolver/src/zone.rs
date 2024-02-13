#[derive(Debug)]
pub(crate) struct Zone {
    pub(crate) name: String,
    pub(crate) record_sets: Vec<RecordSet>,
}

#[derive(Debug, Clone)]
pub(crate) enum RecordType {
    A = 1,
    AAAA = 28,
    CNAME = 5,
    MX = 15,
    NS = 2,
    PTR = 12,
    SOA = 6,
    SRV = 33,
    TXT = 16,
}

impl From<u16> for RecordType {
    fn from(item: u16) -> Self {
        match item {
            1 => RecordType::A,
            28 => RecordType::AAAA,
            5 => RecordType::CNAME,
            15 => RecordType::MX,
            2 => RecordType::NS,
            12 => RecordType::PTR,
            6 => RecordType::SOA,
            33 => RecordType::SRV,
            16 => RecordType::TXT,
            _ => panic!("Invalid value for RecordType"),
        }
    }
}

impl Into<u16> for RecordType {
    fn into(self) -> u16 {
        match self {
            RecordType::A => 1,
            RecordType::AAAA => 28,
            RecordType::CNAME => 5,
            RecordType::MX => 15,
            RecordType::NS => 2,
            RecordType::PTR => 12,
            RecordType::SOA => 6,
            RecordType::SRV => 33,
            RecordType::TXT => 16,
        }
    }
}

impl From<i32> for RecordType {
    fn from(item: i32) -> Self {
        match item {
            1 => RecordType::A,
            28 => RecordType::AAAA,
            5 => RecordType::CNAME,
            15 => RecordType::MX,
            2 => RecordType::NS,
            12 => RecordType::PTR,
            6 => RecordType::SOA,
            33 => RecordType::SRV,
            16 => RecordType::TXT,
            _ => panic!("Invalid value for RecordType"),
        }
    }
}
#[derive(Debug, Clone)]
pub(crate) enum RecordClass {
    IN = 1,
    CH = 3,
    HS = 4,
}

impl Into<u16> for RecordClass {
    fn into(self) -> u16 {
        match self {
            RecordClass::IN => 1,
            RecordClass::CH => 3,
            RecordClass::HS => 4,
        }
    }
}

impl From<u16> for RecordClass {
    fn from(item: u16) -> Self {
        match item {
            1 => RecordClass::IN,
            3 => RecordClass::CH,
            4 => RecordClass::HS,
            _ => panic!("Invalid value for RecordClass"),
        }
    }
}

impl From<i32> for RecordClass {
    fn from(item: i32) -> Self {
        match item {
            1 => RecordClass::IN,
            3 => RecordClass::CH,
            4 => RecordClass::HS,
            _ => panic!("Invalid value for RecordClass"),
        }
    }
}

#[derive(Debug)]
pub(crate) enum RCode {
    NoError = 0,
    FormErr = 1,
    ServFail = 2,
    NXDomain = 3,
    NotImp = 4,
    Refused = 5,
}

impl Into<u8> for RCode {
    fn into(self) -> u8 {
        match self {
            RCode::NoError => 0,
            RCode::FormErr => 1,
            RCode::ServFail => 2,
            RCode::NXDomain => 3,
            RCode::NotImp => 4,
            RCode::Refused => 5,
        }
    }
}

#[derive(Debug)]
pub(crate) struct RecordSet {
    pub(crate) name: String,
    pub(crate) record_type: RecordType,
    pub(crate) record_class: RecordClass,
    pub(crate) ttl: u32,
    pub(crate) records: Vec<Record>,
}

#[derive(Debug)]
pub(crate) struct Record {
    pub(crate) content: String,
    pub(crate) is_disabled: bool,
}
