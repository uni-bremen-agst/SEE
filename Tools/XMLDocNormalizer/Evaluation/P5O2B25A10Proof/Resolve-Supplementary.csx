// Phase-0 evidence generator only. No test invocation, analyzer invocation,
// project evaluation/build, process management, or writes outside A10 Proof.
#r "Microsoft.CodeAnalysis.dll"
#r "Microsoft.CodeAnalysis.CSharp.dll"
#r "System.Web.Extensions"
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var proof = "Evaluation/P5O2B25A10Proof/";
var output = proof + "Supplementary-resolved-bindings-r2.json";
if (File.Exists(output)) throw new Exception("Preserve completed binding record; do not silently rerun/overwrite.");
var json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 200 };
Dictionary<string, object> Load(string p) { return json.Deserialize<Dictionary<string, object>>(File.ReadAllText(p)); }
IEnumerable<Dictionary<string, object>> Rows(object a) { return ((IEnumerable)a).Cast<Dictionary<string, object>>(); }
string Text(Dictionary<string, object> d, string k) { return (string)d[k]; }
string Hash(string p) { using(var h=SHA256.Create()) return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(p))).Replace("-", ""); }
string Norm(string s) { return Regex.Replace(s, @"\s+", ""); }
var catalog = Load(proof + "Complete-fixture-preflight.json");
var inventory = Load("artifacts/p5o2b25a10/test-path-inventory-r1.json");
foreach(var f in Rows(inventory["Files"])) if (Hash(Text(f,"Path")) != Text(f,"Sha256")) throw new Exception("Frozen test/helper changed: " + f["Path"]);
var reviews = new Dictionary<string,string[]>();
string owner = null;
foreach(var line in File.ReadAllLines(proof + "Supplementary-method-reviews.txt"))
{
    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
    if (line.StartsWith("[")) { owner=line.Trim('[',']'); continue; }
    var parts=line.Split(new[]{'|'},3); if(parts.Length!=3) throw new Exception(line);
    reviews.Add(owner+"."+parts[0],new[]{parts[1],parts[2]});
}
var methods=Rows(catalog["WorkerMethods"]).Where(m=>Rows(m["Cases"]).Any(c=>Rows(c["Assertions"]).Any(a=>Text(a,"Classification")=="AMBIGUOUS"))).ToArray();
if(methods.Length!=85 || reviews.Count!=85 || methods.Any(m=>!reviews.ContainsKey(Text(m,"Test")))) throw new Exception("Review coverage mismatch");
var trees=new Dictionary<string,SyntaxTree>();
SyntaxTree Tree(string p) { p=p.Replace('\\','/'); if(!trees.ContainsKey(p)) trees.Add(p,CSharpSyntaxTree.ParseText(File.ReadAllText(p),path:p)); return trees[p]; }
int Line(SyntaxNode n) { return n.SyntaxTree.GetLineSpan(n.Span).StartLinePosition.Line+1; }
string MethodId(MethodDeclarationSyntax n) { var t=n.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();return (t==null?"<top-level>":t.Identifier.ValueText)+"."+n.Identifier.ValueText; }
var sourceDefinitions=new Dictionary<string,object>();
object Definition(MethodDeclarationSyntax m)
{
    var key=m.SyntaxTree.FilePath+":"+Line(m)+":"+MethodId(m);
    if(!sourceDefinitions.ContainsKey(key)) sourceDefinitions.Add(key,new {
        File=m.SyntaxTree.FilePath, FileSha256=Hash(m.SyntaxTree.FilePath), Line=Line(m), Method=MethodId(m), Source=m.ToString(),
        Parameters=m.ParameterList.Parameters.Select(p=>p.ToString()).ToArray(),
        OwnerFields=m.Ancestors().OfType<TypeDeclarationSyntax>().First().Members.OfType<FieldDeclarationSyntax>().Select(f=>f.ToString()).ToArray(),
        OwnerProperties=m.Ancestors().OfType<TypeDeclarationSyntax>().First().Members.OfType<PropertyDeclarationSyntax>().Select(f=>f.ToString()).ToArray(),
        Calls=m.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i=>!i.Expression.ToString().StartsWith("Assert.")).Select(i=>new {Line=Line(i), Call=i.ToString()}).ToArray(),
        LocalDefinitions=m.DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(v=>v.Initializer!=null).Select(v=>new {Line=Line(v),Name=v.Identifier.ValueText,Value=v.Initializer.Value.ToString()}).ToArray(),
        Assignments=m.DescendantNodes().OfType<AssignmentExpressionSyntax>().Select(a=>new {Line=Line(a),Target=a.Left.ToString(),Value=a.Right.ToString()}).ToArray()
    });
    return key;
}
object B(string c,string entry,string input) { return new { Classification=c,Entry=entry,Input=input }; }
const string E="EXACT", O="OUTSIDE PROFILE", U="UNRELATED", D="DIAGNOSTIC";
const string session="Current Session.Analyze / HistoricalAnalysis -> shared AnalyzeSolutionTransitivelyThrownExceptions -> Session.Analyze (as specified in method review)";
const string independent="Independent validation/transport/selection/direct/metadata path; not certified summary admission";
object[] One(string c,string input) { return new[]{B(c,c==U?independent:c==D?"Low-level representation/graph introspection; not a certified result":session,input)}; }
object[] EO(string finite,string outside) { return new[]{B(E,session,finite),B(O,session,outside)}; }
object[] EU(string finite,string separate) { return new[]{B(E,session,finite),B(U,independent,separate)}; }
string CaseValue(Dictionary<string, object> c,int n) { return ((IEnumerable)c["Parameters"]).Cast<object>().ElementAt(n).ToString().Trim('"'); }
object Constant(ExpressionSyntax n,Dictionary<string,object> env)
{
    if(n is LiteralExpressionSyntax) return ((LiteralExpressionSyntax)n).Token.Value;
    if(n is ParenthesizedExpressionSyntax) return Constant(((ParenthesizedExpressionSyntax)n).Expression,env);
    if(n is IdentifierNameSyntax) {object value; return env.TryGetValue(((IdentifierNameSyntax)n).Identifier.ValueText,out value)?value:null;}
    if(n is MemberAccessExpressionSyntax) return ((MemberAccessExpressionSyntax)n).Name.Identifier.ValueText;
    if(n is PrefixUnaryExpressionSyntax && n.IsKind(SyntaxKind.LogicalNotExpression)) {var v=Constant(((PrefixUnaryExpressionSyntax)n).Operand,env);return v is bool?(object)!(bool)v:null;}
    if(n is IsPatternExpressionSyntax)
    {
        var e=(IsPatternExpressionSyntax)n; var v=Constant(e.Expression,env);
        Func<PatternSyntax,object> match=null;
        match=p=>p is ConstantPatternSyntax ? (v==null?null:(object)Equals(v,Constant(((ConstantPatternSyntax)p).Expression,env))) :
            p is BinaryPatternSyntax && p.IsKind(SyntaxKind.OrPattern) ? Or(match(((BinaryPatternSyntax)p).Left),match(((BinaryPatternSyntax)p).Right)) : null;
        return match(e.Pattern);
    }
    if(n is BinaryExpressionSyntax)
    {
        var b=(BinaryExpressionSyntax)n; var l=Constant(b.Left,env);var r=Constant(b.Right,env);
        if(b.IsKind(SyntaxKind.LogicalOrExpression)) return Or(l,r);
        if(b.IsKind(SyntaxKind.LogicalAndExpression)) return And(l,r);
        if(l==null || r==null) return null;
        if(b.IsKind(SyntaxKind.EqualsExpression)) return Equals(l,r);
        if(b.IsKind(SyntaxKind.NotEqualsExpression)) return !Equals(l,r);
        if(l is int && r is int)
        {
            if(b.IsKind(SyntaxKind.GreaterThanExpression))return (int)l>(int)r;
            if(b.IsKind(SyntaxKind.LessThanExpression))return (int)l<(int)r;
            if(b.IsKind(SyntaxKind.GreaterThanOrEqualExpression))return (int)l>=(int)r;
            if(b.IsKind(SyntaxKind.LessThanOrEqualExpression))return (int)l<=(int)r;
        }
    }
    return null; // Explicit symbolic data guard, never guessed or evaluated as C#.
}
object Or(object a,object b) {if(Equals(a,true)||Equals(b,true))return true;if(Equals(a,false)&&Equals(b,false))return false;return null;}
object And(object a,object b) {if(Equals(a,false)||Equals(b,false))return false;if(Equals(a,true)&&Equals(b,true))return true;return null;}
Dictionary<string,object> EnvironmentFor(MethodDeclarationSyntax method,Dictionary<string,object> c)
{
    var env=new Dictionary<string,object>(); var args=((IEnumerable)c["Parameters"]).Cast<object>().ToArray();
    for(int i=0;i<method.ParameterList.Parameters.Count;i++)env[method.ParameterList.Parameters[i].Identifier.ValueText]=i<args.Length?Constant(SyntaxFactory.ParseExpression(args[i].ToString()),new Dictionary<string,object>()):null;
    var id=MethodId(method);
    if(id=="ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat")env["expectedSummary"]=!new[]{"external","no-doc","guard-direct"}.Contains(CaseValue(c,0));
    if(id=="ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat")env["summary"]=CaseValue(c,0)=="true"&&CaseValue(c,1)!="no-doc";
    return env;
}
object[] Guards(InvocationExpressionSyntax node,Dictionary<string,object> env,bool primary)
{
    var result=new List<object>();
    foreach(var n in node.Ancestors().OfType<IfStatementSyntax>())
    {
        bool inverse=n.Else!=null && n.Else.Span.Contains(node.Span);
        var v=primary?Constant(n.Condition,env):null;
        if(inverse && v is bool)v=!(bool)v;
        result.Add(new {Condition=(inverse?"!(":"(")+n.Condition+")",Value=v,Meaning=v==null?"Retained data/helper guard; evidence origin remains the explicitly bound input(s), not an assertion executed unconditionally":"Case-bound parameter/derived guard"});
    }
    return result.ToArray();
}
object[] Bind(string id,Dictionary<string,object> c,string assertionOwner,int line)
{
    // These are explicit reviewed source locations/argument bindings. No matching
    // of assertion text, fixture names, or method-name patterns decides a profile.
    var note=reviews[id][1]; var defaultClass=reviews[id][0];
    var helper=assertionOwner.Substring(assertionOwner.LastIndexOf('.')+1);
    if(new[]{"NoHistoricalLoaded","AssertNoHistoricalLoaded","AssertNoHistorical","AssertStopped","Stopped","Isolation"}.Contains(helper)) return One(U,"Owned-process/isolation boundary; no summary computation here");
    if(assertionOwner.StartsWith("ExternalCompilationProvenanceDescriptorFactoryTests.")) return One(U,"Portable PDB metadata compilation/emit setup, not exception-flow evaluation");
    if(assertionOwner=="ExceptionFlowCanonicalExceptionIdentityTests.CurrentResult") return One(U,"CurrentResult input construction asserts fixed framework reference count and source compilation validity BEFORE the shared Session call; not exact-complete result assertions");
    if(helper=="Summaries") return One(D,"TryCreateSummary serializes existing graph entries only; no production root admission, evaluation, cache promotion, or finding output");
    if(id=="ExceptionFlowAnalysisDispatchTests.WorkerStructuredFailure_PropagatesOriginalFailureAndStopsProcess" || id=="ExceptionFlowAnalysisRouterTests.HistoricalAnalysisFailure_PreservesOriginalB4FailureContract")
        return One(CaseValue(c,1)=="AnalysisFailure"?O:U,note);
    if(id=="HistoricalWorkerClientTests.RealWorkerFailure_IsStructuredAndNeverEmptySuccess") return One(CaseValue(c,1)=="AnalysisFailure"?O:U,note);
    if(id=="ExceptionFlowHistoricalDeclaredPolicyTests.IncompleteRequestOriginalReceiptsAndUnsupportedShapes_DoNotActivateOrDowngrade")
        return helper=="Native"?EO("Native(payload=local), finite Root->A field throws","Native(uncertain=Payload(uncertain)), Root GC.Collect, MissingBody"):One(U,note);
    if(id=="ExceptionFlowHistoricalDeclaredProductionFindingTests.SelectedSummaryWorkerFailure_DoesNotPublishPartialDirectFindings")
    {
        if(line==255) return One(U,"result.Direct evidence analyzes direct ArgumentException, does not request summary");
        return One(O,"result Summary requested with reachable GC.Collect; no final findings or partial fallback; code obligation R10 separately indexed");
    }
    if(id=="ExceptionFlowHistoricalDeclaredReadinessTests.NoLocalDeclarationGuard_CannotBeReplacedByUnguardedProjectExecution")
    {
        if(helper=="Native") return One(O,"Unconditional Project Native session with GC.Collect, despite separate Declared detector guard");
        if(line>=95 && line<=97) return One(U,"Native.Direct / Declared no-local guard, no requested summary; keep empty findings");
        return One(O,"Unguarded opt-in Project worker GC.Collect, MissingBody; original refusal/selection metadata unchanged");
    }
    if(id=="ExceptionFlowHistoricalFindingContractTests.OriginalContextReceipt_RequestAdmissionLegacyAndUncertainGuard_AreNotBypassed")
        return helper=="Native"?One(O,"Native(uncertain), external Root GC.Collect; declared findings no-local guard independent"):One(U,note);
    if(id=="ExceptionFlowHistoricalReferenceIdentityTests.MissingExternalReference_HasSameCompilerErrorAndNoSuccessfulOrFallbackResult")
        return helper=="Native"?One(O,"Current Native session on CS0246 compilation, RepresentationOutsideGrammar; equality is repeat consistency, NOT exact complete output"):One(U,"Worker CS0246 CompilationFailure before admission; existing errors/no-result unchanged");
    if(id=="ExceptionFlowHistoricalInputEquivalenceTests.Inventory_DifferentialWitnessesSeparateParseReferencesSourcesAndNullable")
    {
        if(line==194 || line==202) return One(U,"Worker lacks required PE/source and fails compilation before admission");
        if(helper=="Native") return EO("defined/noDefines/newLanguage/disabled/enabled/withReference/multiple: their actual finite closed programs","oldLanguage C#10 raw-string source: compilation-error control only, not positive flow output");
        return One(E,note);
    }
    if(id=="ExceptionFlowHistoricalScopeTests.CrossProjectSourceClosure_IsRealCurrentInputAndExplicitB15Boundary")
    {
        if(line==222) return One(U,"Refused raw request lacks validated solution graph receipt, no process");
        if(line==218) return EO("solutionFlow after RegisterSupportingSource(dependency)","projectFlow PE-only Helper.Thrower, MissingBody; inequality remains required");
        return One(E,"solutionFlow only: actual registered Project.B body, finite Root->Helper.Thrower, stable repeat");
    }
    if(id=="ExceptionFlowHistoricalSolutionScopeTests.MissingReachableSourceProject_DoesNotReturnACompleteSmallerScope")
    {
        if(helper=="Native" || line==180) return One(E,"Native(full, SolutionTransitive), full P0/P1/P2 source receipts; NOT missing request");
        return One(O,"Separate missing request has only B source plus C PE, missing required C body; code obligation R10");
    }
    if(id=="ExceptionFlowHistoricalSolutionScopeTests.LocalModes_DoNotWidenToSupportingProjects")
    {
        bool direct=CaseValue(c,0)=="Direct";
        if(helper=="Native") return direct?EU("Native(full,SolutionTransitive), complete registered P0/P1/P2","Native(local,Direct), root-only direct analyzer"):
            EO("Native(full,SolutionTransitive), complete registered P0/P1/P2","Native(local,ProjectTransitive), PE-only callee: MissingBody");
        if(line==100) return One(U,"Original local-mode projection refusal before dispatch");
        if(line==103) return One(U,"Inside Direct branch only: raw rootOnly Direct worker, independent of Session admission");
        return direct?EU("Full Solution comparator","Selected local Direct analyzer"):
            EO("Full Solution comparator","Selected local Project session missing supporting body; no widening");
    }
    if(id=="ExceptionFlowHistoricalScopeTests.DeclaredMode_IsGenuineDetectorPolicyNotInventedWorkerTraversal")
    {
        if(line==196) return One(U,"Original Declared projection refused before dispatch");
        return EU("Native(input,Project) finite Root->Helper.Thrower","Native(input,Declared), no-local-type guard uses Direct/no graph");
    }
    if(id=="ExceptionFlowHistoricalProductionAdmissionTests.ActualProductionGuard_AndGenuineB19Inputs_AreRecordedWithoutPartialActivation")
    {
        bool guard=CaseValue(c,0)=="guard";
        if(!guard) return One(E,note);
        if(helper=="Native") return One(O,"Native(payload) unconditionally calls Project session with GC.Collect");
        if(line==131 || line==133 || line==135 || line==154) return One(U,"Production Direct / Declared guard graph zero and empty declared findings, no requested summary");
        if(line==132) return new[]{B(O,session,"Native and Production Project both GC.Collect; repeated/findings equality, no known ordinary missing finding"),B(U,independent,"Declared/direct controls remain independent")};
        return One(O,"guard only: unguarded Project worker GC.Collect, R10 code obligation; no result or partial document");
    }
    if(id=="ExceptionFlowHistoricalDeclaredProductionFindingTests.ProductiveDetector_GenuineAttestedFindingsFullParityAndRepeat")
    {
        string scenario=CaseValue(c,0); bool summary=!new[]{"external","no-doc","guard-direct"}.Contains(scenario);
        if(helper=="Native") return One(scenario=="guard-direct"?O:E,"Unconditional Native(payload) summary; guard-direct GC.Collect is OUTSIDE, other bodies finite");
        if(line>=86 && line<=87) return One(O,"guard-direct projectCall ONLY, unguarded GC.Collect; R10. Other cases do not enter this branch");
        if(line>=88 && line<=89) return One(U,"guard-direct first.Findings DOC610 + graph0 are guarded Direct-only, NOT projectCall");
        if(line>=94 && line<=111) return One(E,"Non-guard-direct projectCall and optional chain/local/combined probe: finite admitted source");
        if(line==77) return One(U,"Clone of selected call cannot mint receipt; independent even for admitted source");
        return One(summary?E:U,summary?"first/repeat selected Declared summary, actual finite source":"first/repeat actual guard suppresses summary; Direct evidence/findings only");
    }
    if(id=="ExceptionFlowHistoricalBoundedFindingTests.GenuineModeContract_FullNativeFindingEvidenceAndRepeat")
    {
        bool summary=CaseValue(c,0)=="true" && CaseValue(c,1)!="no-doc";
        if(helper=="Native") return One(E,"Unconditional Native finite source summary, independent of selected reporting mode");
        if(line==58 || line==59) return One(U,"first.Direct evidence, not summary admission");
        return One(summary?E:U,summary?"Selected Project summary of finite payload":"Selected Direct/no-doc reporting path does not request summary");
    }
    if(id=="ExceptionFlowHistoricalDeclaredBoundaryTests.ActualCurrentPolicyDifferential_DeclaredIsNotAnAnalyzerModeOrXmlFlow")
    {
        string scenario=CaseValue(c,0); bool hasLocal=!new[]{"no-local-type","xml-only"}.Contains(scenario);
        if(helper=="Native") return new[]{B(E,session,"Native Project and Solution, same finite payload"),B(U,independent,"Native Direct"),B(hasLocal?E:U,hasLocal?session:independent,"Native Declared actual local-type guard (case-bound)")};
        if(line==113 || line==116) return One(U,"Original declared request/raw declared mode refused before execution");
        if(line==80 || line==81 || line==82 || line==93) return One(U,"Direct / no-local Declared guarded mode, not admitted summary");
        return One(E,note);
    }
    // Setup Native baselines in otherwise negative receipt tests are real sessions,
    // unlike the rejected result tested by their own method-body assertions.
    if(helper=="Native") return One(E,"Actual finite Native baseline described by this method review; no forged request/response is passed to Native: "+note);
    if(helper=="AssertGraph") return One(E,"AssertGraph receives native/historical results for the SAME finite valid payload; not the independent compilation-failure control");
    if(id=="ExceptionFlowHistoricalReferenceIdentityTests.RealExternalMetadata_FullCanonicalIdentitySourcesUncertaintyGraphRepeatAndIsolation" && line==43) return One(U,"before lacks dependency PE, CompilationFailure before admission");
    if(id=="ExceptionFlowHistoricalProductionAdmissionTests.CoherentBindingSubstitution_IsRejectedWithoutExecutionAttestation_AndCannotEnterProduction" && line<65) return One(E,"Finite genuine Native/Project baseline; redirect-named modifies XML cref only");
    if(id=="ExceptionFlowHistoricalDeclaredPolicyTests.FullFactsAndActualFindingEligibility_DifferentialRepeatOwnershipAndIsolation" && (line==59 || line==60)) return One(U,"Original source ownership/declaration membership, independent of summary admission");
    if(id=="ExceptionFlowHistoricalScopeTests.CompleteProject_OriginalHandoffsFullResultGraphFindingsOwnershipRepeatAndIsolation" && (line==54 || line==72)) return One(U,"Exact source order/ownership receipt comparison, not flow evaluation");
    return One(defaultClass=="E"?E:defaultClass=="O"?O:U,note);
}

// Exact required assertion changes in these 85 methods. Everything else is
// protected; R10 rows outside this subset remain in the existing final batch.
string Permission(string id,Dictionary<string,object> c,int line)
{
    if(id=="ExceptionFlowConstructedInvocationContextTests.GenuineWorkerAndCurrentShareExistingFiniteDefinitionFold_NotConstructedCorrectness" && line>=195 && line<=205) return "R2 APPROVED conditional; classification parity, no successful folded output";
    if(id=="ExceptionFlowCanonicalExceptionIdentityTests.StopRule_CurrentAndGenuineHistoricalShareConstructionLoss_NotCorrectness")
    {
        if(line==148) return "Prior P09 APPROVED conditional: actual concrete containing arguments, all three cases";
        if(line==145 && CaseValue(c,0)=="two-constructed-callees") return "Prior P10 APPROVED conditional: two distinct concrete identities";
    }
    if(id=="ExceptionFlowHistoricalSolutionFindingTests.AssemblyDistinctSameDeclarationId_LosesTypeOwnerDuringSharedEvaluation_AndRemainsUnadmitted" && line==105) return "Prior P15 APPROVED conditional: actual assembly owner P1, not P0";
    if(id=="ExceptionFlowAnalysisDispatchTests.WorkerStructuredFailure_PropagatesOriginalFailureAndStopsProcess" && CaseValue(c,1)=="AnalysisFailure" && line==194) return "R10 PENDING: await case expected code only";
    if(id=="ExceptionFlowAnalysisRouterTests.HistoricalAnalysisFailure_PreservesOriginalB4FailureContract" && CaseValue(c,1)=="AnalysisFailure" && line==172) return "R10 PENDING: await case expected code only";
    if(id=="HistoricalWorkerClientTests.RealWorkerFailure_IsStructuredAndNeverEmptySuccess" && CaseValue(c,1)=="AnalysisFailure" && line==93) return "R10 PENDING: await case expected code only";
    return "PRESERVE; no additional permission/change required by this observation";
}
var resolved=new List<object>(); var caseRecords=new List<object>(); var obligations=new List<object>();
var methodIndex=new Dictionary<string,List<MethodDeclarationSyntax>>();
foreach(var f in Rows(inventory["Files"]))
foreach(var n in Tree(Text(f,"Path")).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
{
    string key=MethodId(n);if(!methodIndex.ContainsKey(key))methodIndex.Add(key,new List<MethodDeclarationSyntax>());methodIndex[key].Add(n);
}
int caseCount=0;
foreach(var m in methods)
{
    string id=Text(m,"Test"),file=Text(m,"File").Replace('\\','/');
    var method=Tree(file).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(n=>MethodId(n)==id);
    string definition=(string)Definition(method);
    var helperDefinitions=new List<object>();
    foreach(var name in ((IEnumerable)m["Helpers"]).Cast<string>())
        if(methodIndex.ContainsKey(name))foreach(var h in methodIndex[name])helperDefinitions.Add(Definition(h));
    var cases=Rows(m["Cases"]).Where(c=>Rows(c["Assertions"]).Any(a=>Text(a,"Classification")=="AMBIGUOUS")).ToArray();
    foreach(var c in cases)
    {
        caseCount++;
        var env=EnvironmentFor(method,c);
        var p=method.ParameterList.Parameters.Select((n,i)=>new {Name=n.Identifier.ValueText,Value=((IEnumerable)c["Parameters"]).Cast<object>().ElementAtOrDefault(i)}).ToArray();
        var caseChanges=new List<object>();
        foreach(var a in Rows(c["Assertions"]))
        {
            string af=Text(a,"File").Replace('\\','/'); int line=Convert.ToInt32(a["Line"]); string current=Text(a,"Current");
            string permission=af==file?Permission(id,c,line):"PRESERVE; helper contract unchanged";
            if(!permission.StartsWith("PRESERVE")) caseChanges.Add(new {File=af,Line=line,Assertion=current,Permission=permission});
            if(Text(a,"Classification")!="AMBIGUOUS") continue;
            var ast=Tree(af).GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Where(n=>Line(n)==line && Norm(n.ToString())==Norm(current)).ToArray();
            if(ast.Length!=1) throw new Exception("Assertion source binding not unique: "+id+" "+af+":"+line+" matches="+ast.Length);
            var node=ast[0]; var containing=node.Ancestors().OfType<MethodDeclarationSyntax>().First();
            string assertOwner=MethodId(containing); var conditions=Guards(node,env,assertOwner==id);
            bool inactive=conditions.Any(g=>Equals(g.GetType().GetProperty("Value").GetValue(g),false));
            var bindings=Bind(id,c,assertOwner,line);
            if(inactive)bindings=One(U,"Not executed in this parameter case: original if/else predicate is false. The assertion itself is preserved, not deleted or relaxed.");
            resolved.Add(new { Observation=resolved.Count+1,Test=id,Case=c["Case"],Parameters=p,File=af,Line=line,SpanStart=node.SpanStart,Assertion=current,
                OwningMethod=assertOwner,SourceDefinition=Definition(containing),TestDefinition=definition,Conditions=conditions,
                ParameterBranchReachability=inactive?"INACTIVE in this parameter case":"Active or retained data/helper-loop predicate, see Conditions and concrete Bindings; not all closure observations are distinct executions",
                ConditionsPolicy="Retain original branch/loop predicates, including guards derived from the actual fixture; this closure observation is not a separate assertion execution for an inactive branch. See bound input/mode variants and method review, never assert all branches execute.",
                Bindings=bindings,Review=reviews[id][1],Change=permission,BindingBasis="Explicit source review + exact AST assertion/owner + actual parameter values + owning method definitions/locals/assignments/calls. Not assertion text/name classification." });
        }
        // Failure-code obligations are not all in the old ambiguous subset. Keep
        // the exact existing R10 request linked without widening it to a helper.
        var existing=Rows(catalog["Changes"]).Where(x=>Text(x,"Group")=="R10" && Text(x,"Case").StartsWith(id+" [")).Select(x=>new {Group="R10",Approved=false,Selector=x["Parameters"],ExactAssertions=x["Assertions"],NewSemantics=x["NewSemantics"]}).ToArray();
        caseRecords.Add(new {Test=id,Case=c["Case"],Parameters=p,File=file,TestDefinition=definition,HelperSourceDefinitions=helperDefinitions,Review=reviews[id][1],
            OriginalFactoryLinks=c["ActualFixtureLinks"],OriginalSourceArguments=c["ActualSourceArguments"],
            FactoryLinksPolicy="Original discovery links only; selected/mutated source/mode/result origin is resolved by explicit review and observation bindings, not the former any-with flag",
            RequiredAssertionChanges=caseChanges,ExistingConditionalR10Scope=existing,ScopePolicy="R10 selector is normative: exact parameter/branch only, not all cases of this method",NewR11Required=false});
        obligations.AddRange(caseChanges);
    }
}
if(caseCount!=429 || resolved.Count!=3202) throw new Exception("Supplementary count mismatch "+caseCount+"/"+resolved.Count);
var record=new {Kind="Read-only supplementary assertion binding closure; NOT implementation admission/validation",Revision=2,PreviousOwnedDraft="Supplementary-resolved-bindings.json (retained, superseded by explicit parameter-branch/metadata-origin refinement)",Head="9b76e179a568c64ae972cc3a45980a094e44245e",Methods=85,ParameterCases=caseCount,AssertionObservations=resolved.Count,
    FrozenTestHelperFilesVerified=340,UnboundAssertions=0,NewR11Required=false,
    Scope="Original revision-4 AMBIGUOUS observations only, one-to-one preserved; one observation may bind multiple real helper invocations/modes, which remain separate classified origins. Other original assertions stay protected except exact indexed prior/R1-R10 grants.",
    Approval="R1-R7/prior explicit grants approved conditional; R8=32/R9=27/R10=14 remain pending; no implementation clearance",
    PriorCoverage="Characterization-approval-batch.md P09/P10/P15 and R2; existing Final-consolidated-approval-batch.md R8/R9/R10, no widening",
    ChangedSourceFiles=0,ChangedTestFiles=0,BuildOrTestRun=false,HistoricalWorkerExecuted=false,CaseRecords=caseRecords,Observations=resolved,SourceDefinitions=sourceDefinitions};
File.WriteAllText(output,json.Serialize(record)+Environment.NewLine,new UTF8Encoding(false));
Console.WriteLine("BOUND: 85 methods / "+caseCount+" cases / "+resolved.Count+" observations; "+sourceDefinitions.Count+" exact source definitions. No product/test execution. No R11 inferred.");
