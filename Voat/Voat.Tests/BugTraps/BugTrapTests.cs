﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voat.Data.Models;
using Voat.Data;
using Voat.Tests.Repository;

namespace Voat.Tests.BugTraps
{
    [TestClass]
    public class BugTrapTests
    {
        [TestMethod]
        [TestCategory("Bug")]
        public async Task Trap_Spam_Vote_Bug()
        {

            /*
                        Sql to verify: no matter how many runs this value should never go down by more than one

                        SELECT
                            UpCount = s.UpCount,
                            RealUpCount = (SELECT ISNULL(SUM(t.VoteStatus), 0) FROM SubmissionVoteTracker t WHERE t.SubmissionID = s.ID AND t.VoteStatus = 1),
                            DownCount = s.DownCount, 
                            RealDownCount = (SELECT ISNULL(SUM(t.VoteStatus), 0) FROM SubmissionVoteTracker t WHERE t.SubmissionID = s.ID AND t.VoteStatus = -1) 
                        FROM Submission s
                        WHERE s.ID = 301210

                        SELECT * FROM SubmissionVoteTracker WHERE SubmissionID = 301210

                        -- EXEC usp_ResetSubmissionVoteCount 301210 --This resets the vote count

            */
            int submissionID = 1;
            Submission beforesubmission;

            using (var repo = new Voat.Data.Repository())
            {
                beforesubmission = repo.GetSubmission(submissionID);
            }

            var principle = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity("User500CCP", "Bearer"), null);
            System.Threading.Thread.CurrentPrincipal = principle;
            Action vote = new Action(() => Voat.Utilities.Voting.UpvoteSubmission(submissionID, "User500CCP", "127.0.0.1"));

            int count = 101;
            var tasks = new List<Task>();
            for (int i = 0; i < count; i++)
            {
                tasks.Add(Task.Run(vote));
            }

            try
            {
                Task.WaitAll(tasks.ToArray());
            }
            catch { /*ignore*/ }

            Submission aftersubmission;
            using (var repo = new Voat.Data.Repository())
            {
                aftersubmission = repo.GetSubmission(submissionID);
            }

            //Assert.Inconclusive(String.Format("Before {0} threads: UpCount:{1}, Afterwards:{2}", count, beforesubmission.UpCount, aftersubmission.UpCount));

            long upCountDiff = beforesubmission.UpCount - aftersubmission.UpCount;
            long downCountDiff = beforesubmission.DownCount - aftersubmission.DownCount;

            Assert.IsTrue(Math.Abs(upCountDiff + downCountDiff) <= 1, String.Format("Difference detected: UpCount Diff: {0}, Down Count Diff: {1}", upCountDiff, downCountDiff));
            Assert.IsTrue(Math.Abs(upCountDiff) <= 1, String.Format("Before {0} threads: UpCount: {1}, Afterwards: {2}", count, beforesubmission.UpCount, aftersubmission.UpCount));
            Assert.IsTrue(Math.Abs(downCountDiff) <= 1, String.Format("Before {0} threads: DownCount: {1}, Afterwards: {2}", count, beforesubmission.DownCount, aftersubmission.DownCount));



        }

    }
}
