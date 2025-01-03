using LiveStreamingServerNet.StreamProcessor.Contracts;
using LiveStreamingServerNet.StreamProcessor.FFmpeg.Contracts;
using LiveStreamingServerNet.StreamProcessor.Hls.Contracts;
using LiveStreamingServerNet.Utilities.Contracts;

namespace Traffic_Control_System_Video.Handlers
{
    public class HlsOutputPathResolver : IHlsOutputPathResolver
    {
        private readonly string _outputDir;

        public HlsOutputPathResolver(string outputDir)
        {
            _outputDir = outputDir;
        }

        public ValueTask<string> ResolveOutputPath(IServiceProvider services, Guid contextIdentifier, string streamPath, IReadOnlyDictionary<string, string> streamArguments)
        {
            string[] streamPathSplit = streamPath.Split("/").ToArray();
            return ValueTask.FromResult(Path.Combine(_outputDir, streamPathSplit[1], streamPathSplit[2], "output.m3u8"));
        }
    }

    public class StreamProcessorEventListener : IStreamProcessorEventHandler
    {
        private readonly string _outputDir;
        private readonly ILogger _logger;

        public StreamProcessorEventListener(string outputDir, ILogger<StreamProcessorEventListener> logger)
        {
            _outputDir = outputDir;
            _logger = logger;
        }

        public Task OnStreamProcessorStartedAsync(IEventContext context, string processor, Guid identifier, uint clientId, string inputPath, string outputPath, string streamPath, IReadOnlyDictionary<string, string> streamArguments)
        {
            outputPath = Path.GetRelativePath(_outputDir, outputPath);
            _logger.LogInformation($"[{identifier}] Streaming processor {processor} started: {inputPath} -> {outputPath}");
            return Task.CompletedTask;
        }

        public Task OnStreamProcessorStoppedAsync(IEventContext context, string processor, Guid identifier, uint clientId, string inputPath, string outputPath, string streamPath, IReadOnlyDictionary<string, string> streamArguments)
        {
            outputPath = Path.GetRelativePath(_outputDir, outputPath);
            _logger.LogInformation($"[{identifier}] Streaming processor {processor} stopped: {inputPath} -> {outputPath}");
            return Task.CompletedTask;
        }
    }
}
