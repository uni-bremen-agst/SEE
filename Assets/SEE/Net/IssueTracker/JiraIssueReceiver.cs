using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SEE.Game.City;
using SEE.GraphProviders;
using SEE.Utils.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using static IssueReceiverInterface;


public class JiraIssueReceiver : BasicIssueProvider
{
    [SerializeField]
    public override IssueReceiverInterface.IssueProvider Type => IssueReceiverInterface.IssueProvider.JiraIssueReceiver;
    public JiraIssueReceiver(SEECity city) : base(city)
    {
        preUrl = $"https://yourgitlabdomain.com/api/v4/projects/{projekt}/issues";
    }
    public List<RootIssue> issues;
    public JArray issuesJ;
    static private string label = "Data";

    public Dictionary<string, string> getCreateIssueAttributes()
    {
        return new Dictionary<string, string> { { "test", "test" } };
    }

    //Github / giblab Issue Classes
    //#region "IssueClasses Jira" 


    //public class Rootobject
    //{
    //    public string expand { get; set; }
    //    public int startAt { get; set; }
    //    public int maxResults { get; set; }
    //    public int total { get; set; }
    //    public Issue[] issues { get; set; }
    //}

    //public class Issue
    //{
    //    public string expand { get; set; }
    //    public string id { get; set; }
    //    public string self { get; set; }
    //    public string key { get; set; }
    //    public Fields fields { get; set; }
    //}

    //public class Fields
    //{
    //    public object customfield_19321 { get; set; }
    //    public object customfield_19442 { get; set; }
    //    public object customfield_19322 { get; set; }
    //    public object customfield_19323 { get; set; }
    //    public object customfield_19324 { get; set; }
    //    public object customfield_19440 { get; set; }
    //    public object customfield_19561 { get; set; }
    //    public object customfield_19441 { get; set; }
    //    public object customfield_19320 { get; set; }
    //    public object customfield_19329 { get; set; }
    //    public object customfield_19325 { get; set; }
    //    public object customfield_19446 { get; set; }
    //    public object customfield_19447 { get; set; }
    //    public object customfield_19326 { get; set; }
    //    public Resolution resolution { get; set; }
    //    public object customfield_19327 { get; set; }
    //    public object customfield_19328 { get; set; }
    //    public object customfield_20307 { get; set; }
    //    public object customfield_20306 { get; set; }
    //    public object customfield_15080 { get; set; }
    //    public object customfield_20304 { get; set; }
    //    public object customfield_19673 { get; set; }
    //    public object customfield_19552 { get; set; }
    //    public object customfield_19310 { get; set; }
    //    public object customfield_19311 { get; set; }
    //    public object customfield_19553 { get; set; }
    //    public object customfield_19432 { get; set; }
    //    public object customfield_19674 { get; set; }
    //    public object lastViewed { get; set; }
    //    public object customfield_19675 { get; set; }
    //    public object customfield_19554 { get; set; }
    //    public object customfield_19312 { get; set; }
    //    public Customfield_19433 customfield_19433 { get; set; }
    //    public object customfield_19313 { get; set; }
    //    public object customfield_19555 { get; set; }
    //    public object customfield_19434 { get; set; }
    //    public object customfield_19676 { get; set; }
    //    public object customfield_19670 { get; set; }
    //    public object customfield_19671 { get; set; }
    //    public object customfield_19430 { get; set; }
    //    public object customfield_19672 { get; set; }
    //    public object customfield_19318 { get; set; }
    //    public object customfield_19319 { get; set; }
    //    public object customfield_19435 { get; set; }
    //    public object customfield_19556 { get; set; }
    //    public object customfield_19314 { get; set; }
    //    public object customfield_19315 { get; set; }
    //    public object customfield_19436 { get; set; }
    //    public object customfield_19557 { get; set; }
    //    public object customfield_19437 { get; set; }
    //    public object customfield_19558 { get; set; }
    //    public object customfield_19316 { get; set; }
    //    public object customfield_19317 { get; set; }
    //    public object aggregatetimeoriginalestimate { get; set; }
    //    public Issuelink[] issuelinks { get; set; }
    //    public Assignee assignee { get; set; }
    //    public object customfield_19662 { get; set; }
    //    public object customfield_19420 { get; set; }
    //    public object customfield_19541 { get; set; }
    //    public object customfield_19421 { get; set; }
    //    public object customfield_19663 { get; set; }
    //    public object customfield_19300 { get; set; }
    //    public object customfield_19301 { get; set; }
    //    public object customfield_19664 { get; set; }
    //    public object customfield_19543 { get; set; }
    //    public object customfield_19422 { get; set; }
    //    public object customfield_19423 { get; set; }
    //    public object customfield_19544 { get; set; }
    //    public object customfield_19665 { get; set; }
    //    public object customfield_19302 { get; set; }
    //    public object customfield_19781 { get; set; }
    //    public object customfield_19660 { get; set; }
    //    public Customfield_17000 customfield_17000 { get; set; }
    //    public object customfield_19661 { get; set; }
    //    public object customfield_19307 { get; set; }
    //    public object customfield_19428 { get; set; }
    //    public object customfield_19429 { get; set; }
    //    public object customfield_19308 { get; set; }
    //    public object customfield_19309 { get; set; }
    //    public object customfield_19666 { get; set; }
    //    public object customfield_19545 { get; set; }
    //    public object customfield_19546 { get; set; }
    //    public object customfield_19667 { get; set; }
    //    public object customfield_19304 { get; set; }
    //    public object customfield_19305 { get; set; }
    //    public object customfield_19668 { get; set; }
    //    public object customfield_19547 { get; set; }
    //    public object customfield_19426 { get; set; }
    //    public object customfield_19427 { get; set; }
    //    public object customfield_19548 { get; set; }
    //    public object customfield_19669 { get; set; }
    //    public object customfield_19306 { get; set; }
    //    public object customfield_21614 { get; set; }
    //    public object customfield_21613 { get; set; }
    //    public string customfield_15180 { get; set; }
    //    public object customfield_19530 { get; set; }
    //    public object customfield_19531 { get; set; }
    //    public object customfield_19532 { get; set; }
    //    public object customfield_19411 { get; set; }
    //    public object customfield_19412 { get; set; }
    //    public object[] subtasks { get; set; }
    //    public object customfield_19417 { get; set; }
    //    public object customfield_19418 { get; set; }
    //    public object customfield_19419 { get; set; }
    //    public object customfield_19776 { get; set; }
    //    public object customfield_19413 { get; set; }
    //    public object customfield_19534 { get; set; }
    //    public object customfield_19414 { get; set; }
    //    public object customfield_19535 { get; set; }
    //    public object customfield_19415 { get; set; }
    //    public object customfield_19536 { get; set; }
    //    public object customfield_19899 { get; set; }
    //    public object customfield_19537 { get; set; }
    //    public object customfield_19416 { get; set; }
    //    public object customfield_21606 { get; set; }
    //    public Votes votes { get; set; }
    //    public object customfield_21604 { get; set; }
    //    public object customfield_14080 { get; set; }
    //    public object customfield_19640 { get; set; }
    //    public object customfield_24712 { get; set; }
    //    public Issuetype issuetype { get; set; }
    //    public object customfield_19883 { get; set; }
    //    public object customfield_19520 { get; set; }
    //    public object customfield_19641 { get; set; }
    //    public object customfield_18310 { get; set; }
    //    public object customfield_24713 { get; set; }
    //    public object customfield_19763 { get; set; }
    //    public object customfield_19642 { get; set; }
    //    public object customfield_19400 { get; set; }
    //    public object customfield_19521 { get; set; }
    //    public object customfield_15280 { get; set; }
    //    public object customfield_19401 { get; set; }
    //    public object customfield_19643 { get; set; }
    //    public object customfield_24715 { get; set; }
    //    public object customfield_15281 { get; set; }
    //    public object customfield_17100 { get; set; }
    //    public object customfield_19648 { get; set; }
    //    public object customfield_19406 { get; set; }
    //    public object customfield_19527 { get; set; }
    //    public object customfield_19528 { get; set; }
    //    public object customfield_19407 { get; set; }
    //    public object customfield_19408 { get; set; }
    //    public object customfield_19529 { get; set; }
    //    public object customfield_19644 { get; set; }
    //    public object customfield_19402 { get; set; }
    //    public object customfield_19523 { get; set; }
    //    public object customfield_19403 { get; set; }
    //    public object customfield_19524 { get; set; }
    //    public object customfield_19645 { get; set; }
    //    public object customfield_19646 { get; set; }
    //    public object customfield_19404 { get; set; }
    //    public object customfield_19525 { get; set; }
    //    public object customfield_19405 { get; set; }
    //    public object customfield_19526 { get; set; }
    //    public object customfield_19647 { get; set; }
    //    public object customfield_22924 { get; set; }
    //    public object customfield_21711 { get; set; }
    //    public object customfield_19751 { get; set; }
    //    public object customfield_19752 { get; set; }
    //    public object customfield_19510 { get; set; }
    //    public object customfield_19753 { get; set; }
    //    public object customfield_19990 { get; set; }
    //    public object customfield_24729 { get; set; }
    //    public object customfield_19637 { get; set; }
    //    public object customfield_19516 { get; set; }
    //    public object customfield_19638 { get; set; }
    //    public object customfield_19759 { get; set; }
    //    public object customfield_19639 { get; set; }
    //    public object customfield_19518 { get; set; }
    //    public object customfield_19519 { get; set; }
    //    public object customfield_19512 { get; set; }
    //    public object[] customfield_19754 { get; set; }
    //    public object customfield_19513 { get; set; }
    //    public object customfield_19755 { get; set; }
    //    public object customfield_19756 { get; set; }
    //    public object customfield_19636 { get; set; }
    //    public object customfield_19757 { get; set; }
    //    public object customfield_19509 { get; set; }
    //    public object customfield_24730 { get; set; }
    //    public object customfield_19981 { get; set; }
    //    public object customfield_19982 { get; set; }
    //    public object customfield_19861 { get; set; }
    //    public object customfield_19741 { get; set; }
    //    public object customfield_19620 { get; set; }
    //    public object customfield_19983 { get; set; }
    //    public object customfield_19862 { get; set; }
    //    public string customfield_19621 { get; set; }
    //    public object customfield_17201 { get; set; }
    //    public object customfield_19980 { get; set; }
    //    public object customfield_19626 { get; set; }
    //    public object customfield_19505 { get; set; }
    //    public object customfield_19506 { get; set; }
    //    public object customfield_19507 { get; set; }
    //    public object customfield_19508 { get; set; }
    //    public object customfield_19622 { get; set; }
    //    public object customfield_19501 { get; set; }
    //    public object customfield_19502 { get; set; }
    //    public object customfield_19623 { get; set; }
    //    public object customfield_19624 { get; set; }
    //    public object customfield_19503 { get; set; }
    //    public object customfield_19504 { get; set; }
    //    public object customfield_19625 { get; set; }
    //    public object customfield_19619 { get; set; }
    //    public object customfield_19850 { get; set; }
    //    public object customfield_19730 { get; set; }
    //    public object customfield_19851 { get; set; }
    //    public object customfield_18400 { get; set; }
    //    public object customfield_19731 { get; set; }
    //    public object customfield_19610 { get; set; }
    //    public object customfield_19978 { get; set; }
    //    public object customfield_19615 { get; set; }
    //    public object customfield_19979 { get; set; }
    //    public object customfield_19858 { get; set; }
    //    public object customfield_19616 { get; set; }
    //    public object customfield_19738 { get; set; }
    //    public object customfield_19859 { get; set; }
    //    public object customfield_19974 { get; set; }
    //    public object customfield_19853 { get; set; }
    //    public object customfield_19732 { get; set; }
    //    public object customfield_19611 { get; set; }
    //    public object customfield_19975 { get; set; }
    //    public object customfield_19733 { get; set; }
    //    public object customfield_19734 { get; set; }
    //    public object customfield_19735 { get; set; }
    //    public object customfield_19977 { get; set; }
    //    public Environment environment { get; set; }
    //    public object customfield_19609 { get; set; }
    //    public object duedate { get; set; }
    //    public object[] customfield_13980 { get; set; }
    //    public object customfield_11681 { get; set; }
    //    public object customfield_19398 { get; set; }
    //    public object customfield_19399 { get; set; }
    //    public object customfield_20020 { get; set; }
    //    public object customfield_19394 { get; set; }
    //    public object customfield_19395 { get; set; }
    //    public object customfield_19396 { get; set; }
    //    public object customfield_19397 { get; set; }
    //    public object customfield_12880 { get; set; }
    //    public object timeestimate { get; set; }
    //    public object customfield_19390 { get; set; }
    //    public object customfield_19392 { get; set; }
    //    public object customfield_20019 { get; set; }
    //    public object customfield_19393 { get; set; }
    //    public object customfield_20017 { get; set; }
    //    public Status status { get; set; }
    //    public object customfield_20016 { get; set; }
    //    public object customfield_19387 { get; set; }
    //    public object customfield_20134 { get; set; }
    //    public object customfield_20013 { get; set; }
    //    public object customfield_19388 { get; set; }
    //    public object customfield_20135 { get; set; }
    //    public object customfield_20014 { get; set; }
    //    public object customfield_19389 { get; set; }
    //    public object customfield_20132 { get; set; }
    //    public object customfield_20133 { get; set; }
    //    public object customfield_19383 { get; set; }
    //    public object customfield_20130 { get; set; }
    //    public object customfield_19384 { get; set; }
    //    public object customfield_20010 { get; set; }
    //    public object customfield_20131 { get; set; }
    //    public object customfield_19385 { get; set; }
    //    public object customfield_19386 { get; set; }
    //    public object aggregatetimeestimate { get; set; }
    //    public object customfield_19380 { get; set; }
    //    public object customfield_20129 { get; set; }
    //    public object customfield_19381 { get; set; }
    //    public object customfield_19382 { get; set; }
    //    public object customfield_20127 { get; set; }
    //    public object customfield_20128 { get; set; }
    //    public object customfield_20125 { get; set; }
    //    public object customfield_20126 { get; set; }
    //    public object customfield_19376 { get; set; }
    //    public object customfield_20123 { get; set; }
    //    public object customfield_19377 { get; set; }
    //    public object customfield_20124 { get; set; }
    //    public Creator creator { get; set; }
    //    public object customfield_19378 { get; set; }
    //    public object customfield_20363 { get; set; }
    //    public object customfield_20242 { get; set; }
    //    public object customfield_19379 { get; set; }
    //    public object customfield_20122 { get; set; }
    //    public object customfield_19372 { get; set; }
    //    public object customfield_19373 { get; set; }
    //    public object customfield_20362 { get; set; }
    //    public object customfield_20241 { get; set; }
    //    public object customfield_19374 { get; set; }
    //    public object customfield_19375 { get; set; }
    //    public object customfield_12983 { get; set; }
    //    public object customfield_12982 { get; set; }
    //    public object customfield_12985 { get; set; }
    //    public object customfield_12984 { get; set; }
    //    public object customfield_19491 { get; set; }
    //    public object customfield_19371 { get; set; }
    //    public object customfield_19492 { get; set; }
    //    public object customfield_21568 { get; set; }
    //    public object customfield_20359 { get; set; }
    //    public object customfield_21567 { get; set; }
    //    public object customfield_21566 { get; set; }
    //    public object customfield_19365 { get; set; }
    //    public object customfield_21565 { get; set; }
    //    public object customfield_19366 { get; set; }
    //    public object customfield_21564 { get; set; }
    //    public object customfield_19488 { get; set; }
    //    public object customfield_21563 { get; set; }
    //    public object customfield_21562 { get; set; }
    //    public object customfield_21561 { get; set; }
    //    public string customfield_19362 { get; set; }
    //    public object customfield_21681 { get; set; }
    //    public object customfield_21560 { get; set; }
    //    public object timespent { get; set; }
    //    public object customfield_19363 { get; set; }
    //    public object customfield_19000 { get; set; }
    //    public object customfield_19001 { get; set; }
    //    public DateTime? customfield_11880 { get; set; }
    //    public object aggregatetimespent { get; set; }
    //    public int workratio { get; set; }
    //    public object customfield_19480 { get; set; }
    //    public object customfield_21559 { get; set; }
    //    public object customfield_19360 { get; set; }
    //    public object customfield_21558 { get; set; }
    //    public object customfield_20226 { get; set; }
    //    public object customfield_20105 { get; set; }
    //    public object customfield_20227 { get; set; }
    //    public object customfield_21557 { get; set; }
    //    public object customfield_21556 { get; set; }
    //    public object customfield_20224 { get; set; }
    //    public object customfield_20225 { get; set; }
    //    public object customfield_19475 { get; set; }
    //    public object customfield_19354 { get; set; }
    //    public object customfield_20222 { get; set; }
    //    public object customfield_19476 { get; set; }
    //    public object customfield_20223 { get; set; }
    //    public object customfield_21432 { get; set; }
    //    public object customfield_19477 { get; set; }
    //    public object customfield_19356 { get; set; }
    //    public object customfield_20341 { get; set; }
    //    public object customfield_20220 { get; set; }
    //    public object customfield_19478 { get; set; }
    //    public object customfield_20221 { get; set; }
    //    public object customfield_19592 { get; set; }
    //    public object customfield_19471 { get; set; }
    //    public object customfield_19350 { get; set; }
    //    public object customfield_19593 { get; set; }
    //    public object customfield_20340 { get; set; }
    //    public object customfield_19594 { get; set; }
    //    public object customfield_19473 { get; set; }
    //    public object customfield_19474 { get; set; }
    //    public object customfield_19358 { get; set; }
    //    public object customfield_19359 { get; set; }
    //    public object customfield_20219 { get; set; }
    //    public object customfield_20217 { get; set; }
    //    public object customfield_20338 { get; set; }
    //    public object customfield_19590 { get; set; }
    //    public object customfield_20218 { get; set; }
    //    public object customfield_20339 { get; set; }
    //    public object customfield_19591 { get; set; }
    //    public object customfield_19470 { get; set; }
    //    public object customfield_20215 { get; set; }
    //    public object customfield_20337 { get; set; }
    //    public object customfield_20216 { get; set; }
    //    public object customfield_20213 { get; set; }
    //    public object customfield_20335 { get; set; }
    //    public object customfield_20214 { get; set; }
    //    public object customfield_19343 { get; set; }
    //    public object customfield_19101 { get; set; }
    //    public object customfield_19464 { get; set; }
    //    public object customfield_20211 { get; set; }
    //    public object customfield_19102 { get; set; }
    //    public object customfield_19344 { get; set; }
    //    public Customfield_19465 customfield_19465 { get; set; }
    //    public Customfield_19586 customfield_19586 { get; set; }
    //    public object customfield_20212 { get; set; }
    //    public object customfield_19345 { get; set; }
    //    public object customfield_19103 { get; set; }
    //    public object customfield_19587 { get; set; }
    //    public object customfield_19466 { get; set; }
    //    public object customfield_19588 { get; set; }
    //    public object customfield_19467 { get; set; }
    //    public object customfield_19104 { get; set; }
    //    public object customfield_19346 { get; set; }
    //    public object customfield_20210 { get; set; }
    //    public object customfield_19581 { get; set; }
    //    public object customfield_19582 { get; set; }
    //    public object customfield_19461 { get; set; }
    //    public object customfield_19340 { get; set; }
    //    public object customfield_19341 { get; set; }
    //    public object customfield_19583 { get; set; }
    //    public object customfield_19584 { get; set; }
    //    public object customfield_19100 { get; set; }
    //    public object customfield_19342 { get; set; }
    //    public object customfield_19347 { get; set; }
    //    public object customfield_19105 { get; set; }
    //    public object customfield_19589 { get; set; }
    //    public object customfield_19468 { get; set; }
    //    public object customfield_19469 { get; set; }
    //    public object customfield_19106 { get; set; }
    //    public object customfield_19348 { get; set; }
    //    public object customfield_19349 { get; set; }
    //    public object customfield_19107 { get; set; }
    //    public object customfield_19108 { get; set; }
    //    public object customfield_20208 { get; set; }
    //    public object customfield_20209 { get; set; }
    //    public object customfield_19580 { get; set; }
    //    public object customfield_20204 { get; set; }
    //    public object customfield_20205 { get; set; }
    //    public object customfield_20202 { get; set; }
    //    public object customfield_20203 { get; set; }
    //    public object customfield_19695 { get; set; }
    //    public object customfield_19574 { get; set; }
    //    public object customfield_19332 { get; set; }
    //    public object customfield_18001 { get; set; }
    //    public object customfield_19333 { get; set; }
    //    public object customfield_18002 { get; set; }
    //    public object customfield_19575 { get; set; }
    //    public object customfield_19454 { get; set; }
    //    public object customfield_19696 { get; set; }
    //    public object customfield_19697 { get; set; }
    //    public object customfield_19576 { get; set; }
    //    public object customfield_19455 { get; set; }
    //    public object customfield_18003 { get; set; }
    //    public Customfield_19334 customfield_19334 { get; set; }
    //    public object customfield_19335 { get; set; }
    //    public object customfield_18004 { get; set; }
    //    public object customfield_19577 { get; set; }
    //    public object customfield_19456 { get; set; }
    //    public object customfield_19698 { get; set; }
    //    public object customfield_19691 { get; set; }
    //    public object customfield_19571 { get; set; }
    //    public object customfield_19450 { get; set; }
    //    public object customfield_19572 { get; set; }
    //    public object customfield_19330 { get; set; }
    //    public object customfield_18000 { get; set; }
    //    public object customfield_19331 { get; set; }
    //    public object customfield_19573 { get; set; }
    //    public object customfield_19452 { get; set; }
    //    public object customfield_19694 { get; set; }
    //    public object customfield_10880 { get; set; }
    //    public object customfield_19699 { get; set; }
    //    public object customfield_19578 { get; set; }
    //    public object customfield_19457 { get; set; }
    //    public object customfield_19336 { get; set; }
    //    public object customfield_18005 { get; set; }
    //    public object customfield_19579 { get; set; }
    //    public object customfield_19339 { get; set; }
    //    public object customfield_21646 { get; set; }
    //    public object customfield_16180 { get; set; }
    //    public object customfield_21523 { get; set; }
    //    public object customfield_17700 { get; set; }
    //    public object customfield_12480 { get; set; }
    //    public string customfield_16600 { get; set; }
    //    public string[] labels { get; set; }
    //    public Component[] components { get; set; }
    //    public object customfield_20097 { get; set; }
    //    public DateTime? customfield_10170 { get; set; }
    //    public Customfield_15980 customfield_15980 { get; set; }
    //    public object customfield_20096 { get; set; }
    //    public object customfield_13680 { get; set; }
    //    public object customfield_14880 { get; set; }
    //    public object customfield_10160 { get; set; }
    //    public Reporter reporter { get; set; }
    //    public object customfield_14881 { get; set; }
    //    public object customfield_16702 { get; set; }
    //    public object customfield_16701 { get; set; }
    //    public object customfield_16700 { get; set; }
    //    public object customfield_24395 { get; set; }
    //    public object customfield_24396 { get; set; }
    //    public Progress progress { get; set; }
    //    public object customfield_20199 { get; set; }
    //    public object customfield_20196 { get; set; }
    //    public object customfield_20197 { get; set; }
    //    public object customfield_20192 { get; set; }
    //    public object customfield_20193 { get; set; }
    //    public Project project { get; set; }
    //    public string customfield_10032 { get; set; }
    //    public object customfield_20190 { get; set; }
    //    public object customfield_20191 { get; set; }
    //    public object customfield_17900 { get; set; }
    //    public DateTime? resolutiondate { get; set; }
    //    public object customfield_24286 { get; set; }
    //    public Watches watches { get; set; }
    //    public object customfield_20187 { get; set; }
    //    public object customfield_20188 { get; set; }
    //    public object customfield_20186 { get; set; }
    //    public object customfield_23210 { get; set; }
    //    public object customfield_20183 { get; set; }
    //    public object customfield_12680 { get; set; }
    //    public object customfield_20182 { get; set; }
    //    public object customfield_16801 { get; set; }
    //    public object customfield_16800 { get; set; }
    //    public object customfield_23209 { get; set; }
    //    public object customfield_23208 { get; set; }
    //    public DateTime updated { get; set; }
    //    public object customfield_20059 { get; set; }
    //    public object timeoriginalestimate { get; set; }
    //    public object customfield_20291 { get; set; }
    //    public object customfield_20292 { get; set; }
    //    public object customfield_13880 { get; set; }
    //    public Description description { get; set; }
    //    public object customfield_20290 { get; set; }
    //    public object customfield_10133 { get; set; }
    //    public string summary { get; set; }
    //    public object customfield_20288 { get; set; }
    //    public object customfield_20046 { get; set; }
    //    public object customfield_20043 { get; set; }
    //    public object customfield_12781 { get; set; }
    //    public object customfield_16901 { get; set; }
    //    public object customfield_16900 { get; set; }
    //    public object customfield_15480 { get; set; }
    //    public DateTime statuscategorychangedate { get; set; }
    //    public object customfield_24635 { get; set; }
    //    public string customfield_13180 { get; set; }
    //    public object customfield_24637 { get; set; }
    //    public object customfield_24758 { get; set; }
    //    public object customfield_24638 { get; set; }
    //    public object customfield_19720 { get; set; }
    //    public object customfield_24639 { get; set; }
    //    public object customfield_17300 { get; set; }
    //    public object customfield_19846 { get; set; }
    //    public object customfield_19847 { get; set; }
    //    public object customfield_19848 { get; set; }
    //    public object customfield_19849 { get; set; }
    //    public Fixversion[] fixVersions { get; set; }
    //    public object customfield_19721 { get; set; }
    //    public object customfield_19843 { get; set; }
    //    public object customfield_19964 { get; set; }
    //    public object customfield_19722 { get; set; }
    //    public object customfield_19723 { get; set; }
    //    public object customfield_19965 { get; set; }
    //    public object customfield_19844 { get; set; }
    //    public object customfield_19724 { get; set; }
    //    public object customfield_19845 { get; set; }
    //    public object customfield_19718 { get; set; }
    //    public object customfield_19719 { get; set; }
    //    public object customfield_15590 { get; set; }
    //    public object customfield_14380 { get; set; }
    //    public object customfield_14381 { get; set; }
    //    public object customfield_15591 { get; set; }
    //    public object[] customfield_16200 { get; set; }
    //    public object customfield_15594 { get; set; }
    //    public object customfield_15595 { get; set; }
    //    public object customfield_15592 { get; set; }
    //    public object customfield_15593 { get; set; }
    //    public object customfield_19714 { get; set; }
    //    public object customfield_19715 { get; set; }
    //    public object customfield_19716 { get; set; }
    //    public object customfield_15596 { get; set; }
    //    public object customfield_19717 { get; set; }
    //    public Priority priority { get; set; }
    //    public object customfield_18500 { get; set; }
    //    public object customfield_19711 { get; set; }
    //    public object customfield_19828 { get; set; }
    //    public Version[] versions { get; set; }
    //    public object customfield_19708 { get; set; }
    //    public object customfield_19709 { get; set; }
    //    public object customfield_24532 { get; set; }
    //    public object customfield_24535 { get; set; }
    //    public object customfield_24537 { get; set; }
    //    public object customfield_15583 { get; set; }
    //    public object customfield_15584 { get; set; }
    //    public object customfield_15581 { get; set; }
    //    public object customfield_15582 { get; set; }
    //    public object customfield_19824 { get; set; }
    //    public object customfield_15587 { get; set; }
    //    public object customfield_19825 { get; set; }
    //    public object customfield_17402 { get; set; }
    //    public object customfield_15588 { get; set; }
    //    public object customfield_19826 { get; set; }
    //    public object customfield_17401 { get; set; }
    //    public object customfield_15585 { get; set; }
    //    public object customfield_19827 { get; set; }
    //    public object customfield_17400 { get; set; }
    //    public object customfield_15586 { get; set; }
    //    public object customfield_19820 { get; set; }
    //    public object customfield_19821 { get; set; }
    //    public object customfield_19700 { get; set; }
    //    public object customfield_15589 { get; set; }
    //    public object customfield_19701 { get; set; }
    //    public object customfield_19822 { get; set; }
    //    public object customfield_19702 { get; set; }
    //    public object customfield_19823 { get; set; }
    //    public object customfield_24543 { get; set; }
    //    public object customfield_24544 { get; set; }
    //    public object customfield_14480 { get; set; }
    //    public object customfield_12180 { get; set; }
    //    public object customfield_14481 { get; set; }
    //    public object customfield_14482 { get; set; }
    //    public object customfield_19934 { get; set; }
    //    public object customfield_18603 { get; set; }
    //    public object customfield_16302 { get; set; }
    //    public object customfield_16301 { get; set; }
    //    public Aggregateprogress aggregateprogress { get; set; }
    //    public object customfield_18601 { get; set; }
    //    public object customfield_18602 { get; set; }
    //    public object customfield_19933 { get; set; }
    //    public object customfield_24677 { get; set; }
    //    public object customfield_24678 { get; set; }
    //    public object customfield_13380 { get; set; }
    //    public object customfield_13382 { get; set; }
    //    public object customfield_13381 { get; set; }
    //    public object customfield_17501 { get; set; }
    //    public Customfield_17500 customfield_17500 { get; set; }
    //    public object customfield_12280 { get; set; }
    //    public object customfield_12281 { get; set; }
    //    public DateTime created { get; set; }
    //    public object customfield_14580 { get; set; }
    //    public object customfield_23174 { get; set; }
    //    public object customfield_16401 { get; set; }
    //    public object customfield_16400 { get; set; }
    //    public object customfield_19910 { get; set; }
    //    public object customfield_18700 { get; set; }
    //    public object customfield_19911 { get; set; }
    //    public object customfield_19909 { get; set; }
    //    public object customfield_17606 { get; set; }
    //    public object customfield_19905 { get; set; }
    //    public object customfield_19906 { get; set; }
    //    public object customfield_19908 { get; set; }
    //    public object customfield_24213 { get; set; }
    //    public object customfield_24214 { get; set; }
    //    public object customfield_24215 { get; set; }
    //    public object customfield_24216 { get; set; }
    //    public object customfield_24217 { get; set; }
    //    public object customfield_23167 { get; set; }
    //    public object customfield_23166 { get; set; }
    //    public object customfield_13480 { get; set; }
    //    public object customfield_23165 { get; set; }
    //    public object customfield_23164 { get; set; }
    //    public object customfield_15780 { get; set; }
    //    public object customfield_23163 { get; set; }
    //    public object customfield_19901 { get; set; }
    //    public object customfield_23162 { get; set; }
    //    public object customfield_19902 { get; set; }
    //    public object customfield_23161 { get; set; }
    //    public object customfield_23160 { get; set; }
    //    public object customfield_17604 { get; set; }
    //    public object customfield_19900 { get; set; }
    //    public object security { get; set; }
    //    public object customfield_24222 { get; set; }
    //    public object customfield_24225 { get; set; }
    //    public object customfield_23159 { get; set; }
    //    public object customfield_23158 { get; set; }
    //    public object customfield_23157 { get; set; }
    //    public object customfield_23156 { get; set; }
    //    public object customfield_12381 { get; set; }
    //    public object customfield_23155 { get; set; }
    //    public object customfield_12380 { get; set; }
    //    public object customfield_23154 { get; set; }
    //    public object customfield_23153 { get; set; }
    //    public object customfield_23152 { get; set; }
    //    public object customfield_23151 { get; set; }
    //    public object customfield_16500 { get; set; }
    //    public object customfield_14683 { get; set; }
    //    public object customfield_18800 { get; set; }
    //}

    //public class Resolution
    //{
    //    public string self { get; set; }
    //    public string id { get; set; }
    //    public string description { get; set; }
    //    public string name { get; set; }
    //}

    //public class Customfield_19433
    //{
    //    public string self { get; set; }
    //    public string value { get; set; }
    //    public string id { get; set; }
    //}

    //public class Assignee
    //{
    //    public string self { get; set; }
    //    public string accountId { get; set; }
    //    public Avatarurls avatarUrls { get; set; }
    //    public string displayName { get; set; }
    //    public bool active { get; set; }
    //    public string timeZone { get; set; }
    //    public string accountType { get; set; }
    //}

    //public class Avatarurls
    //{
    //    public string _48x48 { get; set; }
    //    public string _24x24 { get; set; }
    //    public string _16x16 { get; set; }
    //    public string _32x32 { get; set; }
    //}

    //public class Customfield_17000
    //{
    //    public string self { get; set; }
    //    public string value { get; set; }
    //    public string id { get; set; }
    //}

    //public class Votes
    //{
    //    public string self { get; set; }
    //    public int votes { get; set; }
    //    public bool hasVoted { get; set; }
    //}

    //public class Issuetype
    //{
    //    public string self { get; set; }
    //    public string id { get; set; }
    //    public string description { get; set; }
    //    public string iconUrl { get; set; }
    //    public string name { get; set; }
    //    public bool subtask { get; set; }
    //    public int avatarId { get; set; }
    //    public int hierarchyLevel { get; set; }
    //}

    //public class Environment
    //{
    //    public string type { get; set; }
    //    public int version { get; set; }
    //    public Content[] content { get; set; }
    //}

    //public class Content
    //{
    //    public string type { get; set; }
    //    public Content1[] content { get; set; }
    //}

    //public class Content1
    //{
    //    public string type { get; set; }
    //    public string text { get; set; }
    //}

    //public class Status
    //{
    //    public string self { get; set; }
    //    public string description { get; set; }
    //    public string iconUrl { get; set; }
    //    public string name { get; set; }
    //    public string id { get; set; }
    //    public Statuscategory statusCategory { get; set; }
    //}

    //public class Statuscategory
    //{
    //    public string self { get; set; }
    //    public int id { get; set; }
    //    public string key { get; set; }
    //    public string colorName { get; set; }
    //    public string name { get; set; }
    //}

    //public class Creator
    //{
    //    public string self { get; set; }
    //    public string accountId { get; set; }
    //    public Avatarurls1 avatarUrls { get; set; }
    //    public string displayName { get; set; }
    //    public bool active { get; set; }
    //    public string timeZone { get; set; }
    //    public string accountType { get; set; }
    //}

    //public class Avatarurls1
    //{
    //    public string _48x48 { get; set; }
    //    public string _24x24 { get; set; }
    //    public string _16x16 { get; set; }
    //    public string _32x32 { get; set; }
    //}

    //public class Customfield_19465
    //{
    //    public string self { get; set; }
    //    public string value { get; set; }
    //    public string id { get; set; }
    //}

    //public class Customfield_19586
    //{
    //    public string self { get; set; }
    //    public string value { get; set; }
    //    public string id { get; set; }
    //}

    //public class Customfield_19334
    //{
    //    public string self { get; set; }
    //    public string value { get; set; }
    //    public string id { get; set; }
    //}

    //public class Customfield_15980
    //{
    //    public bool hasEpicLinkFieldDependency { get; set; }
    //    public bool showField { get; set; }
    //}

    //public class Reporter
    //{
    //    public string self { get; set; }
    //    public string accountId { get; set; }
    //    public Avatarurls2 avatarUrls { get; set; }
    //    public string displayName { get; set; }
    //    public bool active { get; set; }
    //    public string timeZone { get; set; }
    //    public string accountType { get; set; }
    //}

    //public class Avatarurls2
    //{
    //    public string _48x48 { get; set; }
    //    public string _24x24 { get; set; }
    //    public string _16x16 { get; set; }
    //    public string _32x32 { get; set; }
    //}

    //public class Progress
    //{
    //    public int progress { get; set; }
    //    public int total { get; set; }
    //}

    //public class Project
    //{
    //    public string self { get; set; }
    //    public string id { get; set; }
    //    public string key { get; set; }
    //    public string name { get; set; }
    //    public string projectTypeKey { get; set; }
    //    public bool simplified { get; set; }
    //    public Avatarurls3 avatarUrls { get; set; }
    //    public Projectcategory projectCategory { get; set; }
    //}

    //public class Avatarurls3
    //{
    //    public string _48x48 { get; set; }
    //    public string _24x24 { get; set; }
    //    public string _16x16 { get; set; }
    //    public string _32x32 { get; set; }
    //}

    //public class Projectcategory
    //{
    //    public string self { get; set; }
    //    public string id { get; set; }
    //    public string description { get; set; }
    //    public string name { get; set; }
    //}

    //public class Watches
    //{
    //    public string self { get; set; }
    //    public int watchCount { get; set; }
    //    public bool isWatching { get; set; }
    //}

    //public class Description
    //{
    //    public string type { get; set; }
    //    public int version { get; set; }
    //    public Content2[] content { get; set; }
    //}

    //public class Content2
    //{
    //    public string type { get; set; }
    //    public Content3[] content { get; set; }
    //    public Attrs attrs { get; set; }
    //}

    //public class Attrs
    //{
    //    public bool isNumberColumnEnabled { get; set; }
    //    public string layout { get; set; }
    //    public int level { get; set; }
    //    public string language { get; set; }
    //}

    //public class Content3
    //{
    //    public string type { get; set; }
    //    public string text { get; set; }
    //    public Mark[] marks { get; set; }
    //    public Content4[] content { get; set; }
    //    public Attrs1 attrs { get; set; }
    //}

    //public class Attrs1
    //{
    //    public string shortName { get; set; }
    //    public string id { get; set; }
    //    public string text { get; set; }
    //    public string accessLevel { get; set; }
    //}

    //public class Mark
    //{
    //    public string type { get; set; }
    //    public Attrs2 attrs { get; set; }
    //}

    //public class Attrs2
    //{
    //    public string href { get; set; }
    //}

    //public class Content4
    //{
    //    public string type { get; set; }
    //    public Content5[] content { get; set; }
    //    public Attrs3 attrs { get; set; }
    //}

    //public class Attrs3
    //{
    //    public string language { get; set; }
    //}

    //public class Content5
    //{
    //    public string type { get; set; }
    //    public string text { get; set; }
    //    public Mark1[] marks { get; set; }
    //    public Content6[] content { get; set; }
    //}

    //public class Mark1
    //{
    //    public string type { get; set; }
    //    public Attrs4 attrs { get; set; }
    //}

    //public class Attrs4
    //{
    //    public string href { get; set; }
    //}

    //public class Content6
    //{
    //    public string type { get; set; }
    //    public string text { get; set; }
    //    public Mark2[] marks { get; set; }
    //}

    //public class Mark2
    //{
    //    public string type { get; set; }
    //    public Attrs5 attrs { get; set; }
    //}

    //public class Attrs5
    //{
    //    public string href { get; set; }
    //}

    //public class Priority
    //{
    //    public string self { get; set; }
    //    public string iconUrl { get; set; }
    //    public string name { get; set; }
    //    public string id { get; set; }
    //}

    //public class Aggregateprogress
    //{
    //    public int progress { get; set; }
    //    public int total { get; set; }
    //}

    //public class Customfield_17500
    //{
    //    public string type { get; set; }
    //    public int version { get; set; }
    //    public Content7[] content { get; set; }
    //}

    //public class Content7
    //{
    //    public string type { get; set; }
    //    public Content8[] content { get; set; }
    //}

    //public class Content8
    //{
    //    public string type { get; set; }
    //    public string text { get; set; }
    //}

    //public class Issuelink
    //{
    //    public string id { get; set; }
    //    public string self { get; set; }
    //    public Type type { get; set; }
    //    public Outwardissue outwardIssue { get; set; }
    //    public Inwardissue inwardIssue { get; set; }
    //}

    //public class Type
    //{
    //    public string id { get; set; }
    //    public string name { get; set; }
    //    public string inward { get; set; }
    //    public string outward { get; set; }
    //    public string self { get; set; }
    //}

    //public class Outwardissue
    //{
    //    public string id { get; set; }
    //    public string key { get; set; }
    //    public string self { get; set; }
    //    public Fields1 fields { get; set; }
    //}

    //public class Fields1
    //{
    //    public string summary { get; set; }
    //    public Status1 status { get; set; }
    //    public Priority1 priority { get; set; }
    //    public Issuetype1 issuetype { get; set; }
    //}

    //public class Status1
    //{
    //    public string self { get; set; }
    //    public string description { get; set; }
    //    public string iconUrl { get; set; }
    //    public string name { get; set; }
    //    public string id { get; set; }
    //    public Statuscategory1 statusCategory { get; set; }
    //}

    //public class Statuscategory1
    //{
    //    public string self { get; set; }
    //    public int id { get; set; }
    //    public string key { get; set; }
    //    public string colorName { get; set; }
    //    public string name { get; set; }
    //}

    //public class Priority1
    //{
    //    public string self { get; set; }
    //    public string iconUrl { get; set; }
    //    public string name { get; set; }
    //    public string id { get; set; }
    //}

    //public class Issuetype1
    //{
    //    public string self { get; set; }
    //    public string id { get; set; }
    //    public string description { get; set; }
    //    public string iconUrl { get; set; }
    //    public string name { get; set; }
    //    public bool subtask { get; set; }
    //    public int avatarId { get; set; }
    //    public int hierarchyLevel { get; set; }
    //}

    //public class Inwardissue
    //{
    //    public string id { get; set; }
    //    public string key { get; set; }
    //    public string self { get; set; }
    //    public Fields2 fields { get; set; }
    //}

    //public class Fields2
    //{
    //    public string summary { get; set; }
    //    public Status2 status { get; set; }
    //    public Priority2 priority { get; set; }
    //    public Issuetype2 issuetype { get; set; }
    //}

    //public class Status2
    //{
    //    public string self { get; set; }
    //    public string description { get; set; }
    //    public string iconUrl { get; set; }
    //    public string name { get; set; }
    //    public string id { get; set; }
    //    public Statuscategory2 statusCategory { get; set; }
    //}

    //public class Statuscategory2
    //{
    //    public string self { get; set; }
    //    public int id { get; set; }
    //    public string key { get; set; }
    //    public string colorName { get; set; }
    //    public string name { get; set; }
    //}

    //public class Priority2
    //{
    //    public string self { get; set; }
    //    public string iconUrl { get; set; }
    //    public string name { get; set; }
    //    public string id { get; set; }
    //}

    //public class Issuetype2
    //{
    //    public string self { get; set; }
    //    public string id { get; set; }
    //    public string description { get; set; }
    //    public string iconUrl { get; set; }
    //    public string name { get; set; }
    //    public bool subtask { get; set; }
    //    public int avatarId { get; set; }
    //    public int hierarchyLevel { get; set; }
    //}

    //public class Component
    //{
    //    public string self { get; set; }
    //    public string id { get; set; }
    //    public string name { get; set; }
    //}

    //public class Fixversion
    //{
    //    public string self { get; set; }
    //    public string id { get; set; }
    //    public string name { get; set; }
    //    public bool archived { get; set; }
    //    public bool released { get; set; }
    //    public string description { get; set; }
    //    public string releaseDate { get; set; }
    //}

    //public class Version
    //{
    //    public string self { get; set; }
    //    public string id { get; set; }
    //    public string name { get; set; }
    //    public bool archived { get; set; }
    //    public bool released { get; set; }
    //    public string description { get; set; }
    //    public string releaseDate { get; set; }
    //}


    //#endregion

    public void Save(ConfigWriter writer, String label)
    {
        SaveAttributes(writer);
    }

    protected internal static JiraIssueReceiver RestoreProvider(Dictionary<string, object> values)
    {
        IssueReceiverInterface.IssueProvider IssueProvider = IssueReceiverInterface.IssueProvider.GitHubIssueReceiver;
        if (ConfigIO.RestoreEnum(values, label, ref IssueProvider))
        {
            JiraIssueReceiver jiraIssueReceiver = new JiraIssueReceiver(new SEECity());
            jiraIssueReceiver.RestoreAttributes(values);
            return jiraIssueReceiver;
        }
        else
        {
            throw new Exception($"Specification of JiraIssueReceiver: label {IssueProvider} is missing.");
        }
    }

    public static JiraIssueReceiver Restore(Dictionary<string, object> attributes, string label)
    {
        if (attributes.TryGetValue(label, out object dictionary))
        {
            Dictionary<string, object> values = dictionary as Dictionary<string, object>;
            return RestoreProvider(values);
        }
        else
        {
            throw new Exception($"A JiraIssueReceiver could not be found under the label {label}.");
        }
    }

    public void SaveAttributes(ConfigWriter writer)
    {

        writer.BeginGroup("");
        writer.Save(this.GetType().ToString(), "Type");

        //SaveAttributes(writer);
        writer.EndGroup();
        //this.se
        //writer.BeginList(pipelineLabel);
        //foreach (CallConvThiscall.)
        //{
        //    provider.Save(writer, "");
        //}
        //writer.EndList();
    }

    public void RestoreAttributes(Dictionary<string, object> attributes)
    {

    }

    public async Task<bool> createIssue(Dictionary<string, string> attributes)
    {
        // Es konnte kein Issue erstelltwerden!
        return false;
    }
    public async Task<bool> updateIssue()
    {
        // Es konnte kein Issue upgedatet werden!
        return false;
    }
    public bool downloadDone = false;
    private async void restAPI(Settings settings)
    {
        int total = 50;
        int startAT = -1;
        string pagingString = "";

        while (startAT < total)
        {
            if (startAT != -1)
            {
                pagingString = $"&startAt={startAT.ToString()}";
            }

            //Jira Query Language(JQL)
            UnityWebRequest request = UnityWebRequest.Get(settings.preUrl + settings.searchUrl + pagingString);


#pragma warning disable CS4014
            request.SendWebRequest();
#pragma warning restore CS4014
            await UniTask.WaitUntil(() => request.isDone);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.result);
                //Rückgabe
                Debug.Log(request.downloadHandler.text);
            }


            string docPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);

            //// Write the string array to a new file named "WriteLines.txt".
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "IssueTestOutputJira.txt")))
            {
                outputFile.Write(request.downloadHandler.text);
            }




            //DeserializeObject der Json response
            //  JsonConvert.DeserializeObject(request.downloadHandler.text);

            Dictionary<string, System.Object> dic = JsonConvert.DeserializeObject<Dictionary<string, System.Object>>(request.downloadHandler.text);

            total = Convert.ToInt32(dic["total"]);

            UnityEngine.Debug.Log($"IssueConvert:{total}");
            // total = (int)dic["total"];

            // UnityEngine.Debug.Log($"Start at: {rootobject.startAt.ToString()}");
            startAT = Convert.ToInt32(dic["startAt"]) + Convert.ToInt32(dic["maxResults"]);// rootobject.startAt + rootobject.maxResults;
                                                                                           //total = rootobject.total;
                                                                                           //// -1 da die 0 mit zählt
                                                                                           //startAT = rootobject.startAt + rootobject.maxResults;

            //// gibt den descriptions aller Issues in der Console aus.
            ///   
            //Dictionary<string, System.Object> issuesDictionary = JsonConvert.DeserializeObject<Dictionary<string, System.Object>>( dic["issues"].ToString());
            issuesJ = JArray.Parse(dic["issues"].ToString());
            //foreach (JToken item in JArray.Parse(dic["issues"].ToString()))
            //{

            //        issuesJ.Add(item);
            //   Console.WriteLine($"Jap{issuesJ.Count()}: ");

            //}

            //if (issuesJ != null)
            //{
            //    //   ShowNotification.Error("issuesJ Is not NUll", "Error", 5, true); }
            //    //= issuesArray;
            //    using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "IssueTestOutputJiraDescipt.txt"), true))
            //    {
            //        foreach (JObject issue in issuesJ)
            //        {
            //            // foreach (Issue issue in rootobject.issues)

            //            foreach (JProperty property in issue.Properties())
            //            {
            //                string keyV = property.Name;
            //                // J//Token value = property.Value;
            //                outputFile.WriteLine($"{keyV}: {property.Value}");
            //                // Console.WriteLine($"{keyV}: {value}");
            //                // oder UnityEngine.Debug.Log($"{key}: {value}");
            //            }
            //            //string key = issue["key"]?.ToString() ?? "Kein Key";
            //            //    string description = issue["fields"]?["description"]?.ToString() ?? "Keine Beschreibung";


            //            //UnityEngine.Debug.Log($"{key}: {description}");
            //            //  outputFile.Write($"{keypair.Key}:  {keypair.Value}/n");
            //            //  UnityEngine.Debug.Log($"Description:  {issue.fields.issuetype.description}/n");
            //        }

            //    }
            //}
        }

    }
    public async Task<JArray> getIssues(Settings settings)
    {
        issuesJ = new JArray();
        issues = null;
        downloadDone = false;
        restAPI(settings);

        return issuesJ;
    }

}
