using FFMpegCore;
using FFMpegCore.Enums;

namespace DunaConverter.Handlers;

public class ffmpegHandler
{
    public static async Task Convert(string inputFilename, string outputFilename)
    {
        await FFMpegArguments
            .FromFileInput(inputFilename)
            .OutputToFile(outputFilename)
            .ProcessAsynchronously();
    }

    public static async Task CompressAudio(string inputFilename, string outputFilename)
    {
        await FFMpegArguments
            .FromFileInput(inputFilename)
            .OutputToFile(outputFilename, true,
                options => options.WithAudioCodec(AudioCodec.LibMp3Lame).WithAudioBitrate(AudioQuality.Normal).OverwriteExisting())
            .ProcessAsynchronously();
    }
    
    public static async Task CompressVideo(string inputFilename, string outputFilename)
        {
            await FFMpegArguments
                .FromFileInput(inputFilename)
                .OutputToFile(outputFilename, true,
                    options => options.WithVideoCodec(VideoCodec.LibX265).OverwriteExisting())
                .ProcessAsynchronously();
        }
}