using Crosstales.RTVoice.Provider;
using Dissonance;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SEE.Utils;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SEE.UI.Window.PropertyWindow
{



    //[TestFixture]
    internal class IssueTrackerTests
    {
        [Test]
        public async Task GitLabIssues()
        {
            //var settings = new IssueReceiverInterface.Settings
            //{
            //    preUrl = "https://api.github.com/repos/uni-bremen-agst/SEE/issues",
            //    searchUrl = "?filter=all"
            //};
            //Console.WriteLine("fdfdfsdfsd");
            JiraIssueProvider jiraIssueProvider = new JiraIssueProvider(new Game.City.SEECity());

            // Wenn getIssues async ist:
           JArray result = await jiraIssueProvider.getIssues(null); //await jiraIssueProvider.createIssue( null);


            //Console.WriteLine("fdfdfsdfsd");
            // GitHubIssueProvider gitHubReceiver = new GitHubIssueProvider(new Game.City.SEECity());

            // JArray jarry = await gitHubReceiver.getIssues(null);
            // gitHubReceiver
            // Wenn getIssues async ist:
            //   var task = gitHubReceiver.createIssue();

            // Unity kann Tasks über Coroutines abwarten:
            //new WaitUntil(() => task.IsCompleted);

            //  Assert.True(task.Result);
            //  Assert.IsTrue(task.IsCompletedSuccessfully, "Task nicht erfolgreich beendet.");
            //var array = await gitHubReceiver.getIssues(settings);
            //// Beispielpruefung: 
            ///
            Console.WriteLine($"Coutn: {result.Count()}");
            Assert.AreEqual(result, 5);// gitHubReceiver.issues.Count);
        }
        //  [Test]
        public async Task GitHubIssues()
        {
            //var settings = new IssueReceiverInterface.Settings
            //{
            //    preUrl = "https://api.github.com/repos/uni-bremen-agst/SEE/issues",
            //    searchUrl = "?filter=all"
            //};



            //Console.WriteLine("fdfdfsdfsd");
            //var gitHubReceiver = new GitHubIssueReceiver( new Game.City.SEECity());

            // Wenn getIssues async ist:
            //   var task = gitHubReceiver.createIssue();

            // Unity kann Tasks über Coroutines abwarten:
            //new WaitUntil(() => task.IsCompleted);

            //  Assert.True(task.Result);
            //  Assert.IsTrue(task.IsCompletedSuccessfully, "Task nicht erfolgreich beendet.");
            //var array = await gitHubReceiver.getIssues(settings);
            //// Beispielprüfung: 
            //Assert.AreEqual(200, gitHubReceiver.issues.Count);
        }

        // [Test]
        public void JiraIssues()
        {
            //IssueReceiverInterface.Settings settings = new IssueReceiverInterface.Settings { preUrl = "https://ecosystem.atlassian.net/rest/api/3/search?jql=", searchUrl = "project=CACHE" };
            //JiraIssueReceiver jiraReceiver = new JiraIssueReceiver();
            //// jiraReceiver.getIssues(settings);
            //Assert.AreEqual(jiraReceiver.issues.Count, 200);
            //Assert.Pass("Test erfolgreich ausgeführt.");
        }
        // [Test]
        //public void GitHubIssues()
        //{
        //    IssueReceiverInterface.Settings settings = new IssueReceiverInterface.Settings { preUrl = "https://api.github.com/repos/uni-bremen-agst/SEE/issues", searchUrl = "?filter=all" };
        //    GitHubIssueReceiver gitHUbReceiver = new GitHubIssueReceiver();
        //  //  gitHUbReceiver.getIssues(settings);
        //    Assert.AreEqual(gitHUbReceiver.issues.Count, 200);
        //    Assert.Pass("Test erfolgreich ausgeführt.");


        //}

    }
}