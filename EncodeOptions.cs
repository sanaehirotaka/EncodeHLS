namespace EncodeHLS;

public class EncodeOptions
{
    public IList<string> InputFiles { get; set; } = [];

    public string OutputDir { get; set; } = default!;
}
