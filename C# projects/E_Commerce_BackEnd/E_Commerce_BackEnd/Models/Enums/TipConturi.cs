using System.Runtime.Serialization;

namespace E_Commerce_BackEnd.Models.Enums;

public enum TipConturi
{
    [EnumMember(Value = "Inregistrat")]
    Registered,
    [EnumMember(Value = "Neinregistrat")]
    Guest
}