#nullable disable
using System.ComponentModel.DataAnnotations;

namespace AppCleaner
{
    public enum PatternType
    {
        [Display(Name = "*.cs")]
        CS,
        [Display(Name = "*.txt")]
        TXT,
        [Display(Name = "*.razor")]
        RAZOR,
        [Display(Name = "*.bak")]
        BAK,
        [Display(Name = "*.*")]
        ALL
    }
}
