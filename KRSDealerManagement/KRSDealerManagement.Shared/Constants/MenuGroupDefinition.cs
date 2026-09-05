namespace KRSDealerManagement.Shared.Constants
{
    public class MenuGroupDefinition
    {
        public required string ParentKey { get; init; }
        public required string ParentName { get; init; }
        public string? Icon { get; init; }
        public IReadOnlyList<MenuItemDefinition> Children { get; init; } = Array.Empty<MenuItemDefinition>();
        public IReadOnlyList<MenuSectionDefinition>? Sections { get; init; }

        public bool HasSections => Sections is { Count: > 0 };

        public IEnumerable<MenuItemDefinition> EnumerateChildren()
        {
            if (HasSections)
            {
                foreach (var section in Sections!)
                {
                    foreach (var child in section.Children)
                        yield return child;
                }
                yield break;
            }

            foreach (var child in Children)
                yield return child;
        }
    }

    public class MenuSectionDefinition
    {
        public required string SectionKey { get; init; }
        public required string SectionName { get; init; }
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
