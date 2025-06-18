namespace EncodeHLS;

public class EncodingProfile
{
    public string Name { get; set; } = "";
    public int Bitrate { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}
