using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using VotingSystem.Models;

namespace VotingSystem.Controllers
{
    [Authorize(Roles = "Voter")]
    public class VoteController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Vote  -- list elections currently open to vote in
        public ActionResult Index()
        {
            var now = DateTime.Now; // dates are entered in local time on the admin form
            var openElections = db.Elections
                .Where(e => e.IsActive && e.StartDate <= now && e.EndDate >= now)
                .ToList();

            // Elections this student has already voted in, so the view can show "Voted".
            string voterId = User.Identity.GetUserId();
            ViewBag.VotedIds = new HashSet<int>(
                db.Votes.Where(v => v.VoterId == voterId).Select(v => v.ElectionId));

            return View(openElections);
        }

        private static bool IsOpen(Election e)
        {
            var now = DateTime.Now;
            return e.IsActive && e.StartDate <= now && e.EndDate >= now;
        }

        // Records the attempt so the admin can see it on the results page,
        // then sends the student back with a warning.
        private ActionResult BlockDoubleVote(int electionId)
        {
            db.BlockedVoteAttempts.Add(new BlockedVoteAttempt
            {
                ElectionId = electionId,
                StudentNumber = User.Identity.GetUserName()
            });
            db.SaveChanges();

            TempData["Message"] = "You have already voted in this election. This attempt has been logged.";
            TempData["MessageType"] = "warning";
            return RedirectToAction("Index");
        }

        // GET: Vote/Cast/5  (5 = electionId)
        public ActionResult Cast(int id)
        {
            string voterId = User.Identity.GetUserId();

            bool alreadyVoted = db.Votes.Any(v => v.ElectionId == id && v.VoterId == voterId);
            if (alreadyVoted) return BlockDoubleVote(id);

            var election = db.Elections.Include(e => e.Candidates)
                                        .FirstOrDefault(e => e.Id == id);
            if (election == null) return HttpNotFound();
            if (!IsOpen(election))
            {
                TempData["Message"] = "This election is not open for voting.";
                return RedirectToAction("Index");
            }

            return View(election);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SubmitVote(int electionId, int candidateId)
        {
            string voterId = User.Identity.GetUserId();

            // Re-check server-side; don't trust the client / a resubmitted form.
            bool alreadyVoted = db.Votes.Any(v => v.ElectionId == electionId && v.VoterId == voterId);
            if (alreadyVoted) return BlockDoubleVote(electionId);

            var election = db.Elections.Find(electionId);
            if (election == null || !IsOpen(election))
            {
                TempData["Message"] = "This election is not open for voting.";
                return RedirectToAction("Index");
            }

            // Make sure the posted candidate really belongs to this election.
            bool validCandidate = db.Candidates.Any(c => c.Id == candidateId && c.ElectionId == electionId);
            if (!validCandidate)
            {
                TempData["Message"] = "Invalid candidate selected.";
                return RedirectToAction("Index");
            }

            var vote = new Vote
            {
                ElectionId = electionId,
                CandidateId = candidateId,
                VoterId = voterId
            };
            db.Votes.Add(vote);

            try
            {
                db.SaveChanges();
                TempData["Message"] = "Your vote has been recorded. Thank you for voting!";
                TempData["MessageType"] = "success";
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException)
            {
                // The unique index on (ElectionId, VoterId) caught two requests
                // racing each other (e.g. a double-clicked submit button).
                db.Entry(vote).State = EntityState.Detached;
                return BlockDoubleVote(electionId);
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
