using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EncodeHLS;

class Program
{
    static async Task Main(string[] args)
    {
        // Check for input file and output directory from command line
        if (args.Length == 0 || string.IsNullOrEmpty(args[0]) || args[0].StartsWith("-"))
        {
            Console.WriteLine("Error: Please provide the input video file path as the first argument.");
            Console.WriteLine("Usage: EncodeHLS.exe <path_to_video_file> [output_directory]");
            return;
        }
        var inputFilePath = args[0];
        var inputFile = new FileInfo(inputFilePath);
        if (!inputFile.Exists)
        {
            Console.WriteLine($"Error: Input file not found at '{inputFilePath}'");
            return;
        }

        var encodeOptions = new EncodeOptions()
        {
            InputFile = args[0],
            OutputDir = args.Length > 1 ? args[1] : Path.Combine(inputFile.DirectoryName ?? "", Path.GetFileNameWithoutExtension(inputFile.Name)),
        };

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(AppContext.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddCommandLine(args);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<HlsEncodingService, HlsEncodingService>();
                services.AddSingleton(encodeOptions);
                services.AddOptions<FFmpegOptions>()
                    .Bind(context.Configuration.GetSection("FFmpeg"));
                services.AddOptions<List<EncodingProfile>>()
                    .Bind(context.Configuration.GetSection("EncodingProfiles"));
            })
            .Build();

        await host.Services.GetService<HlsEncodingService>()!.EncodeAsync();
    }
}
