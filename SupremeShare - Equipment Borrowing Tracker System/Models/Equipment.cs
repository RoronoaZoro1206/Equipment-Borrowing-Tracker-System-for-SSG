using System;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Models
{
    public class Equipment
    {
        public int EquipmentId { get; set; }
        public string EquipmentName { get; set; } = string.Empty;
        public string EquipmentCategory { get; set; } = string.Empty;
        public int EquipmentQuantity { get; set; }
        public DateTime EquipmentDateAdded { get; set; } = DateTime.Now;
        public DateTime EquipmentLastUpdated { get; set; } = DateTime.Now;
    }
}
