using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using BambooStages123;

namespace BambooCompare {
 public sealed class CandidateSeed {
  public Spec Spec; public double Factor,Diameter;
  public string Id=>CandidateSpace.Key(Spec,Factor,Diameter);
 }
 // Only dimensions that belong to a type participate in its Cartesian product.
 // The first value of each list defines the separately reported baseline.
 public sealed class CandidateSpace {
  public List<double> CrossAngles,Depths,Panels,PointEs,PointSlopes,PointFootGaps,PointPanels;
  static string F(double x)=>x.ToString("R",CultureInfo.InvariantCulture);
  public static List<double> Values(IEnumerable<double> source,string name,double min,double max,bool integer=false,bool even=false){
   var values=(source??Enumerable.Empty<double>()).Distinct().ToList();
   MathRules.Require(values.Count>0&&values.Count<=12,name+"须填入1—12个不同候选值；单值固定，多值搜索。");
   MathRules.Require(values.All(x=>double.IsFinite(x)&&x>=min&&x<=max&&(!integer||x==Math.Truncate(x))&&(!even||x%2==0)),name+"超出允许范围"+(even?"，且必须为偶数":integer?"，且必须为整数":"")+"。");
   return values;
  }
  public static CandidateSpace Create(IEnumerable<double> cross,IEnumerable<double> depth,IEnumerable<double> panels,IEnumerable<double> pointE,IEnumerable<double> slope,IEnumerable<double> footGap,IEnumerable<double> pointPanels){
   return new CandidateSpace{
    CrossAngles=Values(cross,"CrossAngles",5,45),Depths=Values(depth,"TrussDepths",100,1500),Panels=Values(panels,"PanelCounts",4,24,true,true),
    PointEs=Values(pointE,"PointEs",50,2000),PointSlopes=Values(slope,"PointSlopes",.35,.95),PointFootGaps=Values(footGap,"PointFootGaps",0,500),PointPanels=Values(pointPanels,"PointPanelCounts",6,12,true)
   };
  }
  public Spec Baseline(Spec source){
   var s=source.Copy();s.CrossAngle=CrossAngles[0];s.Depth=Depths[0];s.Panels=(int)Panels[0];s.PointE=PointEs[0];s.PointSlope=PointSlopes[0];s.PointFootGap=PointFootGaps[0];s.PointPanels=(int)PointPanels[0];return s;
  }
  public long Variants(int type){
   switch(type){case 0:case 1:return 1;case 2:return CrossAngles.Count;case 4:return (long)Depths.Count*Panels.Count;case 5:return (long)PointEs.Count*PointSlopes.Count*PointFootGaps.Count*PointPanels.Count;default:throw new ArgumentException("本版只比较0/1/2/4/5五型。");}
  }
  public long Count(IEnumerable<int> types,int factors,int diameters)=>types.Sum(t=>Variants(t)*factors*diameters);
  public int CheckBudget(IEnumerable<int> types,int factors,int diameters,int maximum){
   MathRules.Require(maximum>=1&&maximum<=2000,"MaxCandidates须为1—2000的整数。");long count=Count(types,factors,diameters);
   MathRules.Require(count<=maximum,"本次有"+count+"个候选组合，超过MaxCandidates="+maximum+"；减少候选值或提高预算（上限2000）。不会截断搜索。");return (int)count;
  }
  public IEnumerable<Spec> Specs(Spec source,int type){
   var b=ComparisonRules.WithType(Baseline(source),type);
   if(type==0||type==1){yield return b;yield break;}
   if(type==2){foreach(double a in CrossAngles){var s=b.Copy();s.CrossAngle=a;yield return s;}yield break;}
   if(type==4){foreach(double d in Depths)foreach(double n in Panels){var s=b.Copy();s.Depth=d;s.Panels=(int)n;yield return s;}yield break;}
   if(type==5){foreach(double e in PointEs)foreach(double slope in PointSlopes)foreach(double gap in PointFootGaps)foreach(double n in PointPanels){var s=b.Copy();s.PointE=e;s.PointSlope=slope;s.PointFootGap=gap;s.PointPanels=(int)n;yield return s;}yield break;}
   throw new ArgumentException("不支持的原型编号。");
  }
  public IEnumerable<CandidateSeed> Seeds(Spec source,IEnumerable<int> types,IEnumerable<double> factors,IEnumerable<double> diameters){
   foreach(int t in types)foreach(var s in Specs(source,t))foreach(double f in factors)foreach(double d in diameters)yield return new CandidateSeed{Spec=s.Copy(),Factor=f,Diameter=d};
  }
  public string Fingerprint()=>string.Join("|",new[]{CrossAngles,Depths,Panels,PointEs,PointSlopes,PointFootGaps,PointPanels}.Select(a=>string.Join(",",a.Select(F))));
  public string Plan(IEnumerable<int> types,int factors,int diameters)=>string.Join("\n",types.Select(t=>ComparisonRules.Names[t]+"："+factors+"高度×"+diameters+"外径×"+Variants(t)+"构造组合＝"+(Variants(t)*factors*diameters)))+"\n总计 "+Count(types,factors,diameters)+" 个候选组合；不含候选集合之外的基准计算。\n各类型参数首值用于同模数基准；基准结果不淘汰任何类型。";
  public static string Key(Spec s,double factor,double diameter){
   int t=ComparisonRules.Code(s);string id=t+"_H"+F(factor)+"_D"+F(diameter);
   if(t==2)id+="_CA"+F(s.CrossAngle);
   if(t==4)id+="_TD"+F(s.Depth)+"_N"+s.Panels;
   if(t==5)id+="_E"+F(s.PointE)+"_SL"+F(s.PointSlope)+"_FG"+F(s.PointFootGap)+"_PN"+s.PointPanels;
   return id;
  }
  public static string Parameters(Spec s){
   int t=ComparisonRules.Code(s);
   if(t==2)return "交叉倾角="+F(s.CrossAngle)+"°";
   if(t==4)return "上下弦高差="+F(s.Depth)+"mm，分格="+s.Panels;
   if(t==5)return "外挑="+F(s.PointE)+"mm，坡度="+F(s.PointSlope)+"，拱脚净距="+F(s.PointFootGap)+"mm，每侧分格="+s.PointPanels;
   return "无本型附加搜索参数";
  }
 }
}
