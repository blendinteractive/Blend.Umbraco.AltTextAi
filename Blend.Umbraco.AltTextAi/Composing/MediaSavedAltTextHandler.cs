using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.HostedServices;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Cms.Web.Common;
using Umbraco.Extensions;
using Image = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Blend.Umbraco.AltTextAi.Composing;

public class MediaSavedAltTextHandler: INotificationHandler<MediaSavedNotification>
{
    private readonly IMediaService _mediaService;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IScopeProvider _scopeProvider;
    private readonly IOptions<Configuration.AltTextAi> _options;
    private readonly AppCaches _appCaches;
    
    
    public MediaSavedAltTextHandler(IMediaService mediaService, IBackgroundTaskQueue backgroundTaskQueue,
        IScopeProvider scopeProvider, IOptions<Configuration.AltTextAi> options, AppCaches appCaches)
    {
        _mediaService = mediaService;
        _backgroundTaskQueue = backgroundTaskQueue;
        _scopeProvider = scopeProvider;
        _options = options;
        _appCaches = appCaches;

    }

    public void Handle(MediaSavedNotification notification)
    {
        var settings = _options.Value;
        foreach (var media in notification.SavedEntities)
        {
            if (media.HasProperty(settings.ImageAltTextProperty))
            {
                var altText = media.GetValue<string>(settings.ImageAltTextProperty);

                if (altText.IsNullOrEmpty())
                {
                    _backgroundTaskQueue.QueueBackgroundWorkItem(cancellationToken => RequestAltTextGeneration(media.Key));
                }
            }
        }        
    }

    public async Task RequestAltTextGeneration(Guid mediaKey)
    {
        var settings = _options.Value;
        var media = _mediaService.GetById(mediaKey);
        if (media == null)
        {
            return; 
        }
        
        var fileInfo = JsonConvert.DeserializeObject<ImageCropperValue>(media.GetValue<string>(Constants.Conventions.Media.File));
        if (fileInfo.Src == null)
        {
            Console.WriteLine("No File URL");
            return;
        }
        var stream = ResizeImage(_mediaService.GetMediaFileContentStream(fileInfo.Src));
        
        
        using HttpClient client = new();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("X-API-Key", settings.AltTextAiApiKey);

        byte[] imgBytes = stream.ToArray();
        
        var request = new
        {
            image = new
            {
                raw = Convert.ToBase64String(imgBytes)
            }
        };
        
        /* //URL-based processing. Preferable but less reliable in dev instances
        IPublishedContent node = _helper.Media(media.Key);
        var request = new
        {
            image = new
            {
                url = node.Url()
            }
        };
        */
        
        var json = JsonSerializer.Serialize(request);
        var data = new StringContent(json, Encoding.UTF8, "application/json");

        var url = "https://alttext.ai/api/v1/images";
        
        var response = await client.PostAsync(url, data);
        
        string responseBody = await response.Content.ReadAsStringAsync();

        AltTextResponse result = JsonSerializer.Deserialize<AltTextResponse>(responseBody);
        
        if (response.IsSuccessStatusCode)
        {
            /* // Suppress notifications - causes cache issues in 14
            using var scope = _scopeProvider.CreateScope(autoComplete: true);
            using var _ = scope.Notifications.Suppress();
            */
            
            media.SetValue(settings.ImageAltTextProperty, result.alt_text);
            _mediaService.Save(media);
            
            _appCaches.RuntimeCache.ClearByKey(media.Key.ToString());
        }
    }
    
    protected MemoryStream ResizeImage(Stream sourceImage)
    {
        var targetStream = new MemoryStream();
        Image srcImage = Image.Load(sourceImage);
        srcImage.Mutate(x=>x.Resize(1080, 0));
        srcImage.Save(targetStream, new JpegEncoder());
        return targetStream;
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

