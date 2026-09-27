using System;
using System.Linq;
using System.Collections.Generic;
using BambooStages123;

namespace BambooCompare {
 public sealed class CompareMetric {
  public string Id,Reason="";public int Type;
  public bool Solved,Warning,Pass,Allowed=true;
  public double Factor,Diameter,Score,Mass,Stress,Displacement,PlanArea,SurfaceArea;
  public bool Eligible=>Allowed&&LocallyEligible;
  public bool LocallyEligible=>Solved&&!Warning&&double.IsFinite(Score)&&Score>=0&&double.IsFinite(Mass)&&Mass>0;
 }
 public static class ComparisonRules {
  public static readonly string[] Names={"单圆拱","单尖拱","交叉拱","侧部交叉尖拱","复合圆拱","复合尖拱"};
  public static int Code(Spec s)=>s.Structure*2+(s.Shape==1?0:1);
  public static Spec WithType(Spec source,int code){MathRules.Require(code>=0&&code<6,"原型编号须为0..5。");var s=source.Copy();s.Structure=code/2;s.Shape=code%2==0?1:0;return s;}
  public static List<int> Types(IEnumerable<int> codes){var result=codes.Distinct().OrderBy(x=>x).ToList();MathRules.Require(result.Count>0&&result.All(x=>x>=0&&x<6),"TypeSet至少包含一个0..5原型编号。");return result;}
  public static List<double> Factors(double lo,double hi,int samples){
   MathRules.Require(double.IsFinite(lo)&&double.IsFinite(hi)&&lo>0&&hi>=lo&&samples>=2&&samples<=9,"Hmin/Hmax或Samples无效。");
   var r=new List<double>();for(int i=0;i<samples;i++){double f=lo+(hi-lo)*i/(samples-1);if(r.All(x=>Math.Abs(x-f)>1e-9))r.Add(f);}
   if(lo<=1&&hi>=1&&r.All(x=>Math.Abs(x-1)>1e-9))r.Add(1);return r.OrderBy(x=>x).ToList();
  }
  public static bool AreaAllowed(double candidate,double baseline,double tolerance)=>double.IsFinite(candidate)&&double.IsFinite(baseline)&&candidate>0&&baseline>0&&double.IsFinite(tolerance)&&tolerance>=0&&Math.Abs(candidate/baseline-1)<=tolerance+1e-9;
  public static CompareMetric Best(IEnumerable<CompareMetric> values)=>values.Where(m=>m.Eligible&&m.Pass).OrderBy(m=>m.Pass?0:1).ThenBy(m=>m.Pass?m.Mass:m.Score).ThenBy(m=>m.Pass?m.Score:m.Mass).ThenBy(m=>m.Id,StringComparer.Ordinal).FirstOrDefault();
  public static CompareMetric Worst(IEnumerable<CompareMetric> values)=>values.Where(m=>m.Eligible).OrderByDescending(m=>m.Score).ThenByDescending(m=>m.Mass).ThenBy(m=>m.Id,StringComparer.Ordinal).FirstOrDefault();
  public static List<CompareMetric> Rank(IEnumerable<CompareMetric> values)=>values.Where(m=>m.Eligible).OrderBy(m=>m.Pass?0:1).ThenBy(m=>m.Pass?m.Mass:m.Score).ThenBy(m=>m.Pass?m.Score:m.Mass).ThenBy(m=>m.Id,StringComparer.Ordinal).ToList();
 }
}
