namespace EncodeHLS;

public class FFmpegOptions
{
    public string? ExecutablesPath { get; set; }
    public string VideoEncoder { get; set; } = "libx264";
}
