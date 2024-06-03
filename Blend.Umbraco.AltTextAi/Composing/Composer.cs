using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

namespace Blend.Umbraco.AltTextAi.Composing;

public class Composer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<Configuration.AltTextAi>(options =>
        {
            builder.Config.GetSection(nameof(Configuration.AltTextAi)).Bind(options);
        });
        builder.AddNotificationHandler<MediaSavedNotification, MediaSavedAltTextHandler>();
    }
}