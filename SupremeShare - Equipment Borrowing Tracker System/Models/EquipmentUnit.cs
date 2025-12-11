using System;
using System.ComponentModel.DataAnnotations;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Models
{
    public class EquipmentUnit
    {
        public int EunitId { get; set; }

        [Required(ErrorMessage = "Equipment ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Equipment ID must be a positive number")]
        [Display(Name = "Equipment")]
        public int EquipmentId { get; set; }

        [Required(ErrorMessage = "Serial number is required")]
        [StringLength(20, ErrorMessage = "Serial number cannot exceed 20 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\s\-_\.]+$", ErrorMessage = "Serial number contains invalid characters")]
        [Display(Name = "Serial Number")]
        public string EunitSerialNo { get; set; } = string.Empty;

        [Display(Name = "Condition")]
        public bool EunitCondition { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
        [RegularExpression(@"^(available|borrowed|for review|lost|maintenance)$", 
            ErrorMessage = "Status must be 'available', 'borrowed', 'for review', 'lost', or 'maintenance'")]
        [Display(Name = "Status")]
        public string EunitStatus { get; set; } = string.Empty;

        [Display(Name = "Date Added")]
        public DateTime EunitDateAdded { get; set; } = DateTime.Now;

        [Display(Name = "Last Updated")]
        public DateTime EunitLastUpdated { get; set; } = DateTime.Now;

        // Navigation property
        public Equipment? Equipment { get; set; }
    }
}
