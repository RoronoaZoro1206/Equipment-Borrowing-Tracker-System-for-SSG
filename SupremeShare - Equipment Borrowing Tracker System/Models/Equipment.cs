using System;
using System.ComponentModel.DataAnnotations;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Models
{
    public class Equipment
    {
        public int EquipmentId { get; set; }

        [Required(ErrorMessage = "Equipment name is required")]
        [StringLength(100, ErrorMessage = "Equipment name cannot exceed 100 characters")]
        [Display(Name = "Equipment Name")]
        public string EquipmentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        [Display(Name = "Category")]
        public string EquipmentCategory { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a non-negative number")]
        [Display(Name = "Quantity")]
        public int EquipmentQuantity { get; set; }

        [Display(Name = "Date Added")]
        public DateTime EquipmentDateAdded { get; set; } = DateTime.Now;

        [Display(Name = "Last Updated")]
        public DateTime EquipmentLastUpdated { get; set; } = DateTime.Now;
    }
}
