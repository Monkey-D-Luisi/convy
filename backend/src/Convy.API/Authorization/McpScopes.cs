namespace Convy.API.Authorization;

public static class McpScopes
{
    public const string HouseholdsRead = "convy.households.read";
    public const string ListsRead = "convy.lists.read";
    public const string ItemsRead = "convy.items.read";
    public const string TasksRead = "convy.tasks.read";
    public const string ActivityRead = "convy.activity.read";
    public const string ItemsWrite = "convy.items.write";
    public const string TasksWrite = "convy.tasks.write";

    public static readonly string[] ReadOnly =
    [
        HouseholdsRead,
        ListsRead,
        ItemsRead,
        TasksRead,
        ActivityRead,
    ];

    public static readonly string[] Write =
    [
        ItemsWrite,
        TasksWrite,
    ];

    public static readonly string[] Supported =
    [
        .. ReadOnly,
        .. Write,
    ];
}
