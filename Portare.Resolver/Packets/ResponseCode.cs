namespace Portare.Resolver.Packets;

// https://www.rfc-editor.org/rfc/rfc6895.html#section-2.3
public enum ResponseCode
{
    NoError = 0, 
    FormError = 1, 
    ServFail = 2, // Server Failure
    NXDomain = 3, // Non-Existent Domain
    NotImplemented = 4,
    Refused = 5,
    YXDomain = 6, // Name Exists when it should not
    YXRRSet = 7, // RR Set Exists when it should not
    NXRRSet = 8, // RR Set that should exist does not
    NotAuth = 9, // Server Not Authoritative for zone
    NotZone = 10, // Name not in zone
    BadVers = 16, // Bad OPT Version
    BadSig = 16, // TSIG Signature Failure
    BadKey = 17, // Key not recognized
    BadTime = 18, // Signature out of time window
    BadMode = 19, // Bad TKEY Mode
    BadName = 20, // Duplicate key name
    BadAlg = 21, // Algorithm not supported
    BadTrunc = 22, // Bad Truncation
}