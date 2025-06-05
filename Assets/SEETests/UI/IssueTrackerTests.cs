using LibGit2Sharp;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using static IssueReceiverInterface;



    internal class IssueTrackerTests


    {
        [Test]
        void JiraIssues()
        {
            IssueReceiverInterface.Settings settings = new IssueReceiverInterface.Settings { preUrl = "https://ecosystem.atlassian.net/rest/api/3/search?jql=", searchUrl = "project=CACHE" };
            JiraIssueReceiver jiraReceiver = new JiraIssueReceiver();
            jiraReceiver.getIssues(settings);
            Assert.AreEqual(jiraReceiver.issues.Count, 200);
        }
        [Test]
        void GitHubIssues()
        {
            IssueReceiverInterface.Settings settings = new IssueReceiverInterface.Settings { preUrl = "https://api.github.com/repos/uni-bremen-agst/SEE/issues", searchUrl = "?filter=all" };
            GitHubIssueReceiver gitHUbReceiver = new GitHubIssueReceiver();
            gitHUbReceiver.getIssues(settings);
        Assert.AreEqual(gitHUbReceiver.issues.Count, 200);


    }

}
