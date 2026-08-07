using System.Runtime.Serialization;

namespace Server.API.Enums;

public enum ImageType
{
    [EnumMember(Value = "banner")]
    Banner,
    [EnumMember(Value = "normal")]
    Normal,
    [EnumMember(Value = "square")]
    Square,
    [EnumMember(Value = "icon")]
    Icon
}