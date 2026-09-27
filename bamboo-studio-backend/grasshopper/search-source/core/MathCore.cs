using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace BambooStages123
{
 public readonly struct V2
 {
  public readonly double X,Z;
  public V2(double x,double z){X=x;Z=z;}
  public double Distance(V2 b)=>Math.Sqrt((X-b.X)*(X-b.X)+(Z-b.Z)*(Z-b.Z));
 }
 public sealed class Spec
 {
  public int Shape,Foot,Units,Structure,Panels=8;
  public double W,H,LegH,Angle,CrossAngle=25,Depth=450;
  public int PointedVersion,PointPanels=6;
  public double PointFootGap=10,PointSlope=.9,PointLowerD=120,PointWebD=60;
  public double PointE=400,PointTip=1800,PointDepth=450;
  public bool CompoundPointed=>Structure==2&&Shape==0&&PointedVersion>=1;
  public Spec Copy()=>(Spec)MemberwiseClone();
  public string Encode()=>string.Join("|",new[]{"B123U","4",Shape.ToString(),Foot.ToString(),W.ToString("R",CultureInfo.InvariantCulture),H.ToString("R",CultureInfo.InvariantCulture),LegH.ToString("R",CultureInfo.InvariantCulture),Angle.ToString("R",CultureInfo.InvariantCulture),Units.ToString(),Structure.ToString(),CrossAngle.ToString("R",CultureInfo.InvariantCulture),Depth.ToString("R",CultureInfo.InvariantCulture),Panels.ToString(),PointedVersion.ToString(),PointE.ToString("R",CultureInfo.InvariantCulture),PointTip.ToString("R",CultureInfo.InvariantCulture),PointDepth.ToString("R",CultureInfo.InvariantCulture),PointPanels.ToString(),PointFootGap.ToString("R",CultureInfo.InvariantCulture),PointSlope.ToString("R",CultureInfo.InvariantCulture),PointLowerD.ToString("R",CultureInfo.InvariantCulture),PointWebD.ToString("R",CultureInfo.InvariantCulture)});
  public static Spec Decode(string text)
  {
   var s=(text??"").Split('|');MathRules.Require(s.Length>=2&&s[0]=="B123U"&&((s[1]=="1"&&s.Length==9)||(s[1]=="2"&&s.Length==13)||(s[1]=="3"&&s.Length==18)||(s[1]=="4"&&s.Length==22)),"UnitSpec 无效，请连接本版02的 UnitSpec 输出。");
   var result=new Spec{Shape=int.Parse(s[2]),Foot=int.Parse(s[3]),W=double.Parse(s[4],CultureInfo.InvariantCulture),H=double.Parse(s[5],CultureInfo.InvariantCulture),LegH=double.Parse(s[6],CultureInfo.InvariantCulture),Angle=double.Parse(s[7],CultureInfo.InvariantCulture),Units=int.Parse(s[8])};
   if(s[1]=="2"||s[1]=="3"||s[1]=="4"){result.Structure=int.Parse(s[9]);result.CrossAngle=double.Parse(s[10],CultureInfo.InvariantCulture);result.Depth=double.Parse(s[11],CultureInfo.InvariantCulture);result.Panels=int.Parse(s[12]);}if(s[1]=="3"||s[1]=="4"){result.PointedVersion=int.Parse(s[13]);result.PointE=double.Parse(s[14],CultureInfo.InvariantCulture);result.PointTip=double.Parse(s[15],CultureInfo.InvariantCulture);result.PointDepth=double.Parse(s[16],CultureInfo.InvariantCulture);result.PointPanels=int.Parse(s[17]);}if(s[1]=="4"){result.PointFootGap=double.Parse(s[18],CultureInfo.InvariantCulture);result.PointSlope=double.Parse(s[19],CultureInfo.InvariantCulture);result.PointLowerD=double.Parse(s[20],CultureInfo.InvariantCulture);result.PointWebD=double.Parse(s[21],CultureInfo.InvariantCulture);}return result;
  }
 }
 public sealed class Circle2 {
  public readonly V2 A,B;public readonly double X,Z,R,Start,Sweep;
  static double Positive(double a){while(a<0)a+=2*Math.PI;while(a>=2*Math.PI)a-=2*Math.PI;return a;}
  public Circle2(V2 a,V2 m,V2 b){
   A=a;B=b;double d=2*(a.X*(m.Z-b.Z)+m.X*(b.Z-a.Z)+b.X*(a.Z-m.Z));
   MathRules.Require(Math.Abs(d)>1e-6,"下弦圆弧退化，请调整肩部弦间距。");
   double aa=a.X*a.X+a.Z*a.Z,mm=m.X*m.X+m.Z*m.Z,bb=b.X*b.X+b.Z*b.Z;
   X=(aa*(m.Z-b.Z)+mm*(b.Z-a.Z)+bb*(a.Z-m.Z))/d;
   Z=(aa*(b.X-m.X)+mm*(a.X-b.X)+bb*(m.X-a.X))/d;
   R=a.Distance(new V2(X,Z));Start=Math.Atan2(a.Z-Z,a.X-X);
   double end=Positive(Math.Atan2(b.Z-Z,b.X-X)-Start),mid=Positive(Math.Atan2(m.Z-Z,m.X-X)-Start);
   Sweep=mid<=end?end:end-2*Math.PI;
  }
  public V2 At(double t){if(t==0)return A;if(t==1)return B;double a=Start+Sweep*t;return new V2(X+R*Math.Cos(a),Z+R*Math.Sin(a));}
 }
 public sealed class Profile
 {
  public readonly Spec Spec;public readonly Circle2 Lower,Return;public readonly double FootOffset;
  public readonly double Half, Rise, Radius, CenterX, CenterZ, StartAngle, Sweep, HalfLength, KneeAngle;
  public Profile(Spec input,double tol)
  {
   Spec=input.Copy();
   if(Spec.CompoundPointed&&Spec.PointedVersion>=2){
    MathRules.Require(Spec.W>0&&Spec.H>0&&Spec.PointLowerD>0&&Spec.PointWebD>0&&Spec.PointFootGap>=0&&Spec.PointFootGap<=500,"拱脚尺寸无效。");
    FootOffset=Spec.PointLowerD+Spec.PointFootGap;double a=Spec.W/2-FootOffset;
    MathRules.Require(a>100&&Spec.PointE>FootOffset,"拱脚净间距或杆径过大：请增加跨度、外挑，或减小间距。");
    MathRules.Require(Spec.PointSlope>=.35&&Spec.PointSlope<=.95&&Spec.PointPanels>=3&&Spec.PointPanels<=12,"上弦坡度系数须为0.35—0.95；每侧分格须为3—12。");
    Half=Spec.W/2+Spec.PointE;
    double cx=Math.Max(.25*a,(Spec.H*Spec.H-a*a)/(2*a)),cz=(Spec.H*Spec.H-a*a-2*a*cx)/(2*Spec.H),rad=Math.Sqrt((a+cx)*(a+cx)+cz*cz);
    double start=(Math.Atan2(-cz,-a-cx)+2*Math.PI)%(2*Math.PI),end=Math.Atan2(Spec.H-cz,-cx),ang=(start+end)/2;
    Lower=new Circle2(new V2(-a,0),new V2(cx+rad*Math.Cos(ang),cz+rad*Math.Sin(ang)),new V2(0,Spec.H));
    Rise=Spec.PointSlope*cx/(Spec.H-cz)*Half;Spec.PointTip=Spec.H-Rise;HalfLength=Math.Sqrt(Half*Half+Rise*Rise);
    double outerFoot=-Spec.W/2-FootOffset,run=Spec.PointE-FootOffset;
    MathRules.Require(Spec.PointTip>run+tol,"外挑过长或拱高过低，回接弧无法保持单调；请缩短外挑或提高拱高。");
    double rr=(run*run+Spec.PointTip*Spec.PointTip)/(2*run),rc=outerFoot-rr,theta=Math.Atan2(Spec.PointTip,rr-run);
    Return=new Circle2(new V2(-Half,Spec.PointTip),new V2(rc+rr*Math.Cos(theta/2),rr*Math.Sin(theta/2)),new V2(outerFoot,0));return;
   }
   if(Spec.CompoundPointed){
    MathRules.Require(Spec.W>100*tol&&Spec.H>100*tol&&Spec.PointE>10*tol&&Spec.PointTip>10*tol&&Spec.PointTip<Spec.H-10*tol&&Spec.PointDepth>10*tol,"复合尖拱要求外挑、挑端高度和肩部间距为正，且挑端低于拱顶。");
    MathRules.Require(Spec.PointPanels>=3&&Spec.PointPanels<=12,"复合尖拱每侧分格为3—12。");
    Half=Spec.W/2+Spec.PointE;Rise=Spec.H-Spec.PointTip;HalfLength=Math.Sqrt(Half*Half+Rise*Rise);
    var foot=new V2(-Spec.W/2,0);var tip=new V2(-Half,Spec.PointTip);
    double shoulderX=-Spec.W/4,shoulderZ=Spec.H+Rise/Half*shoulderX-Spec.PointDepth;
    MathRules.Require(shoulderZ>Spec.H/2+tol,"肩部间距过大，下弦已不再向上拱起，请减小肩部间距。");
    Lower=new Circle2(foot,new V2(shoulderX,shoulderZ),new V2(0,Spec.H));
    Return=new Circle2(tip,new V2((tip.X+foot.X)/2+.2*Spec.PointE,Spec.PointTip/2),foot);
    var previous=Lower.At(0);
    for(int i=1;i<=200;i++){var p=Lower.At(i/200.0);MathRules.Require(p.X>previous.X&&p.Z>previous.Z&&p.X<=tol&&p.Z<=Spec.H+tol&&(i==200||p.Z<Spec.H+Rise/Half*p.X-tol),"肩部弦间距使下弦回折或穿过上弦，请调整挑端高度或肩部间距。");previous=p;}
    for(int i=1;i<200;i++){var p=Return.At(i/200.0);MathRules.Require(p.X>=-Half-tol&&p.X<=foot.X+tol&&p.Z>=-tol&&p.Z<=Spec.PointTip+tol,"挑端回接弧超出边界，请减小外挑或提高挑端。");}
    return;
   }
   MathRules.CheckTypes(Spec.Shape,Spec.Foot);
   MathRules.Require(MathRules.Positive(tol),"容差必须为正数。");
   MathRules.Require(MathRules.Positive(Spec.W)&&MathRules.Positive(Spec.H)&&Spec.W>100*tol&&Spec.H>100*tol,"W/H 必须是相对文档容差足够大的有限正数。");
   MathRules.Require(double.IsFinite(Spec.LegH)&&double.IsFinite(Spec.Angle),"LegH/LegAngle 必须为有限数。");
   if(Spec.Foot==0){Spec.LegH=0;Spec.Angle=0;}
   else
   {
    MathRules.Require(Spec.LegH>10*tol&&Spec.LegH<Spec.H-10*tol,"带下部段时要求 0 < LegH < H。");
    if(Spec.Foot==1)Spec.Angle=0;
    MathRules.Require(Spec.Angle>=0&&Spec.Angle<=60,"LegAngle 范围为0—60度，正值表示两侧向内倾斜。");
   }
   Half=Spec.W/2-Spec.LegH*Math.Tan(Spec.Angle*Math.PI/180);
   Rise=Spec.H-Spec.LegH;
   MathRules.Require(Half>50*tol,"下部倾角或高度过大，两侧过渡点已接近或越过中心线。");
   if(Spec.Shape==0)
   {
    // Two actual circular arcs meeting at a pointed crown, also defined for shallow rises.
    // The springing tangent is allowed to be inclined; no ellipse or scaled circle.
    CenterX=Math.Max(Half,(Rise*Rise-Half*Half)/(2*Half));
    CenterZ=Spec.LegH+(Rise*Rise-Half*Half-2*Half*CenterX)/(2*Rise);
    Radius=Math.Sqrt(Math.Pow(-Half-CenterX,2)+Math.Pow(Spec.LegH-CenterZ,2));
    StartAngle=Math.Atan2(Spec.LegH-CenterZ,-Half-CenterX);
    Sweep=Math.Atan2(Spec.H-CenterZ,-CenterX)-StartAngle;
   }
   else
   {
    MathRules.Require(Rise<=Half+tol,"基础圆拱要求 0 < H-LegH <= 上部起拱宽度/2；不自动把圆拱拉伸成椭圆。");
    // Tiny tolerance excess is rejected too: never change user's H silently.
    MathRules.Require(Rise<=Half,"H 略高于圆拱的半圆上限，请减小H。");
    Radius=(Half*Half+Rise*Rise)/(2*Rise);CenterX=0;CenterZ=Spec.LegH+(Rise*Rise-Half*Half)/(2*Rise);
    StartAngle=Math.Atan2(Spec.LegH-CenterZ,-Half);Sweep=Math.PI/2-StartAngle;
   }
   HalfLength=Radius*Math.Abs(Sweep);
   double tangentAngle=StartAngle-Math.PI/2;
   double legAngle=Math.PI/2-Spec.Angle*Math.PI/180;
   KneeAngle=Spec.Foot==0?0:Math.Abs(tangentAngle-legAngle)*180/Math.PI;
   MathRules.Require(double.IsFinite(Radius)&&double.IsFinite(HalfLength)&&HalfLength>tol,"圆弧数值退化，请调整尺寸。");
  }
  public V2 LeftFoot=>new V2(-Spec.W/2,0);
  public V2 RightFoot=>new V2(Spec.W/2,0);
  public List<(V2 Inner,V2 Outer,double T)> PointedWingLinks(){
   var links=new List<(V2 Inner,V2 Outer,double T)>();if(Spec.PointedVersion<3)return links;
   double slope=Math.Tan(Math.PI/12),columnZ=Spec.H-Rise/Half*Spec.W/2;
   var nodes=PointedOuterNodes().Skip(1).Take(Spec.PointPanels-1).Where(q=>Math.Abs(q.X+Spec.W/2)<.001&&q.Z<columnZ-.001&&Return.At(0).Z-q.Z+slope*Math.Abs(Return.At(0).X-q.X)>.001).ToList();
   MathRules.Require(nodes.Count>=2,"A2外挑区需要两处直柱腹杆节点，请增加每侧分格或调整拱高／外挑。");
   int a=(int)Math.Floor((nodes.Count-1)/3.0+.5),b=(int)Math.Floor(2*(nodes.Count-1)/3.0+.5);if(a==b)b=Math.Min(nodes.Count-1,a+1);
   foreach(int index in new[]{a,b}){var inner=nodes[index];double lo=0,hi=1;for(int k=0;k<60;k++){double mid=(lo+hi)/2;var q=Return.At(mid);double value=q.Z-inner.Z+slope*Math.Abs(q.X-inner.X);if(value>0)lo=mid;else hi=mid;}double t=(lo+hi)/2;var outer=Return.At(t);
    MathRules.Require(outer.Z>0&&outer.Z<inner.Z-.001&&outer.X<inner.X-.001,"A2连接杆不能向外下斜，请调整分格或形态。");links.Add((inner,outer,t));}
   return links;
  }
  public V2[] PointedOuterNodes(){
   int n=Spec.PointPanels;double x=-Spec.W/2,z=Spec.H+Rise/Half*x,total=z+Math.Sqrt(x*x+(Spec.H-z)*(Spec.H-z)),knee=z/total;
   int k=Math.Max(1,Math.Min(n-1,(int)Math.Floor(knee*n+.5)));var nodes=new V2[n+1];
   for(int j=0;j<=n;j++){double t=j==k?knee:j/(double)n;if(j==k)nodes[j]=new V2(x,z);else if(t<knee)nodes[j]=new V2(x,t*total);else{double f=(t-knee)/(1-knee);nodes[j]=new V2(x*(1-f),z+(Spec.H-z)*f);}}
   return nodes;
  }
  public V2 LeftKnee=>new V2(-Half,Spec.CompoundPointed?Spec.PointTip:Spec.LegH);
  public V2 RightKnee=>new V2(Half,Spec.CompoundPointed?Spec.PointTip:Spec.LegH);
  public V2 Crown=>new V2(0,Spec.H);
  // u runs along equal arc length from left transition to right transition.
  public V2 Upper(double u)
  {
   MathRules.Require(double.IsFinite(u)&&u>=0&&u<=1,"上部弧长参数必须在0—1之间。");
   if(u==0)return LeftKnee;if(u==1)return RightKnee;if(u==.5)return Crown;
   if(Spec.CompoundPointed)return new V2((2*u-1)*Half,Spec.H-Math.Abs(2*u-1)*Rise);
   bool right=u>.5;double v=right?2*(1-u):2*u;
   double a=StartAngle+Sweep*v;
   var p=new V2(CenterX+Radius*Math.Cos(a),CenterZ+Radius*Math.Sin(a));
   return right?new V2(-p.X,p.Z):p;
  }
 }
 public static class MathRules
 {
  public static bool Positive(double n)=>double.IsFinite(n)&&n>0;
  public static void Require(bool condition,string error){if(!condition)throw new ArgumentException(error);}
  public static void CheckTypes(int shape,int foot)
  {Require(shape==0||shape==1,"ArchShape=0双圆弧尖拱、1圆弧拱。");Require(foot>=0&&foot<=2,"FootMode=0曲线落地、1直立下部、2倾斜下部。");}
  public static string Prototype(int shape,int foot,int structure=0){CheckTypes(shape,foot);Require(structure>=0&&structure<=2,"Structure=0单肋、1侧部交叉、2桁架。");return $"B123P|2|{shape}|{foot}|{structure}";}
  public static int[] ReadPrototype(string code)
  {var a=(code??"").Split('|');Require(a.Length>=2&&a[0]=="B123P"&&((a.Length==4&&a[1]=="1")||(a.Length==5&&a[1]=="2")),"Prototype 无效，请连接01的 Prototype 输出。");int s=int.Parse(a[2]),f=int.Parse(a[3]),t=a.Length==5?int.Parse(a[4]):0;CheckTypes(s,f);Require(t>=0&&t<=2,"Structure=0..2。");return new[]{s,f,t};}
  public static Spec Defaults(int shape,int foot,int units,double scale)
  {CheckTypes(shape,foot);double w=6000*scale,l=foot==0?0:1200*scale,a=foot==2?12:0;double b=w/2-l*Math.Tan(a*Math.PI/180);return new Spec{Shape=shape,Foot=foot,Units=units,W=w,LegH=l,Angle=a,H=l+(shape==0?1.5:.8)*b};}
  public static List<double> Stations(double length,double spacing,bool end,double tol)
  {
   Require(Positive(length)&&Positive(spacing)&&Positive(tol)&&spacing>10*tol&&length>tol,"轴线长度、间距或容差无效。");
   double raw=Math.Floor(length/spacing);Require(raw<=199,"超过200榀，请增大S或缩短轴线。");var d=new List<double>{0};
   for(int i=1;i<=raw;i++){double x=i*spacing;if(length-x<=tol)x=length;if(x-d.Last()>tol)d.Add(x);}
   if(end&&length-d.Last()>tol)d.Add(length);Require(d.Count<=200,"补末端后超过200榀。");return d;
  }
  public static int RailCount(IEnumerable<Profile> profiles,int mode,double p,int count)
  {
   Require(mode==0||mode==1,"PurlinMode=0最大间距、1指定道数。");
   if(mode==1){Require(count>=3&&count<=201&&count%2==1,"PurlinCount 必须为3—201的奇数，确保左右对称并包含拱顶一道。");return count;}
   Require(Positive(p),"P 必须为有限正数。");double n=Math.Ceiling(profiles.Max(x=>x.HalfLength)/p);Require(n<=100,"檩条超过201道，请增大P。");return 2*Math.Max(1,(int)n)+1;
  }
 }
}
