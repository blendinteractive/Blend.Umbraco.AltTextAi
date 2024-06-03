using System.Runtime.Serialization;

namespace Blend.Umbraco.AltTextAi.Configuration;

[DataContract]
public class AltTextAi
{
    [DataMember(Name="AltTextAiApiKey")]
    public string AltTextAiApiKey { get; set; }
    
    [DataMember(Name="ImageAltTextProperty")]
    public string ImageAltTextProperty { get; set; }
}