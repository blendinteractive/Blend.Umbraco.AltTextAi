using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.HostedServices;
using Umbraco.Cms.Infrastructure.Scoping;
using Image = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Microsoft.Extensions.Logging;

namespace Blend.Umbraco.AltTextAi.Composing;

public class MediaSavedAltTextHandler: INotificationHandler<MediaSavedNotification>
{
    private readonly IMediaService _mediaService;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IScopeProvider _scopeProvider;
    private readonly IOptions<Configuration.AltTextAi> _options;
    private readonly ILogger<MediaSavedAltTextHandler> _logger;
    private readonly AppCaches _appCaches;
    
    
    public MediaSavedAltTextHandler(IMediaService mediaService, 
                                        IBackgroundTaskQueue backgroundTaskQueue,
                                        IScopeProvider scopeProvider, 
                                        IOptions<Configuration.AltTextAi> options, 
                                        ILogger<MediaSavedAltTextHandler> logger, 
                                        AppCaches appCaches)
    {
        _mediaService = mediaService;
        _backgroundTaskQueue = backgroundTaskQueue;
        _scopeProvider = scopeProvider;
        _options = options;
        _logger = logger;
        _appCaches = appCaches;

    }

    public void Handle(MediaSavedNotification notification)
    {
        var settings = _options.Value;

        foreach (var media in notification.SavedEntities)
        {
            if (!media.HasProperty(settings.ImageAltTextProperty))
            {
                _logger.LogInformation($"Alt text property not found for media item {media.Name} ID {media.Id}");
                continue;
            }

            var altText = media.GetValue<string>(settings.ImageAltTextProperty) ?? string.Empty;

            if (altText.Length <= settings.AltTextLengthToSkip)
            {
                _backgroundTaskQueue.QueueBackgroundWorkItem(cancellationToken => RequestAltTextGeneration(media.Key));
            }
            else
            {
                _logger.LogInformation($"Alt text already exists for media item {media.Name} ID {media.Id}. Skipping alt text generation");
            }
        }
    }

    public async Task RequestAltTextGeneration(Guid mediaKey)
    {
        try
        {
            var settings = _options.Value;
            var media = _mediaService.GetById(mediaKey);

            if (media is null)
            {
                _logger.LogError($"Media item with key {mediaKey} not found");
                return;
            }

            var file = media.GetValue<string>(Constants.Conventions.Media.File);
            if (string.IsNullOrEmpty(file))
            {
                _logger.LogError($"No file found for media item {media.Name} ID {media.Id}");
                return;
            }

            var fileInfo = JsonConvert.DeserializeObject<ImageCropperValue>(file);
            if (string.IsNullOrEmpty(fileInfo?.Src))
            {
                _logger.LogError($"No File URL found for file {media.Name} ID {media.Id}");
                return;
            }

            using var stream = _mediaService.GetMediaFileContentStream(fileInfo.Src);
            var imgBytes = ResizeImage(stream);

            var requestPayload = new
            {
                image = new { raw = Convert.ToBase64String(imgBytes) },
                keywords = settings.AltTextKeyWords
            };

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("X-API-Key", settings.AltTextAiApiKey);

            var response = await client.PostAsync(
                "https://alttext.ai/api/v1/images",
                new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to generate alt text for media item {media.Name} ID {media.Id}. Status Code: {response.StatusCode}");
                return;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(responseBody))
            {
                _logger.LogError($"Empty response from AltText.ai for media item {media.Name} ID {media.Id}");
                return;
            }

            var result = JsonSerializer.Deserialize<AltTextResponse>(responseBody);
            if (result is null)
            {
                _logger.LogError($"Invalid response from AltText.ai for media item {media.Name} ID {media.Id}");
                return;
            }

            // unfortunately this raises a second save event, using .Suppress causes issues in v14+
            media.SetValue(settings.ImageAltTextProperty, result.alt_text);
            _mediaService.Save(media);

            // clear the cache for the media item so it is reloaded with the next request
            _appCaches.RuntimeCache.ClearByKey(media.Key.ToString());

            _logger.LogInformation($"Alt text successfully generated for media item {media.Name} ID {media.Id}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception in {nameof(RequestAltTextGeneration)}: {ex.Message}");
            throw;
        }
    }
    
    protected static byte[] ResizeImage(Stream sourceImage)
    {
        var targetStream = new MemoryStream();
        Image srcImage = Image.Load(sourceImage);
        srcImage.Mutate(x=>x.Resize(1080, 0));
        srcImage.Save(targetStream, new JpegEncoder());
        return targetStream.ToArray();
    }       

}

public class AltTextResponse
{
    public string asset_id { get; set; }
    public string url { get; set; }
    public string alt_text { get; set; }
    public int created_at { get; set; }
    public Errors errors { get; set; }
    public object error_code { get; set; }
}


public class Errors
{

}

