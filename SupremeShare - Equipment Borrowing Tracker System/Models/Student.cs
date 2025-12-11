using System;
using System.ComponentModel.DataAnnotations;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Models
{
    public class Student
    {
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Student ID Number is required")]
        [StringLength(20, ErrorMessage = "Student ID Number cannot exceed 20 characters")]
        [Display(Name = "Student ID Number")]
        public string StudentIdNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s\-'\.]+$", ErrorMessage = "Last name contains invalid characters")]
        [Display(Name = "Last Name")]
        public string StudentLastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s\-'\.]+$", ErrorMessage = "First name contains invalid characters")]
        [Display(Name = "First Name")]
        public string StudentFirstName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Middle name cannot exceed 100 characters")]
        [RegularExpression(@"^[a-zA-Z\s\-'\.]*$", ErrorMessage = "Middle name contains invalid characters")]
        [Display(Name = "Middle Name")]
        public string? StudentMiddleName { get; set; }

        [StringLength(20, ErrorMessage = "Suffix cannot exceed 20 characters")]
        [Display(Name = "Suffix")]
        public string? StudentSuffix { get; set; }

        [Required(ErrorMessage = "College is required")]
        [StringLength(150, ErrorMessage = "College cannot exceed 150 characters")]
        [Display(Name = "College")]
        public string StudentCollege { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Course cannot exceed 100 characters")]
        [Display(Name = "Course")]
        public string? StudentCourse { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        [Display(Name = "Email")]
        public string? StudentEmail { get; set; }

        [Phone(ErrorMessage = "Invalid contact number format")]
        [StringLength(20, ErrorMessage = "Contact number cannot exceed 20 characters")]
        [RegularExpression(@"^[\d\s\-\+\(\)]*$", ErrorMessage = "Contact number contains invalid characters")]
        [Display(Name = "Contact Number")]
        public string? StudentContactNo { get; set; }

        [Display(Name = "Active Status")]
        public bool StudentStatus { get; set; } = true;

        [Display(Name = "Date Added")]
        public DateTime StudentDateAdded { get; set; } = DateTime.Now;

        [Display(Name = "Last Updated")]
        public DateTime StudentLastUpdated { get; set; } = DateTime.Now;

        // Computed property for display name
        public string FullName => !string.IsNullOrEmpty(StudentFirstName)
            ? $"{StudentFirstName} {StudentLastName}".Trim()
            : string.Empty;
    }
}
