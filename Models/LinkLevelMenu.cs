using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewPinpadApi.Models
{
    public class LinkLevelMenu
    {
        [Key]
        public int Id { get; set; }

        public int LevelId { get; set; }
        public int MenuId { get; set; }

        public string GrantedBy { get; set; }
        public DateTime CreatedDate { get; set; }

        // Navigation properties
        public SysLevel SysLevel { get; set; }
        public SysMenu SysMenu { get; set; }
    }
}
