using System;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Models
{
    public class EquipmentUnit
    {
        public int EunitId { get; set; }
        public int EquipmentId { get; set; }
        public string EunitSerialNo { get; set; } = string.Empty;
        public bool EunitCondition { get; set; }
        public string EunitStatus { get; set; } = string.Empty;
        public DateTime EunitDateAdded { get; set; } = DateTime.Now;
        public DateTime EunitLastUpdated { get; set; } = DateTime.Now;

        // Navigation property
        public Equipment? Equipment { get; set; }
    }
}
