using System;
using System.Linq;
using System.Collections.Generic;

namespace BambooStages123 {
 public readonly struct P3 {
  public readonly double X,Y,Z;
  public P3(double x,double y,double z){X=x;Y=y;Z=z;}
  public static P3 operator +(P3 a,P3 b)=>new P3(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
  public static P3 operator -(P3 a,P3 b)=>new P3(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
  public static P3 operator *(P3 a,double b)=>new P3(a.X*b,a.Y*b,a.Z*b);
  public double Length=>Math.Sqrt(X*X+Y*Y+Z*Z);
  public double Distance(P3 b)=>(this-b).Length;
  public static double Dot(P3 a,P3 b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
  public static P3 Cross(P3 a,P3 b)=>new P3(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
 }
 public sealed class Frame3 {
  public P3 Origin,Across,Forward;
  public Frame3(P3 o,P3 a,P3 f){Origin=o;Across=a;Forward=f;}
 }
 public sealed class RibMap {
  public Profile Outer,Inner;public Frame3 Frame;public double Cos=1,Sin;
  public P3 Map(V2 v)=>Frame.Origin+Frame.Across*v.X+Frame.Forward*(v.Z*Sin)+new P3(0,0,v.Z*Cos);
  public P3 At(double u,bool inner=false)=>Map((inner?Inner:Outer).Upper(u));
 }
 public sealed class MemberCurve {
  public string Id,Role;public int Frame;public P3 A,B;public RibMap Rib;public bool Inner;public double U0,U1;
  public Circle2 Circle;public RibMap CircleMap;public bool Mirror,EndAtCrown,StartAtCrown;
  public bool Arc=>Circle!=null||Rib!=null;
  public P3 At(double t){if(Circle!=null){var p=Circle.At(U0+(U1-U0)*t);return CircleMap.Map(new V2(Mirror?-p.X:p.X,p.Z));}return Rib!=null?Rib.At(U0+(U1-U0)*t,Inner):A+(B-A)*t;}
  public bool CrownEnd=>EndAtCrown||(Rib!=null&&Math.Abs(U1-.5)<1e-10);
 }
 public sealed class RoofTriangle {
  public P3 A,B,C;public bool Left;public string LoadRailA,LoadRailB;public P3 AreaVector=>P3.Cross((B-A)*.001,(C-A)*.001)*.5;
 }
 public sealed class StructurePlan {
  public Spec Spec;public List<RibMap> Maps=new List<RibMap>();public List<MemberCurve> Curves=new List<MemberCurve>();
  public List<List<P3>> Rails=new List<List<P3>>();public List<P3> Feet=new List<P3>(),CrossNodes=new List<P3>();
  public List<RoofTriangle> Roof=new List<RoofTriangle>();public List<List<P3>> RoofGrid=new List<List<P3>>();
  public List<double> Heights,RoofU;public double MaxRailSpacing;public int RailCount;
 }
 public static class StructureRules {
  public static string Name(Spec s)=>(s.Structure==0?"单肋":s.Structure==1?"侧部交叉":"桁架式")+(s.Shape==0?"尖拱":"圆拱");
  public static Profile PhysicalProfile(Spec s,double tol){
   var p=s.Copy();if(!s.CompoundPointed)p.Structure=0;
   if(s.Structure==1){double c=Math.Cos(s.CrossAngle*Math.PI/180);p.H/=c;p.LegH/=c;p.Angle=Math.Atan(Math.Tan(s.Angle*Math.PI/180)*c)*180/Math.PI;}
   return new Profile(p,tol);
  }
  public static void Validate(Spec s,double tol){
   MathRules.Require(s.Structure>=0&&s.Structure<=2,"Structure=0单肋、1交叉、2桁架。");new Profile(s,tol);
   if(s.Structure==1){MathRules.Require(double.IsFinite(s.CrossAngle)&&s.CrossAngle>0&&s.CrossAngle<=45,"CrossAngle为相对竖直拱面的纵向倾角，须在(0,45]度。");
    try{PhysicalProfile(s,tol);}catch(Exception ex){throw new ArgumentException("交叉拱须保持倾斜面内真实圆弧。请降低H或倾角："+ex.Message);}}
   if(s.Structure==2&&!s.CompoundPointed){MathRules.Require(MathRules.Positive(s.Depth)&&s.Depth>10*tol&&s.Panels>=4&&s.Panels<=32&&s.Panels%2==0,"TrussDepth须为正；Panels为4..32偶数。");
    var inner=s.Copy();inner.H-=s.Depth;try{new Profile(inner,tol);}catch(Exception ex){throw new ArgumentException("下弦无法保持所选拱形，请减小TrussDepth或增加H："+ex.Message);}}
  }
  static void BuildPointed(StructurePlan r,RibMap rib,int frame,List<double> cuts,double tol){
   var s=rib.Outer.Spec;int n=s.PointPanels;var boundary=s.PointedVersion>=2?rib.Outer.PointedOuterNodes():null;var wingLinks=rib.Outer.PointedWingLinks();
   for(int k=0;k<cuts.Count-1;k++)r.Curves.Add(new MemberCurve{Id=$"A{frame}_{k}",Role="Outer",Frame=frame,A=rib.At(cuts[k]),B=rib.At(cuts[k+1]),EndAtCrown=Math.Abs(cuts[k+1]-.5)<1e-9,StartAtCrown=Math.Abs(cuts[k]-.5)<1e-9});
   for(int side=0;side<2;side++){
    bool mirror=side==1;Func<V2,P3> map=p=>rib.Map(new V2(mirror?-p.X:p.X,p.Z));
    Func<int,P3> lower=j=>map(rib.Outer.Lower.At(j/(double)n));
    Func<int,P3> upper=j=>{if(s.PointedVersion>=2)return map(boundary[j]);var p=rib.Outer.Lower.At(j/(double)n);double x=s.PointedVersion>=2&&j==0?-s.W/2:p.X;return map(new V2(x,s.H+rib.Outer.Rise/rib.Outer.Half*x));};
      var circle=rib.Outer.Lower;var center=map(new V2(circle.X,circle.Z));Func<P3,P3,bool> clear=(v,w)=>{var d=w-v;double t=Math.Max(0,Math.Min(1,P3.Dot(center-v,d)/P3.Dot(d,d)));return (v+d*t-center).Length>=circle.R-tol;};
    var returnCuts=new[]{0.0,1.0}.Concat(wingLinks.Select(l=>l.T)).Distinct().OrderBy(t=>t).ToArray();
    for(int j=0;j<returnCuts.Length-1;j++)r.Curves.Add(new MemberCurve{Id=$"R{frame}_{side}_{j}",Role="Return",Frame=frame,Circle=rib.Outer.Return,CircleMap=rib,Mirror=mirror,U0=returnCuts[j],U1=returnCuts[j+1]});
    for(int j=0;j<wingLinks.Count;j++)r.Curves.Add(new MemberCurve{Id=$"W{frame}_{side}_{j}",Role="Web",Frame=frame,A=map(wingLinks[j].Inner),B=map(wingLinks[j].Outer)});
    if(s.PointedVersion>=2)for(int j=0;j<n&&Math.Abs(boundary[j+1].X+s.W/2)<tol;j++)r.Curves.Add(new MemberCurve{Id=$"C{frame}_{side}_{j}",Role="Column",Frame=frame,A=upper(j),B=upper(j+1)});
    for(int j=0;j<n;j++){
     r.Curves.Add(new MemberCurve{Id=$"I{frame}_{side}_{j}",Role="Inner",Frame=frame,Circle=rib.Outer.Lower,CircleMap=rib,Mirror=mirror,U0=j/(double)n,U1=(j+1)/(double)n,EndAtCrown=j==n-1});
     if(s.PointedVersion>=2&&j==0)continue;
     if(s.PointedVersion>=2)MathRules.Require(clear(lower(j),upper(j)),"当前分格连杆穿过下弦，请调整分格或上弦坡度。");
     r.Curves.Add(new MemberCurve{Id=$"V{frame}_{side}_{j}",Role="Web",Frame=frame,A=s.PointedVersion>=2&&j==0?map(new V2(-s.W/2,0)):lower(j),B=upper(j)});
     if(j<n-1){bool top=(j+(s.PointedVersion>=2?n:0))%2==0;P3 a=top?upper(j):lower(j),b=top?lower(j+1):upper(j+1);if(s.PointedVersion>=2){
      if(!clear(a,b)){top=!top;a=top?upper(j):lower(j);b=top?lower(j+1):upper(j+1);}MathRules.Require(clear(a,b),"当前分格斜杆穿过下弦，请调整分格或上弦坡度。");
     }r.Curves.Add(new MemberCurve{Id=$"D{frame}_{side}_{j}",Role="Web",Frame=frame,A=a,B=b});}
    }
   }
  }
  static List<double> Unique(IEnumerable<double> values)=>values.OrderBy(x=>x).Aggregate(new List<double>(),(r,x)=>{if(r.Count==0||Math.Abs(x-r.Last())>1e-9)r.Add(x);return r;});
  public static StructurePlan Build(Spec spec,List<Frame3> frames,List<double> heights,int railCount,double tol,bool requireCross=true){
   MathRules.Require(frames.Count==heights.Count&&frames.Count>=1&&railCount>=3&&railCount<=201&&railCount%2==1,"站点/高度/檩条道数不匹配。");
   var r=new StructurePlan{Spec=spec.Copy(),Heights=heights.ToList(),RailCount=railCount};var railU=Enumerable.Range(0,railCount).Select(j=>j/(double)(railCount-1)).ToList();
   var cuts=new List<double>(railU);cuts.Add(.5);var panelU=Enumerable.Range(0,spec.Panels+1).Select(j=>j/(double)spec.Panels).ToList();if(spec.Structure==2&&!spec.CompoundPointed)cuts.AddRange(panelU);
   for(int i=0;i<frames.Count;i++){
    var s=spec.Copy();s.H=heights[i];Validate(s,tol);var rib=new RibMap{Outer=PhysicalProfile(s,tol),Frame=frames[i]};
    if(spec.Structure==1){double t=s.CrossAngle*Math.PI/180;rib.Cos=Math.Cos(t);rib.Sin=(i%2==0?1:-1)*Math.Sin(t);}
    if(s.CompoundPointed){for(int j=0;j<=s.PointPanels;j++){var p=rib.Outer.Lower.At(j/(double)s.PointPanels);double x=s.PointedVersion>=2&&j==0?-s.W/2:p.X;double u=s.PointedVersion>=2?(rib.Outer.PointedOuterNodes()[j].X/rib.Outer.Half+1)/2:(x/rib.Outer.Half+1)/2;cuts.Add(u);cuts.Add(1-u);}}
    if(spec.Structure==2&&!s.CompoundPointed){var inner=s.Copy();inner.H-=s.Depth;inner.Structure=0;rib.Inner=new Profile(inner,tol);}
    r.Maps.Add(rib);if(s.CompoundPointed&&s.PointedVersion>=2){foreach(int side in new[]{-1,1})foreach(double offset in new[]{-rib.Outer.FootOffset,0.0,rib.Outer.FootOffset})r.Feet.Add(rib.Map(new V2(side*(s.W/2+offset),0)));}else{r.Feet.Add(rib.Map(rib.Outer.LeftFoot));r.Feet.Add(rib.Map(rib.Outer.RightFoot));}
   }
   if(spec.Structure==1){
    var f=frames[0];MathRules.Require(frames.Count>=2,"交叉原型至少需要两榀。");
    for(int i=0;i<frames.Count;i++){
     MathRules.Require(Math.Abs(heights[i]-heights[0])<=tol,"本版侧部交叉拱限定等高；请用HeightMode=0。单肋和桁架仍支持变高。");
     MathRules.Require(frames[i].Across.Distance(f.Across)<1e-8&&frames[i].Forward.Distance(f.Forward)<1e-8&&Math.Abs(P3.Dot(frames[i].Origin-f.Origin,f.Across))<=tol,"本版侧部交叉拱限定直轴；单肋和桁架仍支持光滑曲轴。");
    }
    for(int i=0;i<frames.Count;i++)for(int j=i+1;j<frames.Count;j++){
     double slope=(i%2==0?1:-1)-(j%2==0?1:-1);if(slope<=0)continue;
     double z=P3.Dot(frames[j].Origin-frames[i].Origin,f.Forward)/(slope*Math.Tan(spec.CrossAngle*Math.PI/180));
     if(z<=spec.LegH+tol||z>=heights[0]-tol)continue;
     double a=0,b=.5;for(int k=0;k<64;k++){double u=(a+b)/2;if(r.Maps[i].Outer.Upper(u).Z*r.Maps[i].Cos<z)a=u;else b=u;}
     foreach(double u in new[]{(a+b)/2,1-(a+b)/2}){
      var one=r.Maps[i].At(u);var two=r.Maps[j].At(u);MathRules.Require(one.Distance(two)<=tol,"交叉点三维位置不一致，不能作为连接节点。");
      cuts.Add(u);if(r.CrossNodes.All(p=>p.Distance(one)>tol))r.CrossNodes.Add(one);
     }
    }
    MathRules.Require(!requireCross||r.CrossNodes.Count>0,"拱身尚未真实相交：请增大CrossAngle、减小S或增加H；交点必须在LegH与拱顶之间。未把投影重叠当节点。");
   }
   cuts=Unique(cuts);r.RoofU=cuts;
   for(int i=0;i<r.Maps.Count;i++){
    var rib=r.Maps[i];
    if(spec.CompoundPointed){BuildPointed(r,rib,i,cuts,tol);continue;}
    for(int k=0;k<cuts.Count-1;k++)r.Curves.Add(new MemberCurve{Id=$"A{i}_{k}",Role="Outer",Frame=i,Rib=rib,U0=cuts[k],U1=cuts[k+1]});
    if(spec.Foot!=0){r.Curves.Add(new MemberCurve{Id=$"L{i}_0",Role="Leg",Frame=i,A=r.Feet[2*i],B=rib.At(0)});r.Curves.Add(new MemberCurve{Id=$"L{i}_1",Role="Leg",Frame=i,A=rib.At(1),B=r.Feet[2*i+1]});}
    if(spec.Structure==2){
     for(int k=0;k<spec.Panels;k++)r.Curves.Add(new MemberCurve{Id=$"I{i}_{k}",Role="Inner",Frame=i,Rib=rib,Inner=true,U0=panelU[k],U1=panelU[k+1]});
     for(int k=1;k<spec.Panels;k++)r.Curves.Add(new MemberCurve{Id=$"V{i}_{k}",Role="Web",Frame=i,A=rib.At(panelU[k]),B=rib.At(panelU[k],true)});
     // Shared end nodes already form end triangles; avoid duplicate diagonals along end chords.
     for(int k=1;k<spec.Panels-1;k++)r.Curves.Add(new MemberCurve{Id=$"D{i}_{k}",Role="Web",Frame=i,A=rib.At(panelU[k],k%2!=0),B=rib.At(panelU[k+1],k%2==0)});
    }
   }
   Func<double,List<P3>> rowAt=u=>{
    var points=r.Maps.Select(m=>m.At(u)).ToList();if(spec.Structure==1)points=points.OrderBy(p=>P3.Dot(p-frames[0].Origin,frames[0].Forward)).ToList();return points;
   };
   var railIds=new string[railU.Count,Math.Max(0,frames.Count-1)];
   for(int j=0;j<railU.Count;j++){
    var points=rowAt(railU[j]);var clean=new List<P3>();foreach(var p in points)if(clean.Count==0||p.Distance(clean.Last())>tol)clean.Add(p);
    if(clean.Count>1){r.Rails.Add(clean);for(int i=0;i<clean.Count-1;i++)r.Curves.Add(new MemberCurve{Id=$"P{j}_{i}",Role="Purlin",Frame=-1,A=clean[i],B=clean[i+1]});}
    for(int i=0;i<points.Count-1;i++)if(points[i].Distance(points[i+1])>tol){
     int segment=clean.FindIndex(p=>p.Distance(points[i])<=tol);MathRules.Require(segment>=0&&segment<clean.Count-1,"檩条分跨索引无效。");railIds[j,i]=$"P{j}_{segment}";
    }
   }
   var columns=cuts.Select(rowAt).ToList();for(int i=0;i<frames.Count;i++)r.RoofGrid.Add(columns.Select(c=>c[i]).ToList());
   for(int i=0;i<frames.Count-1;i++)for(int j=0;j<cuts.Count-1;j++){
    var a=columns[j][i];var b=columns[j+1][i];var c=columns[j+1][i+1];var d=columns[j][i+1];
    foreach(var tri in new[]{new RoofTriangle{A=a,B=b,C=c,Left=cuts[j+1]<=.5+1e-9},new RoofTriangle{A=a,B=c,C=d,Left=cuts[j+1]<=.5+1e-9}}){
     if(tri.AreaVector.Length<=1e-10)continue;
     MathRules.Require(tri.AreaVector.Z>1e-10,"屋面水平投影翻折或近似竖直；请减小轴线曲率/跨度或检查布置。");
     int rail=(int)Math.Floor((cuts[j]+cuts[j+1])*.5*(railCount-1));
     tri.LoadRailA=railIds[rail,i];tri.LoadRailB=railIds[rail+1,i];
     MathRules.Require(tri.LoadRailA!=null||tri.LoadRailB!=null,"屋面条带没有有效的承载檩条；请调整檩条间距或交叉角度。");r.Roof.Add(tri);
    }
   }
   r.MaxRailSpacing=r.Maps.Max(m=>m.Outer.HalfLength)*2/(railCount-1);
   MathRules.Require(r.Curves.Count<=4000,"原型杆段超过4000，请减少分格、檩条或榀数。");
   foreach(var m in r.Curves)MathRules.Require(m.At(0).Distance(m.At(1))>tol,"出现零长度杆件："+m.Id);
   return r;
  }
 }
}
