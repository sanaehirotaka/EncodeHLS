using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EncodeHLS;

class Program
{
    static async Task Main(string[] args)
    {
        // Check for input file and output directory from command line
        if (args.Length <= 1)
        {
            Console.WriteLine("Error: Please provide the input video file path as the first argument.");
            Console.WriteLine("Usage: EncodeHLS.exe <output_directory> <path_to_video_file>[...<path_to_video_file>]");
            return;
        }

        var encodeOptions = new EncodeOptions()
        {
            InputFiles = [.. args.Skip(1)],
            OutputDir = args[0],
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
