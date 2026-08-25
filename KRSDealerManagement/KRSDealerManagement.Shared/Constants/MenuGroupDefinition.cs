namespace KRSDealerManagement.Shared.Constants
{
    public class MenuGroupDefinition
    {
        public required string ParentKey { get; init; }
        public required string ParentName { get; init; }
        public string? Icon { get; init; }
        public required IReadOnlyList<MenuItemDefinition> Children { get; init; }
    }

    public class MenuItemDefinition
    {
        public required string Key { get; init; }
        public required string Name { get; init; }
        public bool DefaultAccessible { get; init; }
        public required string Controller { get; init; }
        public required string Action { get; init; }
        public string? Icon { get; init; }
        /// <summary>When set, any of these actions mark the menu item active.</summary>
        public IReadOnlyList<string>? Actions { get; init; }
        /// <summary>Optional route values (e.g. status filter for booking lists).</summary>
        public IReadOnlyDictionary<string, object>? RouteValues { get; init; }
    }
}
