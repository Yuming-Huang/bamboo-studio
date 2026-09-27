using System;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Drawing;
using Rhino;
using Rhino.Geometry;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using BambooStages123;
public abstract class Script_Instance : GH_ScriptInstance {
#if COMPILE_CHECK
 protected IGH_Component Component=>null;
#endif

 private void RunScript(List<object> Bounds, List<object> Unit, List<object> Guides, List<object> DraftRibs, List<object> DraftPurlins, List<object> Preview, List<object> PreviewColors, List<object> PreviewDiameters, List<object> SelectedRibs, List<object> SelectedPurlins, List<object> SelectedDiameters, List<object> SelectedBases, List<object> PreviewBases, List<object> Roof, List<object> Edges, object Stage, object Solid, object Labels, object ShowRoof, ref object Geometry, ref object Colors, ref object Info) {
Geometry=null;Colors=null;Info=null;
 try {
int stage=I(Stage,0);MathRules.Require(stage>=0&&stage<=6,"Stage=0横排；1—6单步");var output=new List<GeometryBase>();var colors=new List<Color>();var boxes=Curves(Bounds);var unit=Curves(Unit);var array=Curves(Guides);var dr=Curves(DraftRibs);var dp=Curves(DraftPurlins);var ribs=Curves(SelectedRibs);var rails=Curves(SelectedPurlins);var diameters=Numbers(SelectedDiameters);var previewDiameters=Numbers(PreviewDiameters);var pre=(Preview??new List<object>()).Select(V).OfType<GeometryBase>().ToList();var preColors=(PreviewColors??new List<object>()).Select(V).OfType<Color>().ToList();var roof=(Roof??new List<object>()).Select(V).OfType<GeometryBase>().ToList();var edges=Curves(Edges);var box=BoundingBox.Empty;foreach(var c in boxes)box.Union(c.GetBoundingBox(true));double pitch=Math.Max(12000,box.Diagonal.X*1.35);string[] names={"","01 明确规模","02 场地内适配单拱","03 线性阵列","04 布置檩条 · 未确定","05 计算推荐 · 用户选型","06 生成屋面 · 仅外观"};
Action<GeometryBase,Color,Transform> add=(g,color,xf)=>{var q=g.Duplicate();q.Transform(xf);output.Add(q);colors.Add(color);};
for(int k=1;k<=6;k++){if(stage!=0&&stage!=k)continue;var tr=Transform.Translation(stage==0?(k-1)*pitch:0,0,0);var ink=Color.FromArgb(65,89,76);
 if(k>=2&&k<=4)foreach(var c in Dash(boxes))add(c,Color.FromArgb(185,195,204),tr);
 if(k==2){var section=new List<Curve>{new LineCurve(new Point3d(box.Min.X,0,0),new Point3d(box.Max.X,0,0)),new LineCurve(new Point3d(box.Min.X,0,0),new Point3d(box.Min.X,0,box.Max.Z)),new LineCurve(new Point3d(box.Max.X,0,0),new Point3d(box.Max.X,0,box.Max.Z)),new LineCurve(new Point3d(box.Min.X,0,box.Max.Z),new Point3d(box.Max.X,0,box.Max.Z))};foreach(var c in Dash(section))add(c,Color.FromArgb(140,155,175),tr);}
 if(k<=4){
  var cs=k==1?boxes:k==2?unit:k==3?array:dr.Concat(dp).ToList();
  double firstY=array.Count>0?array[0].GetBoundingBox(true).Min.Y:double.NaN;
  foreach(var c in cs){bool source=k==2||(k>=3&&Math.Abs(c.GetBoundingBox(true).Min.Y-firstY)<.01&&Math.Abs(c.GetBoundingBox(true).Max.Y-firstY)<.01);foreach(var dash in Dash(new List<Curve>{c}))add(dash,source?Color.FromArgb(40,100,220):Color.FromArgb(108,126,128),tr);}
  if(k==3&&array.Count>=2&&B(Labels,true)){
   var ys=array.Select(c=>c.GetBoundingBox(true).Min.Y).Distinct().OrderBy(y=>y).ToList();
   double x=box.Max.X+550,z=30,lastY=ys.Last();var blue=Color.FromArgb(40,100,220);var grey=Color.FromArgb(130,147,163);
   add(new LineCurve(new Point3d(x,firstY,z),new Point3d(x,lastY,z)),blue,tr);
   add(new LineCurve(new Point3d(x-160,lastY-350,z),new Point3d(x,lastY,z)),blue,tr);add(new LineCurve(new Point3d(x+160,lastY-350,z),new Point3d(x,lastY,z)),blue,tr);
   foreach(double y in ys)add(new LineCurve(new Point3d(x-130,y,z),new Point3d(x+130,y,z)),blue,tr);
   if(firstY>0){add(new LineCurve(new Point3d(x,0,z),new Point3d(x,firstY,z)),grey,tr);add(new TextDot("端部预留 "+(firstY/1000).ToString("0.00")+" m",new Point3d(x,firstY/2,z)),grey,tr);}
   add(new TextDot("首跨榀距 "+((ys[1]-ys[0])/1000).ToString("0.00")+" m",new Point3d(x,(ys[0]+ys[1])/2,z)),blue,tr);
   add(new TextDot("沿 +Y 复制 · 共 "+ys.Count+" 榀",new Point3d(x,lastY+350,z)),blue,tr);
  }
 }
 else{var cs=k==5?pre:ribs.Concat(rails).Cast<GeometryBase>().ToList();for(int j=0;j<cs.Count;j++){var g=cs[j];if(B(Solid)&&g is Curve curve){var ds=k==5?previewDiameters:diameters;double d=j<ds.Count?ds[j]:100;var pipes=Brep.CreatePipe(curve,d/2,false,PipeCapMode.Flat,true,.5,.03);if(pipes!=null&&pipes.Length>0){foreach(var p in pipes)add(p,ink,tr);continue;}}add(g,k==5&&j<preColors.Count?preColors[j]:ink,tr);}if(B(Solid)){var bases=k==5?PreviewBases:SelectedBases;foreach(var b in (bases??new List<object>()).Select(V).OfType<GeometryBase>())add(b,Color.FromArgb(170,179,173),tr);}if(k==6&&B(ShowRoof,true)){foreach(var g in roof)add(g,Color.FromArgb(110,200,210,198),tr);foreach(var c in edges)add(c,Color.FromArgb(140,150,140),tr);}}
 if(B(Labels,true))add(new TextDot(names[k]+(k==3?"\n蓝色首榀继承第②步单拱；灰绿为复制拱架":"")+(k>=5&&ribs.Count==0?"\n尚未采用方案":""),new Point3d(0,box.Min.Y-1400,0)){FontHeight=14},ink,tr);
}
Geometry=output;Colors=colors;Info="第2—4步保留同一场地框。第3步蓝色首榀继承第2步模数，按端部预留定位；灰绿为沿+Y复制的拱架。Labels控制方向/首跨榀距标识。1—4始终虚线；5显示已选方案或推荐预览，6仅在用户选定后生成。Stage=0六步同一水平线；单步可切Solid管状。工程几何请用各步骤原坐标输出。";Component.Message=stage==0?"六步横排":"第"+stage+"步";

 }catch(Exception e){Info="未完成："+e.Message;Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,T(Info));}
 }

static object V(object x)=>x is IGH_Goo g?g.ScriptVariable():x;
static double N(object x,double d=0)=>x==null?d:Convert.ToDouble(V(x),CultureInfo.InvariantCulture);
static int I(object x,int d=0)=>(int)N(x,d);
static bool B(object x,bool d=false)=>x==null?d:Convert.ToBoolean(V(x));
static string T(object x)=>Convert.ToString(V(x),CultureInfo.InvariantCulture);
static List<Plane> Planes(List<object> x)=>(x??new List<object>()).Select(v=>(Plane)V(v)).ToList();
static List<double> Numbers(List<object> x)=>(x??new List<object>()).SelectMany(v=>T(v).Split(new[]{'\n','\r',';',','},StringSplitOptions.RemoveEmptyEntries)).Select(v=>double.Parse(v.Trim(),CultureInfo.InvariantCulture)).ToList();
static List<Curve> Curves(List<object> x)=>(x??new List<object>()).Select(V).OfType<Curve>().ToList();
static double[] SiteData(object x){var p=T(x).Split('|');MathRules.Require(p.Length==4&&p[0]=="SITE6S","请接第一步 Site 输出");return p.Skip(1).Select(v=>double.Parse(v,CultureInfo.InvariantCulture)).ToArray();}
static double[] LayoutData(object x){var p=T(x).Split('|');MathRules.Require(p.Length==5&&p[0]=="SITE6L","请接第三步 Layout 输出");return p.Skip(1).Select(v=>double.Parse(v,CultureInfo.InvariantCulture)).ToArray();}
static List<Curve> Dash(IEnumerable<Curve> cs){var r=new List<Curve>();foreach(var c in cs){double l=c.GetLength();for(double d=0;d<l;d+=300){if(c.LengthParameter(d,out double a)&&c.LengthParameter(Math.Min(l,d+180),out double b)){var q=c.Trim(a,b);if(q!=null)r.Add(q);}}}return r;}
static Dictionary<string,double> Config(object value,Dictionary<string,double> defaults){foreach(var line in T(value).Split(new[]{'\n','\r',';'},StringSplitOptions.RemoveEmptyEntries)){var p=line.Split('=');if(p.Length!=2||!defaults.ContainsKey(p[0].Trim()))throw new Exception("未知配置行："+line);defaults[p[0].Trim()]=double.Parse(p[1].Trim(),CultureInfo.InvariantCulture);}return defaults;}

}
namespace BambooStages123
{
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;


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
   MathRules.Require(FMath.IsFinite(Spec.LegH)&&FMath.IsFinite(Spec.Angle),"LegH/LegAngle 必须为有限数。");
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
   MathRules.Require(FMath.IsFinite(Radius)&&FMath.IsFinite(HalfLength)&&HalfLength>tol,"圆弧数值退化，请调整尺寸。");
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
   MathRules.Require(FMath.IsFinite(u)&&u>=0&&u<=1,"上部弧长参数必须在0—1之间。");
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
  public static bool Positive(double n)=>FMath.IsFinite(n)&&n>0;
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
namespace BambooStages123
{
using System;
using System.Linq;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;


 public sealed class UnitGeometry
 {
  public Curve Whole,Upper;
  public List<Curve> Legs=new List<Curve>();
  public List<Point3d> Keys=new List<Point3d>();
 }
 public sealed class LayoutResult
 {
  public StructurePlan Plan;
  public List<List<Curve>> InnerMembers=new List<List<Curve>>(),WebMembers=new List<List<Curve>>();
  public List<List<string>> InnerLinks=new List<List<string>>(),WebLinks=new List<List<string>>();
  public List<Curve> Ribs=new List<Curve>(),Upper=new List<Curve>(),Purlins=new List<Curve>();
  public List<List<Curve>> Legs=new List<List<Curve>>(),ArchMembers=new List<List<Curve>>(),RailMembers=new List<List<Curve>>();
  public List<List<Point3d>> Nodes=new List<List<Point3d>>();
  public List<List<string>> NodeIds=new List<List<string>>(),ArchLinks=new List<List<string>>(),LegLinks=new List<List<string>>(),RailLinks=new List<List<string>>();
  public List<Point3d> Feet=new List<Point3d>(),Crowns=new List<Point3d>(),Stations=new List<Point3d>();
  public List<Plane> Frames=new List<Plane>();
  public List<double> Heights=new List<double>(),Distances=new List<double>(),ActualS=new List<double>(),ActualP=new List<double>(),Knees=new List<double>();
  public int Count;
 }
 public static class RhinoRules
 {
  public static Point3d Pt(V2 v)=>new Point3d(v.X,0,v.Z);
  public static Curve ArcSegment(Profile p,double a,double b)
  {
   var arc=new Arc(Pt(p.Upper(a)),Pt(p.Upper((a+b)/2)),Pt(p.Upper(b)));
   MathRules.Require(arc.IsValid,"圆弧构造失败，请增大尺寸或减少分段。");return new ArcCurve(arc);
  }
  public static UnitGeometry Unit(Profile p)
  {
   var g=new UnitGeometry();
   if(p.Spec.Shape==1)g.Upper=ArcSegment(p,0,1);
   else{var up=new PolyCurve();MathRules.Require(up.Append(ArcSegment(p,0,.5))&&up.Append(ArcSegment(p,.5,1)),"尖拱双圆弧拼接失败。");g.Upper=up;}
   if(p.Spec.Foot!=0)
   {
    var left=new LineCurve(Pt(p.LeftFoot),Pt(p.LeftKnee));var right=new LineCurve(Pt(p.RightKnee),Pt(p.RightFoot));
    g.Legs.Add(left);g.Legs.Add(right);var whole=new PolyCurve();
    MathRules.Require(whole.Append(left.DuplicateCurve())&&whole.Append(g.Upper.DuplicateCurve())&&whole.Append(right.DuplicateCurve()),"分段拱拼接失败。");g.Whole=whole;
   }
   else g.Whole=g.Upper.DuplicateCurve();
   g.Keys.AddRange(new[]{Pt(p.LeftFoot),Pt(p.LeftKnee),Pt(p.Crown),Pt(p.RightKnee),Pt(p.RightFoot)});
   MathRules.Require(g.Whole.IsValid&&g.Upper.IsValid,"单榀曲线无效。");return g;
  }
  public static Curve DemoAxis(int kind)
  {
   MathRules.Require(kind==1||kind==2,"Demo=0自定义，1演示直线，2演示曲线。");
   return NurbsCurve.Create(false,3,new[]{new Point3d(0,0,0),new Point3d(0,4000,0),new Point3d(kind==2?1800:0,8000,0),new Point3d(kind==2?1800:0,12000,0)});
  }
  public static Curve DemoGuide(Curve axis,Spec spec)
  {
   var c=axis.ToNurbsCurve();var factors=new[]{.9,1,.85,.95};
   for(int i=0;i<c.Points.Count;i++){var old=c.Points[i];var p=old.Location;p.Z+=spec.LegH+(spec.H-spec.LegH)*factors[i%4];c.Points.SetPoint(i,p,old.Weight);}
   return c;
  }
  public static void CheckAxis(Curve c,double tol)
  {
   MathRules.Require(c!=null&&c.IsValid&&!c.IsClosed,"C 必须是有效开放曲线。");
   var b=c.GetBoundingBox(true);MathRules.Require(b.Max.Z-b.Min.Z<=tol,"首版 C 必须水平等标高，逐榀高度请使用HGuide或Heights。");
   MathRules.Require(!c.GetNextDiscontinuity(Continuity.G1_continuous,c.Domain.T0,c.Domain.T1,out _),"C 不支持有折角的折线，请改为切线连续曲线。");
   var self=Intersection.CurveSelf(c,tol);MathRules.Require(self==null||self.Count==0,"C 存在自交或重叠。");
  }
  public static double[] GuideHeights(Curve axis,Curve guide,List<Point3d> stations,double tol)
  {
   MathRules.Require(guide!=null&&guide.IsValid&&!guide.IsClosed,"HeightMode=1 必须提供有效开放HGuide。");
   using(var flat=guide.DuplicateCurve())
   {
    MathRules.Require(flat.Transform(Transform.PlanarProjection(new Plane(axis.PointAtStart,Vector3d.ZAxis))),"HGuide 投影失败。");
    bool f=flat.PointAtStart.DistanceTo(axis.PointAtStart)<=tol&&flat.PointAtEnd.DistanceTo(axis.PointAtEnd)<=tol;
    bool r=flat.PointAtStart.DistanceTo(axis.PointAtEnd)<=tol&&flat.PointAtEnd.DistanceTo(axis.PointAtStart)<=tol;
    MathRules.Require(f||r,"HGuide 投影的起止点必须与完整C对应，允许反向，不支持缺段或延长。");
    var hits=Intersection.CurveSelf(flat,tol);MathRules.Require(hits==null||hits.Count==0,"HGuide 平面投影自交或重叠。");
    double max,ta,tb,min,sa,sb;
    MathRules.Require(Curve.GetDistancesBetweenCurves(axis,flat,tol,out max,out ta,out tb,out min,out sa,out sb)&&max<=tol,"HGuide 的XY投影须与C重合；请复制轴线后仅修改Z。");
    MathRules.Require(Curve.GetDistancesBetweenCurves(flat,axis,tol,out max,out ta,out tb,out min,out sa,out sb)&&max<=tol,"HGuide 平面投影与C不一致。");
   }
   var box=guide.GetBoundingBox(true);double pad=Math.Max(box.Diagonal.Length,1);var hs=new List<double>();
   foreach(var s in stations)
   {
    var ray=new Line(new Point3d(s.X,s.Y,box.Min.Z-pad),new Point3d(s.X,s.Y,box.Max.Z+pad));
    var hits=Intersection.CurveLine(guide,ray,tol,tol);var pts=new List<Point3d>();
    if(hits!=null)foreach(var h in hits){MathRules.Require(!h.IsOverlap,"HGuide 含竖直重叠段，高度不唯一。");if(h.IsPoint&&pts.All(p=>p.DistanceTo(h.PointA)>tol))pts.Add(h.PointA);}
    MathRules.Require(pts.Count==1,$"第{hs.Count+1}榀获得{pts.Count}个高度交点，要求恰好1个。");
    MathRules.Require(Math.Sqrt(Math.Pow(pts[0].X-s.X,2)+Math.Pow(pts[0].Y-s.Y,2))<=2*tol,"高度交点偏离轴线。");hs.Add(pts[0].Z-s.Z);
   }
   return hs.ToArray();
  }
  static string Nid(int i,int j)=>$"N{i}_{j}";
  static string Link(string id,int i,int j,int k)=>$"{id}|{Nid(i,j)}|{Nid(i,k)}|Joint=UNDEFINED";
  public static LayoutResult Layout(Spec spec,Curve axis,double spacing,bool end,int heightMode,Curve guide,double[] heights,int pMode,double p,int count,double tol)
  {
   CheckAxis(axis,tol);MathRules.Require(heightMode>=0&&heightMode<=2,"HeightMode=0统一高度、1控制线、2高度列表。");
   var r=new LayoutResult();double length=axis.GetLength();r.Distances=MathRules.Stations(length,spacing,end,tol);
   foreach(double d in r.Distances)
   {
    double t=axis.Domain.T0;if(d>=length-tol)t=axis.Domain.T1;else if(d>0)MathRules.Require(axis.LengthParameter(d,out t),"无法按弧长定位阵列。");
    var pt=axis.PointAt(t);var tangent=axis.TangentAt(t);tangent.Z=0;MathRules.Require(tangent.Unitize(),"轴线站点切向无效。");
    var across=Vector3d.CrossProduct(tangent,Vector3d.ZAxis);MathRules.Require(across.Unitize(),"横跨方向无效。");
    r.Stations.Add(pt);r.Frames.Add(new Plane(pt,across,Vector3d.ZAxis));
   }
   var hs=heightMode==1?GuideHeights(axis,guide,r.Stations,tol):heightMode==2?heights:Enumerable.Repeat(spec.H,r.Stations.Count).ToArray();
   MathRules.Require(hs!=null&&hs.Length==r.Stations.Count,$"Heights 应含{r.Stations.Count}个数，每榀一个；不自动重复或截断。");
   var profiles=new List<Profile>();
   for(int i=0;i<hs.Length;i++){var s=spec.Copy();s.H=hs[i];try{profiles.Add(new Profile(s,tol));}catch(Exception e){throw new ArgumentException($"第{i+1}榀 / 里程{r.Distances[i]:0.###} / H={hs[i]:0.###}：{e.Message}");}}
   var spacingProfiles=spec.Structure==1?profiles.Select(pr=>StructureRules.PhysicalProfile(pr.Spec,tol)).ToList():profiles;
   r.Count=MathRules.RailCount(spacingProfiles,pMode,p,count);MathRules.Require((long)r.Count*profiles.Count<=30000,"模型超过30000个对应节点，请减少榀数或檩条道数。");
   if(spec.Structure!=0)return TopologyRhino.Layout(spec,r,hs.ToList(),tol);
   var plane=new Plane(Point3d.Origin,Vector3d.XAxis,Vector3d.ZAxis);
   var groundSpans=new List<Line>();
   for(int i=0;i<profiles.Count;i++)
   {
    var pr=profiles[i];var g=Unit(pr);var xf=Transform.PlaneToPlane(plane,r.Frames[i]);
    g.Whole.Transform(xf);g.Upper.Transform(xf);foreach(var leg in g.Legs)leg.Transform(xf);
    r.Ribs.Add(g.Whole);r.Upper.Add(g.Upper);r.Legs.Add(g.Legs);r.Heights.Add(hs[i]);r.Knees.Add(pr.KneeAngle);r.ActualP.Add(pr.HalfLength*2/(r.Count-1));
    if(i>0)r.ActualS.Add(r.Distances[i]-r.Distances[i-1]);
    var nodes=new List<Point3d>();var ids=new List<string>();
    for(int j=0;j<r.Count;j++){var q=Pt(pr.Upper((double)j/(r.Count-1)));q.Transform(xf);nodes.Add(q);ids.Add(Nid(i,j));}
    var fl=Pt(pr.LeftFoot);var fr=Pt(pr.RightFoot);fl.Transform(xf);fr.Transform(xf);r.Feet.Add(fl);r.Feet.Add(fr);r.Crowns.Add(nodes[r.Count/2]);groundSpans.Add(new Line(fl,fr));
    var leglinks=new List<string>();
    if(pr.Spec.Foot!=0){nodes.Add(fl);ids.Add(Nid(i,r.Count));nodes.Add(fr);ids.Add(Nid(i,r.Count+1));leglinks.Add(Link($"L{i}_0",i,r.Count,0));leglinks.Add(Link($"L{i}_1",i,r.Count-1,r.Count+1));}
    r.Nodes.Add(nodes);r.NodeIds.Add(ids);r.LegLinks.Add(leglinks);
    var members=new List<Curve>();var links=new List<string>();
    for(int j=0;j<r.Count-1;j++){var seg=ArcSegment(pr,(double)j/(r.Count-1),(double)(j+1)/(r.Count-1));seg.Transform(xf);members.Add(seg);links.Add(Link($"A{i}_{j}",i,j,j+1));}
    r.ArchMembers.Add(members);r.ArchLinks.Add(links);
   }
   // Conservative geometric rejection, not a 3D collision or stability test.
   for(int i=0;i<groundSpans.Count;i++)for(int j=i+1;j<groundSpans.Count;j++)
   {double a,b;if(Intersection.LineLine(groundSpans[i],groundSpans[j],out a,out b,tol,true))throw new ArgumentException($"第{i+1}与{j+1}榀的地面跨度线相交；轴线弯曲过急或跨度过大，首版不支持此布置。");}
   if(profiles.Count>1)for(int j=0;j<r.Count;j++)
   {
    var line=new PolylineCurve(r.Nodes.Select(n=>n[j]));r.Purlins.Add(line);var segments=new List<Curve>();var links=new List<string>();
    for(int i=0;i<profiles.Count-1;i++){MathRules.Require(r.Nodes[i][j].DistanceTo(r.Nodes[i+1][j])>tol,"相邻檩条节点重合。");segments.Add(new LineCurve(r.Nodes[i][j],r.Nodes[i+1][j]));links.Add($"P{j}_{i}|{Nid(i,j)}|{Nid(i+1,j)}|Joint=UNDEFINED");}
    r.RailMembers.Add(segments);r.RailLinks.Add(links);
   }
   return r;
  }
  public static List<Curve> Dashes(IEnumerable<Curve> input,double dash,double tol)
  {
   MathRules.Require(MathRules.Positive(dash)&&dash>tol,"Dash 必须大于文档容差，或用0自动选择。");
   var result=new List<Curve>();
   foreach(var c in input)
   {
    double len=c.GetLength();for(double d=0;d<len-tol;d+=1.6*dash)
    {
     MathRules.Require(result.Count<50000,"虚线超过50000段，请增大Dash。");double e=Math.Min(d+dash,len),a=c.Domain.T0,b=c.Domain.T1;
     if(d>0)MathRules.Require(c.LengthParameter(d,out a),"虚线起点定位失败。");if(e<len)MathRules.Require(c.LengthParameter(e,out b),"虚线终点定位失败。");
     var part=c.Trim(a,b);if(part!=null&&part.IsValid)result.Add(part);
    }
   }
   return result;
  }
 }
}

namespace BambooStages123 {
using System;
using System.Linq;
using System.Collections.Generic;


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
   if(s.Structure==1){MathRules.Require(FMath.IsFinite(s.CrossAngle)&&s.CrossAngle>0&&s.CrossAngle<=45,"CrossAngle为相对竖直拱面的纵向倾角，须在(0,45]度。");
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
namespace BambooStages123 {
using System;
using System.Linq;
using System.Collections.Generic;
using Rhino.Geometry;


 public static class TopologyRhino {
  public static P3 P(Point3d p)=>new P3(p.X,p.Y,p.Z);
  public static P3 V(Vector3d v)=>new P3(v.X,v.Y,v.Z);
  public static Point3d Point(P3 p)=>new Point3d(p.X,p.Y,p.Z);
  public static List<Frame3> Frames(List<Plane> planes)=>planes.Select(p=>new Frame3(P(p.Origin),V(p.XAxis),V(Vector3d.CrossProduct(Vector3d.ZAxis,p.XAxis)))).ToList();
  public static Curve Curve(MemberCurve m){
   if(!m.Arc)return new LineCurve(Point(m.A),Point(m.B));
   var arc=new Arc(Point(m.At(0)),Point(m.At(.5)),Point(m.At(1)));MathRules.Require(arc.IsValid,"原型圆弧退化："+m.Id);return new ArcCurve(arc);
  }
  public static List<Curve> Unit(Spec spec,double tol,out StructurePlan plan){
   var frames=new List<Frame3>{new Frame3(new P3(0,0,0),new P3(1,0,0),new P3(0,1,0))};if(spec.Structure==1)frames.Add(new Frame3(new P3(0,2000,0),new P3(1,0,0),new P3(0,1,0)));
   plan=StructureRules.Build(spec,frames,Enumerable.Repeat(spec.H,frames.Count).ToList(),9,tol,false);return plan.Curves.Where(m=>m.Role!="Purlin").Select(Curve).ToList();
  }
  public static Mesh Roof(StructurePlan plan){var mesh=new Mesh();mesh.Vertices.UseDoublePrecisionVertices=true;foreach(var t in plan.Roof){int n=mesh.Vertices.Count;mesh.Vertices.Add(Point(t.A));mesh.Vertices.Add(Point(t.B));mesh.Vertices.Add(Point(t.C));mesh.Faces.AddFace(n,n+1,n+2);}mesh.Normals.ComputeNormals();mesh.Compact();return mesh;}
  public static LayoutResult Layout(Spec spec,LayoutResult r,List<double> heights,double tol){
   var plan=StructureRules.Build(spec,Frames(r.Frames),heights,r.Count,tol);r.Plan=plan;
   var allNodes=new List<P3>();Func<P3,string> nid=p=>{int i=allNodes.FindIndex(q=>p.Distance(q)<=tol);if(i<0){i=allNodes.Count;allNodes.Add(p);}return "NX"+i;};
   for(int i=0;i<r.Frames.Count;i++){
    r.Legs.Add(new List<Curve>());r.ArchMembers.Add(new List<Curve>());r.InnerMembers.Add(new List<Curve>());r.WebMembers.Add(new List<Curve>());
    r.LegLinks.Add(new List<string>());r.ArchLinks.Add(new List<string>());r.InnerLinks.Add(new List<string>());r.WebLinks.Add(new List<string>());
    r.Nodes.Add(new List<Point3d>());r.NodeIds.Add(new List<string>());
   }
   foreach(var m in plan.Curves.Where(m=>m.Role!="Purlin")){
    var curve=Curve(m);r.Ribs.Add(curve);string a=nid(m.At(0)),b=nid(m.At(1));string link=$"{m.Id}|{a}|{b}|Role={m.Role}|Joint=UNDEFINED";int i=m.Frame;
    foreach(var entry in new[]{(a,m.At(0)),(b,m.At(1))})if(!r.NodeIds[i].Contains(entry.Item1)){r.NodeIds[i].Add(entry.Item1);r.Nodes[i].Add(Point(entry.Item2));}
    if(m.Role=="Outer"){r.Upper.Add(curve);r.ArchMembers[i].Add(curve);r.ArchLinks[i].Add(link);}
    else if(m.Role=="Inner"){r.InnerMembers[i].Add(curve);r.InnerLinks[i].Add(link);}
    else if(m.Role=="Web"){r.WebMembers[i].Add(curve);r.WebLinks[i].Add(link);}
    else{r.Legs[i].Add(curve);r.LegLinks[i].Add(link);}
   }
   for(int j=0;j<plan.Rails.Count;j++){
    var rail=plan.Rails[j];r.Purlins.Add(new PolylineCurve(rail.Select(Point)));var curves=new List<Curve>();var links=new List<string>();
    for(int i=0;i<rail.Count-1;i++){curves.Add(new LineCurve(Point(rail[i]),Point(rail[i+1])));links.Add($"P{j}_{i}|{nid(rail[i])}|{nid(rail[i+1])}|Role=Purlin|Joint=UNDEFINED");}
    r.RailMembers.Add(curves);r.RailLinks.Add(links);
   }
   r.Feet.AddRange(plan.Feet.Select(Point));r.Crowns.AddRange(plan.Maps.Select(m=>Point(m.At(.5))));r.Heights.AddRange(heights);r.Knees.AddRange(plan.Maps.Select(m=>m.Outer.KneeAngle));
   r.ActualP.AddRange(plan.Maps.Select(m=>2*m.Outer.HalfLength/(r.Count-1)));for(int i=1;i<r.Distances.Count;i++)r.ActualS.Add(r.Distances[i]-r.Distances[i-1]);
   return r;
  }
 }
}


public static class FMath {public static bool IsFinite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x); }
