using System;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Models
{
    public class Transaction
    {
        public int TransactionId { get; set; }
        public int StudentId { get; set; }
        public int UserId { get; set; }
        public string? TransactionCode { get; set; }
        public DateTime TransactionDateBorrowed { get; set; }
        public string? TransactionRemarks { get; set; }
        public DateTime TransactionDateAdded { get; set; } = DateTime.Now;
        public DateTime TransactionLastUpdated { get; set; } = DateTime.Now;

        // Navigation properties
        public Student? Student { get; set; }
        public User? User { get; set; }
    }
}
