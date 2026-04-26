using Convy.Application.Common.Interfaces;
using Convy.Application.Common.Models;
using Convy.Domain.Entities;

namespace Convy.Infrastructure.Services;

public class PushNotificationTextProvider : IPushNotificationTextProvider
{
    public LocalizedPushNotification Render(PushNotificationTemplate template, string? locale)
    {
        var language = DeviceToken.NormalizeLocale(locale);
        return language == "es" ? RenderSpanish(template) : RenderEnglish(template);
    }

    private static LocalizedPushNotification RenderEnglish(PushNotificationTemplate template)
    {
        var p = template.Parameters;
        var actor = Get(p, "actorName", "Someone");
        var list = Get(p, "listName", "the list");
        var count = GetCount(p);

        return template.Key switch
        {
            NotificationTemplateKey.ItemsAdded => new("Items added", $"{actor} added {FormatCount(count, "item", "items")} to {list}"),
            NotificationTemplateKey.TasksAdded => new("Tasks added", $"{actor} added {FormatCount(count, "task", "tasks")} to {list}"),
            NotificationTemplateKey.ItemsCompleted => new("Items completed", $"{actor} completed {FormatCount(count, "item", "items")} in {list}"),
            NotificationTemplateKey.TasksCompleted => new("Tasks completed", $"{actor} completed {FormatCount(count, "task", "tasks")} in {list}"),
            NotificationTemplateKey.ItemUpdated => new("Item updated", $"{Get(p, "title", "An item")} was updated"),
            NotificationTemplateKey.TaskUpdated => new("Task updated", $"{Get(p, "title", "A task")} was updated"),
            NotificationTemplateKey.ItemDeleted => new("Item removed", "An item was removed from the list"),
            NotificationTemplateKey.TaskDeleted => new("Task removed", "A task was removed from the list"),
            NotificationTemplateKey.ListCreated => new("New list created", $"{list} was created"),
            NotificationTemplateKey.ListRenamed => new("List renamed", $"A list was renamed to {list}"),
            NotificationTemplateKey.ListArchived => new("List archived", $"{list} was archived"),
            NotificationTemplateKey.MemberJoined => new("New member", $"{Get(p, "displayName", "Someone")} joined the household"),
            NotificationTemplateKey.MemberLeft => new("Member left", $"{Get(p, "displayName", "Someone")} left the household"),
            _ => new("Convy", "There is a new update")
        };
    }

    private static LocalizedPushNotification RenderSpanish(PushNotificationTemplate template)
    {
        var p = template.Parameters;
        var actor = Get(p, "actorName", "Alguien");
        var list = Get(p, "listName", "la lista");
        var count = GetCount(p);

        return template.Key switch
        {
            NotificationTemplateKey.ItemsAdded => new("Artículos añadidos", $"{actor} añadió {FormatCount(count, "artículo", "artículos")} a {list}"),
            NotificationTemplateKey.TasksAdded => new("Tareas añadidas", $"{actor} añadió {FormatCount(count, "tarea", "tareas")} a {list}"),
            NotificationTemplateKey.ItemsCompleted => new("Artículos completados", $"{actor} completó {FormatCount(count, "artículo", "artículos")} en {list}"),
            NotificationTemplateKey.TasksCompleted => new("Tareas completadas", $"{actor} completó {FormatCount(count, "tarea", "tareas")} en {list}"),
            NotificationTemplateKey.ItemUpdated => new("Artículo actualizado", $"{Get(p, "title", "Un artículo")} se actualizó"),
            NotificationTemplateKey.TaskUpdated => new("Tarea actualizada", $"{Get(p, "title", "Una tarea")} se actualizó"),
            NotificationTemplateKey.ItemDeleted => new("Artículo eliminado", "Se eliminó un artículo de la lista"),
            NotificationTemplateKey.TaskDeleted => new("Tarea eliminada", "Se eliminó una tarea de la lista"),
            NotificationTemplateKey.ListCreated => new("Nueva lista creada", $"Se creó {list}"),
            NotificationTemplateKey.ListRenamed => new("Lista renombrada", $"Una lista se renombró a {list}"),
            NotificationTemplateKey.ListArchived => new("Lista archivada", $"Se archivó {list}"),
            NotificationTemplateKey.MemberJoined => new("Nuevo miembro", $"{Get(p, "displayName", "Alguien")} se unió al hogar"),
            NotificationTemplateKey.MemberLeft => new("Miembro salió", $"{Get(p, "displayName", "Alguien")} salió del hogar"),
            _ => new("Convy", "Hay una nueva actualización")
        };
    }

    private static string Get(IReadOnlyDictionary<string, string> parameters, string key, string fallback) =>
        parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

    private static int GetCount(IReadOnlyDictionary<string, string> parameters) =>
        parameters.TryGetValue("count", out var raw) && int.TryParse(raw, out var count) && count > 0 ? count : 1;

    private static string FormatCount(int count, string singular, string plural) =>
        count == 1 ? $"1 {singular}" : $"{count} {plural}";
}
