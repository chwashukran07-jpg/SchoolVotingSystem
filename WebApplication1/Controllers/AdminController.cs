using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using VotingSystem.Models;

namespace VotingSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // admin stuff
        public ActionResult Index()
        {
            var elections = db.Elections.OrderByDescending(e => e.StartDate).ToList();
            return View(elections);
        }

        // GET: Admin/CreateElection
        public ActionResult CreateElection()
        {
            var now = DateTime.Now;
            return View(new Election
            {
                StartDate = now,
                EndDate = now.AddDays(1),
                IsActive = true
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult CreateElection(Election election)
        {
            if (election.EndDate <= election.StartDate)
                ModelState.AddModelError("EndDate", "End date must be after the start date.");

            if (!ModelState.IsValid) return View(election);

            db.Elections.Add(election);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Admin/Candidates/5  (5 = electionId)
        public ActionResult Candidates(int id)
        {
            var election = db.Elections.Include(e => e.Candidates)
                                        .FirstOrDefault(e => e.Id == id);
            if (election == null) return HttpNotFound();
            return View(election);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult AddCandidate(int electionId, string name, string party)
        {
            db.Candidates.Add(new Candidate
            {
                ElectionId = electionId,
                Name = name,
                Party = party
            });
            db.SaveChanges();
            return RedirectToAction("Candidates", new { id = electionId });
        }

        // GET: Admin/Results/5
        public ActionResult Results(int id)
        {
            var election = db.Elections
                .Include(e => e.Candidates.Select(c => c.Votes))
                .FirstOrDefault(e => e.Id == id);
            if (election == null) return HttpNotFound();

            // Candidate -> vote count. A named type (not anonymous) so the view can read it.
            var tally = election.Candidates
                .Select(c => new CandidateResult { Name = c.Name, Party = c.Party, Votes = c.Votes.Count })
                .OrderByDescending(c => c.Votes)
                .ToList();

            ViewBag.Election = election;
            ViewBag.EligibleCount = db.Students.Count();

            // Who tried to vote twice, with their names from the voter list.
            ViewBag.BlockedAttempts = (
                from a in db.BlockedVoteAttempts
                where a.ElectionId == id
                join s in db.Students on a.StudentNumber equals s.StudentNumber into matches
                from s in matches.DefaultIfEmpty()
                orderby a.AttemptedAt descending
                select new BlockedAttemptRow
                {
                    StudentNumber = a.StudentNumber,
                    FullName = s == null ? "(removed from list)" : s.FullName,
                    AttemptedAt = a.AttemptedAt
                }).ToList();

            return View(tally);
        }

        // GET: Admin/Students  -- the voter list
        public ActionResult Students()
        {
            var students = db.Students.OrderBy(s => s.StudentNumber).ToList();

            // Student IDs that already have an account (usernames are Student IDs).
            ViewBag.Registered = new HashSet<string>(db.Users.Select(u => u.UserName));
            return View(students);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult AddStudent(string studentNumber, string fullName, string course)
        {
            var number = Student.Normalize(studentNumber);
            fullName = (fullName ?? "").Trim();

            if (number == "" || fullName == "")
            {
                SetMessage("Student ID and full name are required.", "danger");
            }
            else if (number.Length > 20 || number.Contains(" "))
            {
                SetMessage("Student ID must be at most 20 characters with no spaces.", "danger");
            }
            else if (db.Students.Any(s => s.StudentNumber == number))
            {
                SetMessage(number + " is already on the voter list.", "warning");
            }
            else
            {
                db.Students.Add(new Student { StudentNumber = number, FullName = fullName, Course = Trim(course, 100) });
                db.SaveChanges();
                SetMessage("Added " + fullName + " (" + number + ") to the voter list.", "success");
            }

            return RedirectToAction("Students");
        }

        // Paste a class list, one student per line: "Student ID, Full Name, Course".
        // Commas or tabs both work, so rows copied straight from Excel are fine.
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ImportStudents(string list)
        {
            var existing = new HashSet<string>(db.Students.Select(s => s.StudentNumber));
            int added = 0, skipped = 0;

            foreach (var line in (list ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split(',', '\t');
                var number = Student.Normalize(parts[0]);
                var name = parts.Length > 1 ? parts[1].Trim() : "";

                // Skips blank IDs, header rows like "Student ID, Name" (IDs have
                // no spaces), over-long values and IDs already on the list.
                bool valid = number != "" && name != "" && !number.Contains(" ")
                             && number.Length <= 20 && name.Length <= 150;
                if (!valid || !existing.Add(number))
                {
                    skipped++;
                    continue;
                }

                db.Students.Add(new Student
                {
                    StudentNumber = number,
                    FullName = name,
                    Course = parts.Length > 2 ? Trim(parts[2], 100) : null
                });
                added++;
            }

            db.SaveChanges();
            SetMessage("Imported " + added + " student(s). Skipped " + skipped + " line(s) (duplicates, headers or missing ID/name).",
                       added > 0 ? "success" : "warning");
            return RedirectToAction("Students");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult DeleteStudent(int id)
        {
            var student = db.Students.Find(id);
            if (student != null)
            {
                // Once a student has an account they may have voted, so keep them
                // on the list to keep the records consistent.
                if (db.Users.Any(u => u.UserName == student.StudentNumber))
                {
                    SetMessage(student.StudentNumber + " already has a voter account and can't be removed.", "danger");
                }
                else
                {
                    db.Students.Remove(student);
                    db.SaveChanges();
                    SetMessage("Removed " + student.StudentNumber + " from the voter list.", "success");
                }
            }
            return RedirectToAction("Students");
        }

        private void SetMessage(string text, string type)
        {
            TempData["Message"] = text;
            TempData["MessageType"] = type;
        }

        private static string Trim(string value, int maxLength)
        {
            value = (value ?? "").Trim();
            if (value == "") return null;
            return value.Length > maxLength ? value.Substring(0, maxLength) : value;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
