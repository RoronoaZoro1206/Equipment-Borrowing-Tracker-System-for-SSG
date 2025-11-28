using System;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Models
{
    public class Student
    {
        public int StudentId { get; set; }
        public string StudentIdNumber { get; set; } = string.Empty;
        public string StudentLastName { get; set; } = string.Empty;
        public string StudentFirstName { get; set; } = string.Empty;
        public string? StudentMiddleName { get; set; }
        public string? StudentSuffix { get; set; }
        public string StudentCollege { get; set; } = string.Empty;
        public string? StudentCourse { get; set; }
        public string? StudentEmail { get; set; }
        public string? StudentContactNo { get; set; }
        public bool StudentStatus { get; set; } = true;
        public DateTime StudentDateAdded { get; set; } = DateTime.Now;
        public DateTime StudentLastUpdated { get; set; } = DateTime.Now;

        // Computed property for display name
        public string FullName => !string.IsNullOrEmpty(StudentFirstName)
            ? $"{StudentFirstName} {StudentLastName}".Trim()
            : string.Empty;
    }
}
