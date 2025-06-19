using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;
using Xabe.FFmpeg;

namespace EncodeHLS;

public class HlsEncodingService
{
    private FFmpegOptions FFmpegOptions { get; }

    private List<EncodingProfile> EncodingProfiles { get; }

    private EncodeOptions EncodeOptions { get; }

    public HlsEncodingService(IOptions<FFmpegOptions> ffmpegOptions, IOptions<List<EncodingProfile>> encodingProfiles, EncodeOptions encodeOptions)
    {
        FFmpegOptions = ffmpegOptions.Value;
        EncodingProfiles = encodingProfiles.Value;
        EncodeOptions = encodeOptions;
    }

    public async Task EncodeAsync()
    {
        if (!string.IsNullOrEmpty(FFmpegOptions.ExecutablesPath))
        {
            FFmpeg.SetExecutablesPath(FFmpegOptions.ExecutablesPath);
        }
        foreach (var inputFile in EncodeOptions.InputFiles)
        {
            await EncodeMainAsync(new FileInfo(inputFile));
        }
    }

    private async Task EncodeMainAsync(FileInfo inputFile)
    {
        var outputDirectory = new DirectoryInfo(Path.Combine(EncodeOptions.OutputDir, Path.GetFileNameWithoutExtension(inputFile.Name)));

        foreach (var profile in EncodingProfiles)
        {
            var profileOutputDirectory = outputDirectory.CreateSubdirectory(profile.Name);
            var playlistPath = Path.Combine(profileOutputDirectory.FullName, "playlist.m3u8");

            Console.WriteLine($"-- Starting {profile.Name} ({profile.Width}x{profile.Height} @ {profile.Bitrate / 1000}kbps) --");

            var conversion = FFmpeg.Conversions.New()
                .AddParameter($"-i \"{inputFile.FullName}\"")
                .AddParameter($"-b:v {profile.Bitrate}")
                .AddParameter($"-b:a 128000")
                .AddParameter($"-s {profile.Width}x{profile.Height}")
                .AddParameter($"-c:v {FFmpegOptions.VideoEncoder}")
                .AddParameter("-c:a aac")
                .AddParameter("-f hls")
                .AddParameter("-hls_time 10")
                .AddParameter("-hls_list_size 0")
                .AddParameter($"-hls_segment_filename \"{Path.Combine(profileOutputDirectory.FullName, "segment%03d.ts")}\"")
                .SetOutput(playlistPath)
                .PipeOutput(PipeDescriptor.stdout);
            conversion.OnProgress += (sender, args) =>
            {
                var percent = (int)(Math.Round(args.Duration.TotalSeconds / args.TotalLength.TotalSeconds, 2) * 100);
                Console.WriteLine($"[{args.Duration} / {args.TotalLength}] {percent}%");
            };
            var stopwatch = Stopwatch.StartNew();
            await conversion.Start();
            stopwatch.Stop();

            Console.WriteLine($"-- Finished {profile.Name} in {stopwatch.Elapsed.TotalSeconds:F2} seconds. --");
        }

        Console.WriteLine("All profiles encoded. Creating master playlist...");
        var masterPlaylistPath = Path.Combine(outputDirectory.FullName, "master.m3u8");
        var masterPlaylistContent = new StringBuilder();
        masterPlaylistContent.AppendLine("#EXTM3U");
        masterPlaylistContent.AppendLine("#EXT-X-VERSION:3");
        var variantPlaylists = new List<string>();
        foreach (var profile in EncodingProfiles)
        {
            variantPlaylists.Add($"#EXT-X-STREAM-INF:BANDWIDTH={profile.Bitrate},RESOLUTION={profile.Width}x{profile.Height}\n{profile.Name}/playlist.m3u8");
        }
        masterPlaylistContent.Append(string.Join("\n", variantPlaylists));

        await File.WriteAllTextAsync(masterPlaylistPath, masterPlaylistContent.ToString());

        Console.WriteLine($"\nEncoding complete!");
        Console.WriteLine($"Output files are located in: {outputDirectory.FullName}");
        Console.WriteLine($"Master playlist: {masterPlaylistPath}");
    }
}
