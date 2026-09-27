using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using BambooStages123;
using Bamboo45;

namespace BambooCompare {
 public sealed class TypeCandidate {
  public CompareMetric Metric;public Spec Spec;public Evaluation Eval;
 }
 public sealed class ComparisonResult {
  public List<TypeCandidate> Candidates=new List<TypeCandidate>(),Baselines=new List<TypeCandidate>();public List<int> Types;
  public TypeCandidate Best,Worst;public Dictionary<int,TypeCandidate> Representatives=new Dictionary<int,TypeCandidate>();
  public bool Complete=true;public string StopReason="";public int Requested;
 }
 public static class ComparisonEngine {
  public static double[] Areas(GeometrySet g){
   return new[]{g.Network.Roof.Sum(t=>t.AreaVector.Z),g.Network.Roof.Sum(t=>t.AreaVector.Length)};
  }
  public static TypeCandidate Wrap(Spec spec,double factor,Settings settings,Evaluation e,double refArea,double areaTol){
   var a=e.Geometry==null?new[]{double.NaN,double.NaN}:Areas(e.Geometry);int type=ComparisonRules.Code(spec);
   var m=new CompareMetric{Id=CandidateSpace.Key(spec,factor,settings.AD),Type=type,Factor=factor,Diameter=settings.AD,Solved=e.Solved,Warning=e.Warnings.Count>0,Pass=e.Pass,Score=e.Score,Mass=e.Mass,Stress=e.StressMax,Displacement=e.Displacement,PlanArea=a[0],SurfaceArea=a[1]};
   m.Allowed=e.Geometry!=null&&ComparisonRules.AreaAllowed(a[0],refArea,areaTol);
   m.Reason=e.State;
   if(e.Geometry!=null&&!m.Allowed)m.Reason+=$"；投影面积差{Math.Abs(a[0]/refArea-1):P1}超过AreaTol={areaTol:P1}，不参与全局推荐";
   return new TypeCandidate{Spec=spec.Copy(),Metric=m,Eval=e};
  }
  public static ComparisonResult Search(Spec basis,List<Frame3> planes,List<double> heights,int rails,Settings settings,List<int> types,List<double> factors,List<double> diameters,double tolerance,double areaTol,Evaluation baseline,CandidateSpace space,int maximum,Func<string,GeometrySet,Settings,Evaluation> solve=null){
   solve=solve??Engine.Evaluate;var result=new ComparisonResult{Types=types,Requested=space.CheckBudget(types,factors.Count,diameters.Count,maximum)};
   var refArea=Areas(baseline.Geometry)[0];
   if(Systemic(baseline)){result.Complete=false;result.StopReason=baseline.State;return result;}
   var cache=new Dictionary<string,TypeCandidate>(StringComparer.Ordinal);
   // Baselines are reference results only. A failed baseline never removes its type.
   foreach(int type in types){
    var spec=ComparisonRules.WithType(space.Baseline(basis),type);var cs=settings.Copy();
    var e=type==ComparisonRules.Code(basis)?baseline:EvaluateCandidate(spec,planes,heights,rails,1,cs,tolerance,solve);
    var c=Wrap(spec,1,cs,e,refArea,areaTol);result.Baselines.Add(c);cache[c.Metric.Id]=c;
    if(Systemic(e)){result.Complete=false;result.StopReason=e.State;goto Finished;}
   }
   foreach(var seed in space.Seeds(basis,types,factors,diameters)){
    TypeCandidate c;if(!cache.TryGetValue(seed.Id,out c)){
     var cs=settings.Copy();cs.AD=seed.Diameter;
     var e=EvaluateCandidate(seed.Spec,planes,heights,rails,seed.Factor,cs,tolerance,solve);
     c=Wrap(seed.Spec,seed.Factor,cs,e,refArea,areaTol);cache[seed.Id]=c;
    }
    result.Candidates.Add(c);
    if(Systemic(c.Eval)){result.Complete=false;result.StopReason=c.Eval.State;goto Finished;}
   }
   Finished:
   Select(result);return result;
  }
  static Evaluation EvaluateCandidate(Spec spec,List<Frame3> planes,List<double> heights,int rails,double factor,Settings settings,double tolerance,Func<string,GeometrySet,Settings,Evaluation> solve){
   GeometrySet g=null;string name=ComparisonRules.Names[ComparisonRules.Code(spec)];
   try{
    g=Engine.Geometry(spec,planes,heights,rails,factor,tolerance);
    Settings.Require(g.VerticalHeights.All(h=>h>=500&&h<=12000),"候选总高超出本版0.5—12m范围，无法传入05B");
    if(settings.HeightMax>0)Settings.Require(g.VerticalHeights.Max()<=settings.HeightMax+.001,"超过场地限高");
    foreach(var m in g.Network.Curves)foreach(double u in new[]{0.0,.5,1.0}){
     var p=m.At(u);
     if(settings.SiteWidth>0)Settings.Require(Math.Abs(p.X)<=settings.SiteWidth/2+.001,"超出横向场地边界");
     if(settings.AxisLength>0)Settings.Require(p.Y>=settings.SiteOrigin-settings.EndMargin-.001&&p.Y<=settings.SiteOrigin+settings.AxisLength+settings.EndMargin+.001,"超出纵向场地边界");
    }
    return solve(name,g,settings);
   }catch(OperationCanceledException){throw;}catch(Exception ex){return new Evaluation{Name=name,Geometry=g,Settings=settings,State="候选未完成："+ex.Message,Error=ex.Message};}
  }
  public static void Select(ComparisonResult r){
   r.Best=r.Worst=null;r.Representatives.Clear();
   if(r.Complete){var b=ComparisonRules.Best(r.Candidates.Select(c=>c.Metric));var w=ComparisonRules.Worst(r.Candidates.Select(c=>c.Metric));r.Best=r.Candidates.FirstOrDefault(c=>c.Metric==b);r.Worst=r.Candidates.FirstOrDefault(c=>c.Metric==w);}
   foreach(int type in r.Types){var list=r.Candidates.Where(c=>c.Metric.Type==type).ToList();var winner=list.Select(c=>c.Metric).Where(m=>m.LocallyEligible&&m.Pass).OrderBy(m=>m.Mass).ThenBy(m=>m.Score).ThenBy(m=>m.Id,StringComparer.Ordinal).FirstOrDefault();
    r.Representatives[type]=list.FirstOrDefault(c=>c.Metric==winner)??list.Where(c=>c.Metric.LocallyEligible).OrderBy(c=>c.Metric.Score).FirstOrDefault()??list.FirstOrDefault(c=>c.Eval.Geometry!=null&&c.Metric.Allowed)??list.FirstOrDefault(c=>c.Eval.Geometry!=null)??list.FirstOrDefault();
   }
  }
  public static bool Systemic(Evaluation e)=>e!=null&&((e.Error??"").Contains("许可证")||(e.Error??"").IndexOf("license",StringComparison.OrdinalIgnoreCase)>=0||(e.Error??"").IndexOf("dll",StringComparison.OrdinalIgnoreCase)>=0||(e.Error??"").Contains("程序集"));
  public static string TypeReport(ComparisonResult r,int type){
   if(r==null)return "未运行跨原型搜索";if(!r.Types.Contains(type))return "未纳入TypeSet";
   var rows=r.Candidates.Where(c=>c.Metric.Type==type).ToList();var rep=r.Representatives.ContainsKey(type)?r.Representatives[type]:null;
   int eligible=rows.Count(c=>c.Metric.Eligible),good=rows.Count(c=>c.Metric.LocallyEligible&&c.Metric.Pass);
   string count=$"候选{rows.Count}，完成计算{rows.Count(c=>c.Eval.Solved)}，初筛满足{good}，可比已求解{eligible}";
   if(rep==null)return count+"；搜索中断或尚未检查";
   if(!rep.Metric.LocallyEligible)return count+"；"+rep.Metric.Reason;
   var m=rep.Metric;string rank=r.Complete?(m.Pass?"本型初筛推荐":"本型全部超限，仅供改进比较"):"搜索未完成，暂不排名";
   string difference=!m.Allowed?"；"+m.Reason+"；满足所设阈值时仍可采用本型最优":r.Best==null?"":rep==r.Best?"；当前B":$"；相对B质量{m.Mass-r.Best.Metric.Mass:+0.##;-0.##;0}kg，位移{m.Displacement-r.Best.Metric.Displacement:+0.###;-0.###;0}mm";
   if(m.Allowed&&r.Best!=null&&rep!=r.Best){var b=r.Best.Metric;difference+="；未列为B："+(b.Pass&&!m.Pass?"本型仍超限，B满足所设初筛":b.Pass&&m.Mass>b.Mass?"本型代表也满足初筛，但质量更大":!b.Pass&&m.Score>b.Score?"本型最大超限比更大":"按质量/超限比的次级排序；相等时用固定编号消除随机排序，不构成性能差异");}
   return $"{count}；{rank}；H倍率={m.Factor:0.###}；实际总高={rep.Eval.Geometry.VerticalHeights[0]:0.###}mm；D={m.Diameter:0.##}mm；"+CandidateSpace.Parameters(rep.Spec)+$"；位移={m.Displacement:0.###}mm；应力={m.Stress:0.###}MPa；质量={m.Mass:0.##}kg；超限比={m.Score:0.###}；投影/实际面积={m.PlanArea:0.##}/{m.SurfaceArea:0.##}m²"+difference;
  }
  public static string Recommendation(ComparisonResult r,Evaluation baseline){
   if(r==null)return "CrossCompare=False或尚未Run+Optimize；保留原有同型搜索。";
   if(!r.Complete)return "跨原型搜索未完成，暂不输出B推荐，避免用部分结果冒充完整比较。原因："+r.StopReason;
   if(r.Best==null)return "没有可推荐候选：先看各类型几何不适用、面积不一致、约束不满足或求解警告。失败不等于坍塌，不能据此断言换型有效。";
   var c=r.Best;var m=c.Metric;int count=r.Candidates.Count(x=>x.Metric.Eligible),types=r.Candidates.Where(x=>x.Metric.Eligible).Select(x=>x.Metric.Type).Distinct().Count();
   string result=m.Pass?$"推荐B：{ComparisonRules.Names[m.Type]}。在{types}类、{count}个可比已求解候选中，先筛选满足所设阈值者，再取杆件质量最小者。":"没有候选满足所设初筛。B仅为最大超限比最小的改进候选："+ComparisonRules.Names[m.Type]+"。";
   result+=$"\nB：H倍率={m.Factor:0.###}，D={m.Diameter:0.##}mm，最大超限比={m.Score:0.###}；{(c.Eval.Settings.Strength?"包含用户正应力阈值":"强度未评估，不能称为强度合格")}。";
   if(baseline.Solved)result+=$"\nB相对C当前设计：位移差={m.Displacement-baseline.Displacement:+0.###;-0.###;0}mm，质量差={m.Mass-baseline.Mass:+0.##;-0.##;0}kg，应力差={m.Stress-baseline.StressMax:+0.###;-0.###;0}MPa。尺寸、外径和类型可能同时改变，不把差值全部归因于拱型。";
   if(types<2)result+="\n仅一个原型取得可比有效结果，不能据此宣称该类型优于其他类型。";
   return result+"\n不是全局最优、规范安全批准或施工建议。材料性能、节点和稳定性仍须核查；C由①—③及当前截面生成，B不自动回写C。";
  }
 }
}
