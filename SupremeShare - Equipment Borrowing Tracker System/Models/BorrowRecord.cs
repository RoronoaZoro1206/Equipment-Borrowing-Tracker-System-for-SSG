using System;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Models
{
    public class BorrowRecord
    {
        public int BorrowId { get; set; }
        public int TransactionId { get; set; }
        public int EunitId { get; set; }
        public DateTime BorrowDueDate { get; set; }
        public DateTime? BorrowDateReturned { get; set; }
        public bool? BorrowReturnCondition { get; set; }
        public string BorrowStatus { get; set; } = string.Empty;
        public DateTime BorrowDateAdded { get; set; } = DateTime.Now;
        public DateTime BorrowLastUpdated { get; set; } = DateTime.Now;

        // Navigation properties
        public Transaction? Transaction { get; set; }
        public EquipmentUnit? EquipmentUnit { get; set; }
    }
}
