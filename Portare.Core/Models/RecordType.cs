
namespace Portare.Core.Models;

public enum RecordType
{
    A = 0x0001,
    NS = 0x0002,
    CNAME = 0x0005,
    SOA = 0x0006,
    PTR = 0x000c,
    MX = 0x000f,
    TXT = 0x0010,
    AAAA = 0x001c,
    SRV = 0x0021,
    ANY = 0x00ff
}