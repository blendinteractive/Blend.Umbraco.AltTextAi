using System.Runtime.Serialization;

namespace Blend.Umbraco.AltTextAi.Configuration;

[DataContract]
public class AltTextAi
{
    [DataMember(Name = "AltTextAiApiKey")]
    public string AltTextAiApiKey { get; set; } = string.Empty;

    [DataMember(Name = "ImageAltTextProperty")]
    public string ImageAltTextProperty { get; set; } = string.Empty;

    // the length of the alt text to skip the AI generation
    // shorter than this setting will (re)generate the alt text
    // set to 0 to skip generation if alt text is not empty
    [DataMember(Name = "AltTextLengthToSkip")]
    public int AltTextLengthToSkip { get; set; } = 0;

    // an array of up to 6 keywords to use in the AI generation
    // useful for adding SEO context to the generated alt text
    [DataMember(Name = "AltTextKeyWords")]
    public string[] AltTextKeyWords { get; set; } = Array.Empty<string>();
}