using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VotingSystem.Models
{
    public class Election
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Convenience flag; you can also compute this from dates if you prefer.
        public bool IsActive { get; set; }

        public virtual ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
        public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
    }

    public class Candidate
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; }

        public string Party { get; set; } // optional, drop if not needed

        public int ElectionId { get; set; }
        [ForeignKey("ElectionId")]
        public virtual Election Election { get; set; }

        public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
    }

    // One vote per (ElectionId, VoterId), enforced by the unique index below.
    public class Vote
    {
        public int Id { get; set; }

        [Index("IX_Election_Voter", 1, IsUnique = true)]
        public int ElectionId { get; set; }
        [ForeignKey("ElectionId")]
        public virtual Election Election { get; set; }

        public int CandidateId { get; set; }
        [ForeignKey("CandidateId")]
        public virtual Candidate Candidate { get; set; }

        // Maps to AspNetUsers.Id. Length is capped so the column can be indexed.
        [Required, StringLength(128)]
        [Index("IX_Election_Voter", 2, IsUnique = true)]
        public string VoterId { get; set; }

        public DateTime CastAt { get; set; } = DateTime.Now;
    }

    // The official list of students allowed to vote. A student can only
    // create an account if their ID is on this list.
    public class Student
    {
        public int Id { get; set; }

        [Required, StringLength(20)]
        [Index(IsUnique = true)]
        [Display(Name = "Student ID")]
        public string StudentNumber { get; set; }

        [Required, StringLength(150)]
        [Display(Name = "Full name")]
        public string FullName { get; set; }

        [StringLength(100)]
        public string Course { get; set; }

        // Student IDs are stored trimmed and upper-case so " 2024-0001 " and
        // "2024-0001" are treated as the same student.
        public static string Normalize(string studentNumber)
        {
            return (studentNumber ?? "").Trim().ToUpperInvariant();
        }
    }

    // Logged whenever a student who has already voted tries to vote again.
    public class BlockedVoteAttempt
    {
        public int Id { get; set; }
        public int ElectionId { get; set; }

        [Required, StringLength(20)]
        public string StudentNumber { get; set; }

        public DateTime AttemptedAt { get; set; } = DateTime.Now;
    }

    // One row of the Admin/Results table.
    public class CandidateResult
    {
        public string Name { get; set; }
        public string Party { get; set; }
        public int Votes { get; set; }
    }

    // One row of the "blocked double-vote attempts" table on Admin/Results.
    public class BlockedAttemptRow
    {
        public string StudentNumber { get; set; }
        public string FullName { get; set; }
        public DateTime AttemptedAt { get; set; }
    }
}
