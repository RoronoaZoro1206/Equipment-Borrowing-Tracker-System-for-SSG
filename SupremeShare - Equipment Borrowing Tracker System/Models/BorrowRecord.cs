using System;
using System.ComponentModel.DataAnnotations;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Models
{
    public class BorrowRecord
    {
        public int BorrowId { get; set; }

        [Required(ErrorMessage = "Transaction is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Transaction ID must be a positive number")]
        [Display(Name = "Transaction")]
        public int TransactionId { get; set; }

        [Required(ErrorMessage = "Equipment unit is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Equipment unit ID must be a positive number")]
        [Display(Name = "Equipment Unit")]
        public int EunitId { get; set; }

        [Required(ErrorMessage = "Due date is required")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Due Date")]
        public DateTime BorrowDueDate { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Date Returned")]
        public DateTime? BorrowDateReturned { get; set; }

        [Display(Name = "Return Condition")]
        public bool? BorrowReturnCondition { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters")]
        [RegularExpression(@"^(borrowed|returned|overdue|lost)$",
            ErrorMessage = "Status must be 'borrowed', 'returned', 'overdue', or 'lost'")]
        [Display(Name = "Status")]
        public string BorrowStatus { get; set; } = string.Empty;

        [Display(Name = "Date Added")]
        public DateTime BorrowDateAdded { get; set; } = DateTime.Now;

        [Display(Name = "Last Updated")]
        public DateTime BorrowLastUpdated { get; set; } = DateTime.Now;

        // Navigation properties
        public Transaction? Transaction { get; set; }
        public EquipmentUnit? EquipmentUnit { get; set; }
    }
}
