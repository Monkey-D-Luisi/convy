using Convy.Application.Common.Models;
using Convy.Infrastructure.Services;
using FluentAssertions;

namespace Convy.Infrastructure.Tests.Services;

public class PushNotificationTextProviderTests
{
    private readonly PushNotificationTextProvider _provider = new();

    [Theory]
    [InlineData("es-ES", "Artículos añadidos", "Luis añadió 1 artículo a Compra")]
    [InlineData("en-US", "Items added", "Luis added 1 item to Compra")]
    [InlineData("pl-PL", "Items added", "Luis added 1 item to Compra")]
    public void Render_ItemsAdded_UsesSupportedLocaleOrEnglishFallback(string locale, string title, string body)
    {
        var template = new PushNotificationTemplate(
            NotificationTemplateKey.ItemsAdded,
            new Dictionary<string, string>
            {
                ["actorName"] = "Luis",
                ["listName"] = "Compra",
                ["count"] = "1",
            });

        var rendered = _provider.Render(template, locale);

        rendered.Title.Should().Be(title);
        rendered.Body.Should().Be(body);
    }
}
