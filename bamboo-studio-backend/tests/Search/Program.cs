using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using BambooStages123;
using BambooCompare;
using Bamboo45;
class Program {
 static int checks;static void Check(bool ok,string text){checks++;if(!ok)throw new Exception(text);}
 static void Reject(Action a,string text){bool rejected=false;try{a();}catch(ArgumentException){rejected=true;}Check(rejected,text);}
 static CandidateSpace Space(double[] panels=null)=>CandidateSpace.Create(new[]{25.0,15,35},new[]{450.0,300,600},panels??new[]{8.0,6,10},new[]{1200.0},new[]{.9},new[]{10.0},new[]{6.0});
 static void Main(){
  var types=new List<int>{0,1,2,4,5};var factors=new List<double>{.9,1,1.1};var diameters=new List<double>{100,150,300};
  var sp=new Spec{Shape=1,Structure=0,Foot=1,Units=2,W=5600,H=4040,LegH=1800,PointedVersion=3,PointLowerD=120,PointWebD=60};
  var space=Space();sp=space.Baseline(sp);
  Check(space.Count(types,3,3)==135,"default candidate count");
  Check(space.Count(types,1,3)==45,"locked-height candidate count");
  Check(space.CheckBudget(types,3,3,135)==135,"exact budget accepted");
  Reject(()=>space.CheckBudget(types,3,3,120),"budget stops before search");
  Reject(()=>Space(new[]{7.0}),"odd panel counts rejected");
  Reject(()=>Space(new[]{8.5}),"fractional panel counts rejected");
  Reject(()=>CandidateSpace.Values(new[]{double.NaN},"x",0,1),"nonfinite rejected");
  Reject(()=>CandidateSpace.Values(Array.Empty<double>(),"x",0,1),"empty list rejected");
  Check(space.Baseline(sp).Depth==450&&space.Baseline(sp).CrossAngle==25&&space.Baseline(sp).Panels==8,"first entry defines baseline, not lowest");
  var seeds=space.Seeds(sp,types,factors,diameters).ToList();Check(seeds.Count==135,"enumeration count");
  Check(seeds.Select(x=>x.Id).Distinct().Count()==135,"all compound candidates have distinct IDs");
  foreach(var pair in new Dictionary<int,int>{{0,9},{1,9},{2,27},{4,81},{5,9}})Check(seeds.Count(x=>ComparisonRules.Code(x.Spec)==pair.Key)==pair.Value,"type-specific product count");
  Check(seeds.All(x=>x.Spec.W==5600),"type search never changes common span");
  var expanded=CandidateSpace.Create(new[]{25.0},new[]{450.0},new[]{8.0},new[]{1200.0,900},new[]{.9,.8},new[]{10.0,20},new[]{6.0,8});
  Check(expanded.Variants(5)==16&&expanded.Variants(0)==1,"pointed-only expansion");
  Check(expanded.Specs(sp,5).Select(x=>CandidateSpace.Key(x,1,100)).Distinct().Count()==16,"pointed parameter keys unique");
  Check(space.Fingerprint()!=expanded.Fingerprint(),"changed search space changes cache fingerprint");
  var ys=MathRules.Stations(12000,2000,true,.001).Select(y=>y+2500).ToArray();
  var frames=ys.Select(y=>new Frame3(new P3(0,y,0),new P3(1,0,0),new P3(0,1,0))).ToList();var heights=ys.Select(_=>4040.0).ToList();
  var settings=new Settings();var geometry=Engine.Geometry(sp,frames,heights,9,1,.001);var baseline=Engine.Evaluate("baseline",geometry,settings);
  var result=ComparisonEngine.Search(sp,frames,heights,9,settings,types,factors,diameters,.001,.15,baseline,space,300);
  Check(result.Complete&&result.Candidates.Count==135,"full grid accounted for");
  Check(result.Baselines.Count==5,"five independent baselines reported");
  Check(result.Baselines.All(x=>x.Eval.Geometry.VerticalHeights[0]==4040&&x.Metric.Diameter==100),"all baselines share height and diameter");
  Check(!result.Baselines.Single(x=>x.Metric.Type==4).Metric.Pass,"fixture compound baseline fails");
  Check(result.Candidates.Any(x=>x.Metric.Type==4&&x.Metric.Pass),"failed baseline does not prune type");
  Check(Engine.Calls.Count==Engine.Calls.Distinct().Count(),"baseline candidates reused, no duplicate solves");
  Check(result.Candidates.Any(x=>!x.Metric.Solved)&&result.Candidates.Where(x=>!x.Metric.Solved).All(x=>x.Metric.Reason.Length>0),"invalid geometry retained with reasons");
  Check(result.Candidates.Any(x=>!x.Metric.Allowed)&&result.Best!=null&&result.Best.Metric.Allowed,"area rule enforced for global recommendation");
  Check(result.Candidates.Where(x=>x.Eval.Geometry!=null).All(x=>Math.Abs(x.Eval.Geometry.VerticalHeights[0]-(1800+(4040-1800)*x.Metric.Factor))<1e-7),"rise scaling retains leg height");
  Engine.Calls.Clear();
  var locked=ComparisonEngine.Search(sp,frames,heights,9,settings,types,new List<double>{1},diameters,.001,.15,baseline,space,300);
  Check(locked.Candidates.Count==45&&locked.Candidates.Where(x=>x.Eval.Geometry!=null).All(x=>x.Eval.Geometry.VerticalHeights[0]==4040),"height lock propagated to every type");
  // The baseline is still reported even if the search omits its height and diameter.
  var omitted=ComparisonEngine.Search(sp,frames,heights,9,settings,types,new List<double>{1.1},new List<double>{150},.001,.15,baseline,space,300);
  Check(omitted.Baselines.Count==5&&omitted.Candidates.Count==15,"baseline outside search grid remains separate");
  Check(omitted.Baselines.All(x=>x.Metric.Factor==1&&x.Metric.Diameter==100),"baseline is not an arbitrary search result");
  var low=settings.Copy();low.HeightMax=4100;
  var limited=ComparisonEngine.Search(sp,frames,heights,9,low,types,factors,diameters,.001,.15,baseline,space,300);
  Check(limited.Candidates.Where(x=>x.Metric.Factor==1.1).All(x=>!x.Metric.Solved),"height limit excludes over-height candidates before solver");
  var report=new{checks,requested=result.Requested,geometry_valid=result.Candidates.Count(x=>x.Eval.Geometry!=null),mock_solved=result.Candidates.Count(x=>x.Metric.Solved),invalid=result.Candidates.Count(x=>!x.Metric.Solved),types=result.Candidates.GroupBy(x=>x.Metric.Type).Select(g=>new{type=g.Key,count=g.Count()}),note="Real geometry and production search engine; mock FE scores used solely for software tests. No structural validation."};
  string json=JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true});File.WriteAllText("test-results.json",json);Console.WriteLine(json);
 }
}
