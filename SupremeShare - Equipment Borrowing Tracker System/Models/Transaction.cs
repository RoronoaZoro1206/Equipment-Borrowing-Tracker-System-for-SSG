using System;
using System.ComponentModel.DataAnnotations;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Models
{
    public class Transaction
    {
        public int TransactionId { get; set; }

        [Required(ErrorMessage = "Student is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Student ID must be a positive number")]
        [Display(Name = "Student")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "User is required")]
        [Range(1, int.MaxValue, ErrorMessage = "User ID must be a positive number")]
        [Display(Name = "Recorded By")]
        public int UserId { get; set; }

        [StringLength(100, ErrorMessage = "Transaction code cannot exceed 100 characters")]
        [Display(Name = "Transaction Code")]
        public string? TransactionCode { get; set; }

        [Required(ErrorMessage = "Borrow date is required")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Date Borrowed")]
        public DateTime TransactionDateBorrowed { get; set; }

        [StringLength(1000, ErrorMessage = "Remarks cannot exceed 1000 characters")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Remarks")]
        public string? TransactionRemarks { get; set; }

        [Display(Name = "Date Added")]
        public DateTime TransactionDateAdded { get; set; } = DateTime.Now;

        [Display(Name = "Last Updated")]
        public DateTime TransactionLastUpdated { get; set; } = DateTime.Now;

        // Navigation properties
        public Student? Student { get; set; }
        public User? User { get; set; }
    }
}
