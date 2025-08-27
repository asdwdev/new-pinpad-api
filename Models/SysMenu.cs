namespace NewPinpadApi.Models
{
    public class SysMenu
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string? Icon { get; set; }

        public string? Urls { get; set; }

        // Parent-child relationship
        public int? ParentId { get; set; }
        public SysMenu? Parent { get; set; }
        public ICollection<SysMenu>? Children { get; set; }

        // Relasi ke LinkLevelMenu
        public ICollection<LinkLevelMenu> LinkLevelMenus { get; set; }
    }
}
