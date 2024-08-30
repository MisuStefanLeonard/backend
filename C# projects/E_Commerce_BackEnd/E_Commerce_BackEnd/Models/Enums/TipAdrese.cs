using System.Runtime.Serialization;

namespace E_Commerce_BackEnd.Models.Enums;

public enum TipAdrese
{
    [EnumMember(Value = "Livrare")]
    Livrare,
    [EnumMember(Value = "Facturare")]
    Facturare
}