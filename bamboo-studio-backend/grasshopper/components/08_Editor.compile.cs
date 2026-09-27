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
using Bamboo45;
using BambooCompare;
public abstract class Script_Instance : GH_ScriptInstance {
#if COMPILE_CHECK
 protected IGH_Component Component=>null;
#endif
int lastRequest=0;string editorBaseKey="",editorAfterKey="";Evaluation beforeEval,afterEval;
 private void RunScript(object SelectedPackage, object AppPackage, object SourceMode, object Overrides, object Calculate, object UseEdited, object ShowCompare, ref object SelectedSpec, ref object SelectedHeights, ref object SelectedFrames, ref object SelectedRailCount, ref object SelectedRibs, ref object SelectedPurlins, ref object SelectedDiameters, ref object SelectedFeet, ref object SelectedBases, ref object BeforeStress, ref object AfterStress, ref object LegendMax, ref object CompareGeometry, ref object CompareColors, ref object IsFresh, ref object AfterValid, ref object EditTemplate, ref object Report, ref object Info) {
SelectedSpec=null;SelectedHeights=null;SelectedFrames=null;SelectedRailCount=null;SelectedRibs=null;SelectedPurlins=null;SelectedDiameters=null;SelectedFeet=null;SelectedBases=null;BeforeStress=null;AfterStress=null;LegendMax=null;CompareGeometry=null;CompareColors=null;IsFresh=null;AfterValid=null;EditTemplate=null;Report=null;Info=null;
 try {
Settings.Require(RhinoDoc.ActiveDoc!=null&&RhinoDoc.ActiveDoc.ModelUnitSystem==UnitSystem.Millimeters,"请在 Rhino 毫米文档运行。");
int request=I(Calculate),mode=I(SourceMode);Settings.Require(mode==0||mode==1,"SourceMode=0接第五步；1读取App参数包");
bool clicked=request>0&&request!=lastRequest;lastRequest=request;
string source=mode==1?T(AppPackage):T(SelectedPackage);
if(string.IsNullOrWhiteSpace(source)){Info=mode==1?"将App导出的GH参数包完整粘贴到AppPackage面板。":"先在05选择推荐方案或改进起点。";Component.Message="05B 等待选定方案";return;}
var packet=BambooExchange.Exchange.Parse(source);
var bp=packet.Before;
var editKeys=new List<string>{"W","H","ArchD","ArchT","PurlinCount","PurlinD","PurlinT"};
int editType=(int)bp["type"];
if(editType!=5)editKeys.AddRange(new[]{"Foot","LegH","LegAngle"});
if(editType==2)editKeys.Add("CrossAngle");
if(editType==4)editKeys.AddRange(new[]{"TrussDepth","Panels"});
if(editType==5)editKeys.AddRange(new[]{"PointE","PointSlope","PointFootGap","PointPanels","PointLowerD","PointLowerT","PointWebD","PointWebT"});
EditTemplate="# 已选构型："+ComparisonRules.Names[editType]+"\n# 以下是05实际选中值；复制需修改的行到Overrides。\n"+string.Join("\n",editKeys.Select(k=>k+"="+bp[k].ToString("R",CultureInfo.InvariantCulture)));
var ap=BambooExchange.Exchange.Edit(packet.After,T(Overrides));
string baseKey=BambooExchange.Exchange.Key(bp,packet.Stations),afterKey=BambooExchange.Exchange.Key(ap,packet.Stations);
if(editorBaseKey!=baseKey){editorBaseKey=baseKey;beforeEval=null;afterEval=null;editorAfterKey="";}
var bg=BambooExchange.Exchange.Geometry(bp,packet.Stations);var ag=BambooExchange.Exchange.Geometry(ap,packet.Stations);
if(clicked){
 if(beforeEval==null||!beforeEval.Solved)beforeEval=Engine.Evaluate("修改前固定基准",bg,BambooExchange.Exchange.SettingsFor(bp));
 afterEval=baseKey==afterKey?beforeEval:Engine.Evaluate("修改后精确方案",ag,BambooExchange.Exchange.SettingsFor(ap));editorAfterKey=afterKey;
}
bool fresh=afterEval!=null&&editorAfterKey==afterKey;
var current=fresh?afterEval:null;bool usable=current!=null&&current.Solved&&current.Pass&&current.Warnings.Count==0;
AfterValid=usable;IsFresh=fresh;
BeforeStress=beforeEval!=null&&beforeEval.Solved?beforeEval.Stresses:null;AfterStress=current!=null&&current.Solved?current.Stresses:null;
double max=Math.Max(.001,Math.Max(beforeEval!=null&&beforeEval.Solved?beforeEval.StressMax:0,current!=null&&current.Solved?current.StressMax:0));LegendMax=max;
var geo=new List<GeometryBase>();var col=new List<Color>();
if(B(ShowCompare,true)){
 double pitch=Math.Max(bp["SiteW"],ap["SiteW"])*1.4;double y=-Math.Max(bp["SiteL"],ap["SiteL"])-6000;
 Action<GeometrySet,Evaluation,double,string> draw=(g,e,x,label)=>{
  var transform=Transform.Translation(x,y,0);
  if(e!=null&&e.Solved){for(int j=0;j<e.Members.Count;j++){var line=new LineCurve(e.Members[j].A,e.Members[j].B);line.Transform(transform);geo.Add(line);col.Add(BambooExchange.Exchange.ColorFor(e.Stresses[j],max));}}
  else foreach(var curve in g.All){var c=curve.DuplicateCurve();c.Transform(transform);geo.Add(c);col.Add(Color.FromArgb(136,151,158));}
  geo.Add(new TextDot(label+(e!=null&&e.Solved?$"\n位移 {e.Displacement:0.00} mm · 应力 {e.StressMax:0.00} MPa · 质量 {e.Mass:0.00} kg":"\n几何预览 · 尚无有效应力"),new Point3d(x,y-1000,0)));col.Add(Color.FromArgb(50,66,81));
 };
 draw(bg,beforeEval,0,"修改前 · 固定基准");draw(ag,current,pitch,usable?"修改后 · 满足所设初筛":current!=null&&current.Solved?"修改后 · 已计算但未通过":"修改后 · 待计算");
 geo.Add(new TextDot($"共用色标 0—{max:0.00} MPa；逐梁正应力绝对值包络，非安全等级。",new Point3d(pitch/2,y-2000,0)));col.Add(Color.FromArgb(50,66,81));
 // Readable five-stop scale, same mapping used by both diagrams.
 for(int i=0;i<40;i++){double x=pitch*.3+pitch*.4*i/40;geo.Add(new LineCurve(new Point3d(x,y-2600,0),new Point3d(x+pitch*.4/40,y-2600,0)));col.Add(BambooExchange.Exchange.ColorFor(max*i/39,max));}
}
CompareGeometry=geo;CompareColors=col;
var chosen=B(UseEdited)?usable?ap:null:beforeEval!=null&&beforeEval.Solved&&beforeEval.Pass&&beforeEval.Warnings.Count==0?bp:null;
if(chosen!=null){
 var g=B(UseEdited)?ag:bg;var s=BambooExchange.Exchange.SettingsFor(chosen);
 SelectedSpec=g.Network.Spec.Encode();SelectedHeights=g.VerticalHeights;SelectedFrames=g.Frames;SelectedRailCount=g.Network.RailCount;
 SelectedRibs=g.Ribs;SelectedPurlins=g.Rails;SelectedFeet=g.Network.Feet.Select(TopologyRhino.Point).ToList();SelectedBases=BambooExchange.Exchange.Bases(g,s);
 SelectedDiameters=g.Network.Curves.Where(m=>m.Role!="Purlin").Concat(g.Network.Curves.Where(m=>m.Role=="Purlin")).Select(m=>s.DiameterFor(m,g.Network.Spec)).ToList();
}
string state=current==null?"当前修改待计算：将Calculate加1。":!current.Solved?"未生成数值："+current.Error:usable?"修改后满足所设初筛。":"修改后已求解，但未通过所设初筛。";
Info=state+(B(UseEdited)?usable?" 已采用编辑方案，第六步已联动。":" UseEdited已开，但当前没有可采用结果，第六步暂停。":" UseEdited=False：仅当基准已计算且通过初筛才传至06；开启UseEdited采用通过初筛的修改。")+" 修改参数不会自动搜索或改选型；Calculate每增加1，单独计算当前输入。";
Report=Info+"\n来源="+(mode==0?"GH第五步已选方案":"App参数包；数值由Karamba重新计算")+"\n"+BambooExchange.Exchange.Difference(bp,ap)+"\n\n修改前：\n"+BambooExchange.Exchange.Report(beforeEval)+"\n\n修改后：\n"+BambooExchange.Exchange.Report(current)+"\n"+Engine.Scope;
Component.Message=usable?"05B 可采用当前修改":fresh?"05B 查看计算原因":"05B 参数待计算";

Report=T(Report)+"\n\n修改前阈值：\n"+ScreeningNote.Report(BambooExchange.Exchange.SettingsFor(bp),bp["W"],bp["Limit"],beforeEval)+"\n\n修改后阈值：\n"+ScreeningNote.Report(BambooExchange.Exchange.SettingsFor(ap),ap["W"],ap["Limit"],current)+"\n改变阈值、材料或荷载后，通过状态的变化不能单独证明构型改善。";

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

namespace Bamboo45
{
using System;
using System.Linq;
using System.Collections.Generic;
using Rhino.Geometry;
using BambooStages123;
using KarambaCommon;
using Karamba.CrossSections;
using Karamba.Materials;
using Karamba.Elements;
using Karamba.Loads;
using Karamba.Supports;
using Karamba.Joints;
using Karamba.Results;
using Karamba.Utilities;
using KPoint=Karamba.Geometry.Point3;
using KVector=Karamba.Geometry.Vector3;
using KLine=Karamba.Geometry.Line3;


 public sealed class Settings {
  public Action Check=()=>{};
  public double HeightMax,SiteWidth,EndMargin,AxisLength,SiteOrigin;
  public int WindModel=1,WindDirection,WindEnds;public bool WindReverse;public double WindAngle,WindRoofWindward=.8,WindRoofLeeward=-.5,WindRoofParallel=-.7,WindEndWindward=.8,WindEndLeeward=-.5,WindEndParallel=-.7;
 public double LD=120,LT=10,WD=60,WT=6;
 public double DiameterFor(MemberCurve m,Spec spec)=>m.Role=="Purlin"?PD:spec.CompoundPointed?(m.Role=="Web"?WD:m.Role=="Inner"||m.Role=="Return"||m.Role=="Column"?LD:AD):AD;
 public double WallFor(MemberCurve m,Spec spec)=>m.Role=="Purlin"?PT:spec.CompoundPointed?(m.Role=="Web"?WT:m.Role=="Inner"||m.Role=="Return"||m.Role=="Column"?LT:AT):AT;
  public double AD=100,AT=10,PD=100,PT=10,E=10850,Nu=.4,G=0,Rho=644,Fc=0,Ft=0,Fb=0;
  public double RoofG=.2,RoofQ=.5,W0=.7,MuZ=1,Beta=1,Cp=.8,Cs=-1,Snow=0,Limit=24,Pmax=0;
  public int Support=0,Joints=0,Mesh=2; public bool Buckle=false;
  public Settings Copy()=>(Settings)MemberwiseClone();
  public bool Strength=>Fc>0&&Ft>0&&Fb>0;
  public double Shear=>G>0?G:E/(2*(1+Nu));
  public void Validate(){
   foreach(var x in new[]{LD,LT,WD,WT,AD,AT,PD,PT,E,Rho,Limit,MuZ,Beta})Require(FMath.IsFinite(x)&&x>0,"截面、刚度、密度、位移阈值和风压系数应为有限正数。");
   foreach(var x in new[]{G,Fc,Ft,Fb,RoofG,RoofQ,W0,Snow,Pmax})Require(FMath.IsFinite(x)&&x>=0,"材料阈值与荷载大小不得为负或无穷。");
   Require(2*LT<LD&&2*WT<WD&&2*AT<AD&&2*PT<PD,"壁厚必须小于外径的一半。");Require(Nu>-.99&&Nu<.5,"泊松比需在(-0.99,0.5)内，仅用于等效各向同性近似。");
   Require((Fc==0&&Ft==0&&Fb==0)||Strength,"Fc/Ft/Fb须全部填写正值，或全部0表示强度未评估。");
   Require(FMath.IsFinite(Cp)&&Cp>=0&&FMath.IsFinite(Cs)&&Cs<=0,"Cp为非负下压系数，Cs为非正吸力系数。");
   Require(Support>=0&&Support<=1&&Joints>=0&&Joints<=3&&Mesh>=1&&Mesh<=8,"Support=0/1，Joints=0..3，Mesh=1..8。");
  }
  public static void Require(bool condition,string text){if(!condition)throw new ArgumentException(text);}
 }
 public sealed class GeometrySet {
  public StructurePlan Network;public List<double> VerticalHeights;
  public List<Curve> Ribs=new List<Curve>(),Rails=new List<Curve>(),Upper=new List<Curve>();
  public List<Profile> Profiles=new List<Profile>(); public List<Plane> Frames;
  public Point3d[][] Grid;public List<Point3d> Feet=new List<Point3d>();public double Factor;
  public List<Curve> All=>Ribs.Concat(Rails).ToList();
  public double MaxP=>Network?.MaxRailSpacing??Profiles.Max(p=>p.HalfLength*2)/(Grid[0].Length-1);
 }
 public sealed class Member {
  public Point3d A,B;public string Id,Role;public bool Arch;public double Area,I,Diameter;
 }
 public sealed class Evaluation {
  public string Name,State,Error="";public GeometrySet Geometry;public Settings Settings;
  public Karamba.Models.Model Model;public bool Attempted,Solved,Pass;
  public double Displacement,Ratio=double.NaN,Mass,Score,Equilibrium,Buckling=double.NaN;
  public double AxialMax,BendingMax,ShearMax,TorsionMax,ReactionMax,StressMax;
  public string DisplacementCase="",StressMember="",StressCase="";
  public List<Member> Members=new List<Member>();public List<double> Ratios=new List<double>(),Stresses=new List<double>();
  public List<string> Warnings=new List<string>(),CaseReports=new List<string>();
 }
 public static class Engine {
  public static GeometrySet Geometry(Spec spec,List<Plane> planes,List<double> heights,int rails,double factor,double tol){
   Settings.Require(planes.Count>=2&&planes.Count==heights.Count,"至少需要两榀，Frames与ActualHeights数量一致。");
   Settings.Require(rails>=3&&rails%2==1&&rails<=201,"RailCount需要3—201的奇数。");
   Settings.Require(FMath.IsFinite(factor)&&factor>0,"高度倍率必须为正。");
   var g=new GeometrySet{Factor=factor,Frames=planes,VerticalHeights=heights.Select(h=>spec.LegH+(h-spec.LegH)*factor).ToList()};g.Grid=new Point3d[planes.Count][];
   if(spec.Structure>=0){
    g.Network=StructureRules.Build(spec,TopologyRhino.Frames(planes),g.VerticalHeights,rails,tol);
    g.Ribs=g.Network.Curves.Where(m=>m.Role!="Purlin").Select(TopologyRhino.Curve).ToList();
    g.Upper=g.Network.Curves.Where(m=>m.Role=="Outer").Select(TopologyRhino.Curve).ToList();
    g.Rails=g.Network.Rails.Select(ps=>(Curve)new PolylineCurve(ps.Select(TopologyRhino.Point))).ToList();
    g.Profiles=g.Network.Maps.Select(m=>m.Outer).ToList();g.Grid=g.Network.RoofGrid.Select(row=>row.Select(TopologyRhino.Point).ToArray()).ToArray();g.Feet=g.Network.Feet.Select(TopologyRhino.Point).ToList();return g;
   }
   var source=new Plane(Point3d.Origin,Vector3d.XAxis,Vector3d.ZAxis);
   for(int i=0;i<planes.Count;i++){
    Settings.Require(planes[i].IsValid&&planes[i].YAxis.Z>.999999,"Frames局部Y必须竖直向上。");
    var s=spec.Copy();s.H=s.LegH+(heights[i]-s.LegH)*factor;
    var p=new Profile(s,tol);g.Profiles.Add(p);var xf=Transform.PlaneToPlane(source,planes[i]);var unit=RhinoRules.Unit(p);
    unit.Whole.Transform(xf);unit.Upper.Transform(xf);g.Ribs.Add(unit.Whole);g.Upper.Add(unit.Upper);
    g.Grid[i]=Enumerable.Range(0,rails).Select(j=>At(g,i,j/(double)(rails-1))).ToArray();
    var a=RhinoRules.Pt(p.LeftFoot);var b=RhinoRules.Pt(p.RightFoot);a.Transform(xf);b.Transform(xf);g.Feet.Add(a);g.Feet.Add(b);
   }
   for(int j=0;j<rails;j++)g.Rails.Add(new PolylineCurve(g.Grid.Select(row=>row[j])));
   for(int i=0;i<planes.Count-1;i++)for(int j=0;j<rails-1;j++)Cell(g.Grid[i][j],g.Grid[i][j+1],g.Grid[i+1][j+1],g.Grid[i+1][j]);
   return g;
  }
  public static Point3d At(GeometrySet g,int i,double u){var v=g.Profiles[i].Upper(u);return g.Frames[i].PointAt(v.X,v.Z);}
  public static Vector3d Cell(Point3d a,Point3d b,Point3d c,Point3d d){
   var ab=(b-a)*.001;var ac=(c-a)*.001;var ad=(d-a)*.001;
   var one=Vector3d.CrossProduct(ab,ac)*.5;var two=Vector3d.CrossProduct(ac,ad)*.5;
   Settings.Require(one.Z*two.Z>=0&&Math.Abs(one.Z+two.Z)>1e-10,"屋面单元水平投影翻折或退化，请检查阵列轴线。");
   var n=one+two;if(n.Z<0)n=-n;return n;
  }
  public static double SurfaceArea(Point3d a,Point3d b,Point3d c,Point3d d)=>
   (Vector3d.CrossProduct((b-a)*.001,(c-a)*.001).Length+Vector3d.CrossProduct((c-a)*.001,(d-a)*.001).Length)*.5;
  public static Rhino.Geometry.Mesh Roof(GeometrySet g){
   if(g.Network!=null)return TopologyRhino.Roof(g.Network);
   var mesh=new Rhino.Geometry.Mesh();mesh.Vertices.UseDoublePrecisionVertices=true;
   int n=g.Grid[0].Length;foreach(var row in g.Grid)foreach(var p in row)mesh.Vertices.Add(p);
   for(int i=0;i<g.Grid.Length-1;i++)for(int j=0;j<n-1;j++){
    int a=i*n+j,b=a+1,c=(i+1)*n+j+1,d=c-1;
    if(Vector3d.CrossProduct(g.Grid[i][j+1]-g.Grid[i][j],g.Grid[i+1][j+1]-g.Grid[i][j]).Z>0){mesh.Faces.AddFace(a,b,c);mesh.Faces.AddFace(a,c,d);}else{mesh.Faces.AddFace(a,c,b);mesh.Faces.AddFace(a,d,c);}
   }mesh.Normals.ComputeNormals();mesh.Compact();return mesh;
  }
  public static double Area(double d,double t)=>Math.PI/4*(d*d-(d-2*t)*(d-2*t));
  public static double Inertia(double d,double t)=>Math.PI/64*(Math.Pow(d,4)-Math.Pow(d-2*t,4));
  static KPoint KP(Point3d p)=>new KPoint(p.X*.001,p.Y*.001,p.Z*.001);
  static KVector KV(Vector3d v)=>new KVector(v.X,v.Y,v.Z);
  public static Evaluation Evaluate(string name,GeometrySet g,Settings s){
   var e=new Evaluation{Name=name,Geometry=g,Settings=s};
   try{ s.Check();s.Validate();if(s.Pmax>0)Settings.Require(g.MaxP<=s.Pmax+.001,"候选超出Pmax檩条间距约束；没有偷偷改变道数。");if(s.HeightMax>0)Settings.Require(g.VerticalHeights.Max()<=s.HeightMax+.001,"超过结构限高");
    foreach(var m in g.Network.Curves)foreach(double t in new[]{0.0,.5,1.0}){var pt=m.At(t);if(s.SiteWidth>0)Settings.Require(Math.Abs(pt.X)<=s.SiteWidth/2+.001,"超出横向边界");if(s.AxisLength>0)Settings.Require(pt.Y>=s.SiteOrigin-s.EndMargin-.001&&pt.Y<=s.SiteOrigin+s.AxisLength+s.EndMargin+.001,"超出端部预留");}e.Attempted=true;Solve(e);s.Check();e.Solved=true;
    e.State=e.Warnings.Count>0?"求解有警告，不能列为通过":e.Pass?(s.Strength?"满足所设初筛，非规范安全验算":"位移初筛满足；强度未评估"):"超出所设初筛阈值（不是坍塌判定）";
   }catch(OperationCanceledException){throw;}catch(Exception ex){e.Error=ex.Message;e.State=(e.Attempted?"求解未完成（非坍塌判定）：":"候选约束不满足：")+ex.Message;}return e;
  }
  static void Solve(Evaluation e){
   if(e.Geometry.Network!=null){SolveNetwork(e);return;}
   var g=e.Geometry;var s=e.Settings;int nf=g.Grid.Length,nr=g.Grid[0].Length,ns=(nr-1)*s.Mesh;
   long count=(long)nf*ns+(long)(nf-1)*nr+(g.Profiles[0].Spec.Foot==0?0:2L*nf*s.Mesh);
   Settings.Require(count<=4000,"超过本演示4000梁单元上限，请降低Mesh或模型规模。");
   var k=new Toolkit();double gamma=s.Rho*9.80665/1000;
   // Factory material uses kN/m² and kN/m³. Placeholder strengths are NEVER used for checks.
   var mat=k.Material.IsotropicMaterial("Bamboo demo","User supplied equivalent bamboo",s.E*1000,s.Shear*1000,s.Shear*1000,gamma,s.Strength?s.Ft*1000:1e12,s.Strength?-s.Fc*1000:-1e12,FemMaterial.FlowHypothesis.rankine,0,null);
   var ac=k.CroSec.CircularHollow(s.AD/10,s.AT/10,mat,"Demo","Arch");var pc=k.CroSec.CircularHollow(s.PD/10,s.PT/10,mat,"Demo","Purlin");
   double aa=Area(s.AD/1000,s.AT/1000),pa=Area(s.PD/1000,s.PT/1000),ai=Inertia(s.AD/1000,s.AT/1000),pi=Inertia(s.PD/1000,s.PT/1000);
   Settings.Require(Math.Abs(ac.A/aa-1)<1e-8&&Math.Abs(pc.A/pa-1)<1e-8,"Karamba截面单位不符，停止求解。");
   var lines=new List<KLine>();var ids=new List<string>();var secs=new List<CroSec>();var joints=new List<Joint>();
   Action<Point3d,Point3d,string,bool> add=(a,b,id,arch)=>{Settings.Require(a.DistanceTo(b)>.001,"零长度构件。");lines.Add(new KLine(KP(a),KP(b)));ids.Add(id);secs.Add(arch?ac:pc);e.Members.Add(new Member{A=a,B=b,Id=id,Arch=arch,Area=arch?aa:pa,I=arch?ai:pi});};
   for(int i=0;i<nf;i++){
    for(int j=0;j<ns;j++){string id=$"A{i}_{j}";add(At(g,i,j/(double)ns),At(g,i,(j+1)/(double)ns),id,true);if((s.Joints&1)!=0&&j==ns/2-1){var p=new double?[12];p[10]=p[11]=0;joints.Add(new Joint(p,new List<string>{id},new List<Guid>()));}}
    if(g.Profiles[i].Spec.Foot!=0)for(int side=0;side<2;side++){
     var a=g.Feet[2*i+side];var b=g.Grid[i][side==0?0:nr-1];
     for(int j=0;j<s.Mesh;j++)add(a+(b-a)*(j/(double)s.Mesh),a+(b-a)*((j+1)/(double)s.Mesh),$"L{i}_{side}_{j}",true);
    }
   }
   var cases=new List<string>{"G_Qfull","G_Qleft","G_Qright"};if(s.Snow>0)cases.Add("G_Snow");if(s.W0>0){cases.Add("G_Wpressure");cases.Add("G_Wsuction");}
   var loads=new List<Load>();var expected=cases.ToDictionary(c=>c,c=>Vector3d.Zero);var loadVectors=cases.ToDictionary(c=>c,c=>new Vector3d[nf-1,nr]);
   foreach(var lc in cases)loads.Add(k.Load.GravityLoad(new KVector(0,0,-1),lc));
   double orientation=0;
   for(int i=0;i<nf-1;i++)for(int j=0;j<nr-1;j++){
    var a=g.Grid[i][j];var b=g.Grid[i][j+1];var c=g.Grid[i+1][j+1];var d=g.Grid[i+1][j];var normal=Cell(a,b,c,d);double plan=normal.Z,surface=SurfaceArea(a,b,c,d);
    double sign=Math.Sign(Vector3d.CrossProduct(b-a,c-a).Z);if(orientation==0)orientation=sign;else Settings.Require(sign==orientation,"屋面网格朝向反转。");
    foreach(var lc in cases){
     double live=lc=="G_Qfull"?s.RoofQ:lc=="G_Qleft"&&j<(nr-1)/2?s.RoofQ:lc=="G_Qright"&&j>=(nr-1)/2?s.RoofQ:lc=="G_Snow"?s.Snow:0;
     var force=new Vector3d(0,0,-s.RoofG*surface-live*plan);
     if(lc=="G_Wpressure"||lc=="G_Wsuction")force+=normal*(-s.W0*s.MuZ*s.Beta*(lc=="G_Wpressure"?s.Cp:s.Cs));
     loadVectors[lc][i,j]+=force*.5;loadVectors[lc][i,j+1]+=force*.5;
    }
   }
   for(int i=0;i<nf-1;i++)for(int j=0;j<nr;j++){
    string id=$"P{j}_{i}";var a=g.Grid[i][j];var b=g.Grid[i+1][j];double len=a.DistanceTo(b)*.001;add(a,b,id,false);
    if((s.Joints&2)!=0){var p=new double?[12];p[4]=p[5]=p[10]=p[11]=0;joints.Add(new Joint(p,new List<string>{id},new List<Guid>()));}
    foreach(var lc in cases){var force=loadVectors[lc][i,j];if(force.Length>1e-12){var dir=force;dir.Unitize();loads.Add(k.Load.ConstantForceLoad(KV(dir),force.Length/len,0,1,LoadOrientation.global,lc,id));}expected[lc]+=force;}
   }
   double volume=e.Members.Sum(m=>m.Area*m.A.DistanceTo(m.B)*.001);e.Mass=volume*s.Rho;
   foreach(var lc in cases)expected[lc]+=new Vector3d(0,0,-volume*gamma);
   var supports=g.Feet.Select(p=>k.Support.Support(KP(p),new[]{true,true,true,s.Support==1,s.Support==1,s.Support==1})).ToList();
   var builders=k.Part.LineToBeam(lines,ids,secs,new MessageLogger(),out var nodes,true,1e-6);
   var model=k.Model.AssembleModel(builders.Cast<BuilderElement>().ToList(),supports,loads,out var info,out var mass,out var cog,out var msg,out var warn,joints,null,null,1e-6);
   if(warn)e.Warnings.Add(info+" "+msg);
   if(!Karamba.Licenses.License.license_is_valid(model,out var licenseMsg))throw new Exception("Karamba许可证限制："+licenseMsg);
   Finish(e,k,model,cases,expected,ids);
  }
  static void Finish(Evaluation e,Toolkit k,Karamba.Models.Model model,List<string> cases,Dictionary<string,Vector3d> expected,List<string> ids){
   var s=e.Settings;s.Check();
   var solved=k.Algorithms.Analyze(model,cases,out var maxDisp,out var resultForces,out var energies,out var warning);e.Model=solved;
   if(!string.IsNullOrWhiteSpace(warning))e.Warnings.Add(warning);
   e.Displacement=maxDisp.Max()*1000;e.DisplacementCase="节点包络";e.Ratios=Enumerable.Repeat(0.0,e.Members.Count).ToList();e.Stresses=Enumerable.Repeat(0.0,e.Members.Count).ToList();
   foreach(var lc in cases){
    s.Check();double disp=0;
    BeamDisplacements.solve(solved,ids,lc,.15,8,out var bd,out var br,out var dlc,out var dli,out var di);
    Settings.Require(bd.Count==e.Members.Count,"梁内位移结果不完整。");foreach(var m in bd)foreach(var p in m)foreach(var v in p)disp=Math.Max(disp,Math.Sqrt(v.X*v.X+v.Y*v.Y+v.Z*v.Z)*1000);
    if(disp>=e.Displacement){e.Displacement=disp;e.DisplacementCase=lc;}
    BeamForces.solve(solved,ids,lc,.15,5,out var forces,out var moments,out var fc,out var fi,out var inds);
    Settings.Require(forces.Count==ids.Count,"内力结果不完整。");
    for(int j=0;j<forces.Count;j++){
     int idx=ids.IndexOf(solved.elems[inds[j]].id);Settings.Require(idx>=0,"结果构件ID未对应。");var member=e.Members[idx];double diameter=(member.Diameter>0?member.Diameter:member.Arch?s.AD:s.PD)*.001,w=member.I/(diameter*.5);
     for(int p=0;p<forces[j].Count;p++)for(int z=0;z<forces[j][p].Count;z++){
      var f=forces[j][p][z];var m=moments[j][p][z];double axial=f.X/member.Area/1000,bend=Math.Sqrt(m.Y*m.Y+m.Z*m.Z)/w/1000;
      e.AxialMax=Math.Max(e.AxialMax,Math.Abs(f.X));e.BendingMax=Math.Max(e.BendingMax,Math.Sqrt(m.Y*m.Y+m.Z*m.Z));e.ShearMax=Math.Max(e.ShearMax,Math.Sqrt(f.Y*f.Y+f.Z*f.Z));e.TorsionMax=Math.Max(e.TorsionMax,Math.Abs(m.X));
      double stress=Math.Abs(axial)+bend;if(stress>e.StressMax){e.StressMax=stress;e.StressMember=member.Id;e.StressCase=lc;}e.Stresses[idx]=Math.Max(e.Stresses[idx],stress);
      if(s.Strength)e.Ratios[idx]=Math.Max(e.Ratios[idx],Math.Abs(axial)/(axial<0?s.Fc:s.Ft)+bend/s.Fb);
     }
    }
    Reaction.solve(solved,lc,new List<int>(),out var orientations,out var rf,out var rm,out var sums,out var sumMoments,out var rlc,out var rli);
    Settings.Require(sums.Count>0,"支座反力结果缺失。");foreach(var v in sums)e.Equilibrium=Math.Max(e.Equilibrium,(new Vector3d(v.X,v.Y,v.Z)+expected[lc]).Length/Math.Max(expected[lc].Length,1e-6));
    foreach(var loadCase in rf)foreach(var v in loadCase)e.ReactionMax=Math.Max(e.ReactionMax,Math.Sqrt(v.X*v.X+v.Y*v.Y+v.Z*v.Z));
    e.CaseReports.Add(FormattableString.Invariant($"{lc}: 位移={disp:0.###}mm; 合外力=({expected[lc].X:0.###},{expected[lc].Y:0.###},{expected[lc].Z:0.###})kN"));
    if(s.Buckle){
     try{Karamba.Algorithms.Buckling.solve(solved,1,1,new List<string>{lc},100,1e-8,1,out var factors,out var bmodel,out var bmsg);
      if(!string.IsNullOrWhiteSpace(bmsg))e.Warnings.Add("屈曲 "+lc+": "+bmsg);
      var positive=factors.Where(v=>FMath.IsFinite(v)&&v>0).ToList();if(positive.Count==0)e.Warnings.Add("屈曲 "+lc+" 未取得正特征值");else e.Buckling=double.IsNaN(e.Buckling)?positive.Min():Math.Min(e.Buckling,positive.Min());
     }catch(Exception ex){e.Warnings.Add("屈曲未完成 "+lc+": "+ex.Message);}
    }
   }
   if(s.Strength)e.Ratio=e.Ratios.Max();e.Score=Math.Max(e.Displacement/s.Limit,s.Strength?e.Ratio:0);
   if(s.Buckle&&FMath.IsFinite(e.Buckling))e.Score=Math.Max(e.Score,1/e.Buckling);
   Settings.Require(FMath.IsFinite(e.Score)&&e.Equilibrium<=.001,"数值有效性/合力平衡检查失败："+e.Equilibrium);
   e.Pass=e.Score<=1&&e.Warnings.Count==0;
  }
  static void SolveNetwork(Evaluation e){
   var g=e.Geometry;var net=g.Network;var s=e.Settings;var k=new Toolkit();double gamma=s.Rho*9.80665/1000;
   long count=net.Curves.Sum(m=>(long)(m.Arc||m.Role=="Leg"?s.Mesh:1));Settings.Require(count<=4000,"新原型超过4000梁单元，请减少Mesh、Panels、檩条或榀数。");
   var mat=k.Material.IsotropicMaterial("Bamboo demo","Equivalent bamboo prototype",s.E*1000,s.Shear*1000,s.Shear*1000,gamma,s.Strength?s.Ft*1000:1e12,s.Strength?-s.Fc*1000:-1e12,FemMaterial.FlowHypothesis.rankine,0,null);
   var ac=k.CroSec.CircularHollow(s.AD/10,s.AT/10,mat,"Demo","Arch and web");var pc=k.CroSec.CircularHollow(s.PD/10,s.PT/10,mat,"Demo","Purlin");
   double aa=Area(s.AD/1000,s.AT/1000),pa=Area(s.PD/1000,s.PT/1000),ai=Inertia(s.AD/1000,s.AT/1000),pi=Inertia(s.PD/1000,s.PT/1000);
   Settings.Require(Math.Abs(ac.A/aa-1)<1e-8&&Math.Abs(pc.A/pa-1)<1e-8,"Karamba截面单位不符。");
   var lines=new List<KLine>();var ids=new List<string>();var sections=new List<CroSec>();var joints=new List<Joint>();
   foreach(var m in net.Curves){
    double d=s.DiameterFor(m,net.Spec),th=s.WallFor(m,net.Spec);var section=k.CroSec.CircularHollow(d/10,th/10,mat,"Demo",m.Role);bool arch=m.Role!="Purlin";int divisions=m.Arc||m.Role=="Leg"?s.Mesh:1;
    for(int j=0;j<divisions;j++){
     string id=m.Id+"_"+j;var a=TopologyRhino.Point(m.At(j/(double)divisions));var b=TopologyRhino.Point(m.At((j+1)/(double)divisions));
     Settings.Require(a.DistanceTo(b)>.001,"零长度梁："+id);lines.Add(new KLine(KP(a),KP(b)));ids.Add(id);sections.Add(section);e.Members.Add(new Member{Id=id,Role=m.Role,A=a,B=b,Arch=arch,Diameter=d,Area=Area(d/1000,th/1000),I=Inertia(d/1000,th/1000)});
     if(arch&&(s.Joints&1)!=0&&((m.CrownEnd&&j==divisions-1)||(m.StartAtCrown&&j==0))){var release=new double?[12];if(m.CrownEnd&&j==divisions-1)release[10]=release[11]=0;if(m.StartAtCrown&&j==0)release[4]=release[5]=0;joints.Add(new Joint(release,new List<string>{id},new List<Guid>()));}
     if(!arch&&(s.Joints&2)!=0){var release=new double?[12];release[4]=release[5]=release[10]=release[11]=0;joints.Add(new Joint(release,new List<string>{id},new List<Guid>()));}
    }
   }
   var cases=new List<string>{"G_Qfull","G_Qleft","G_Qright"};if(s.Snow>0)cases.Add("G_Snow");if(s.W0>0&&s.WindModel==0){cases.Add("G_Wpressure");cases.Add("G_Wsuction");}var winds=WindLoads.Build(net,s);cases.AddRange(winds.Select(w=>w.Id));
   var loads=new List<Load>();var expected=cases.ToDictionary(c=>c,c=>Vector3d.Zero);
   var railMembers=net.Curves.Where(m=>m.Role=="Purlin").ToDictionary(m=>m.Id);
   var railLoads=railMembers.Keys.ToDictionary(id=>id,id=>cases.ToDictionary(c=>c,c=>new P3(0,0,0)));
   foreach(var tri in net.Roof){
    var n=tri.AreaVector;double plan=n.Z,area=n.Length;
    var targets=new[]{tri.LoadRailA,tri.LoadRailB}.Where(id=>id!=null).Distinct().ToList();
    foreach(var id in targets){
     foreach(var lc in cases){
      double q=lc=="G_Qfull"?s.RoofQ:lc=="G_Qleft"&&tri.Left?s.RoofQ:lc=="G_Qright"&&!tri.Left?s.RoofQ:lc=="G_Snow"?s.Snow:0;
      var force=new P3(0,0,-s.RoofG*area-q*plan);
      if(lc=="G_Wpressure"||lc=="G_Wsuction")force=force+n*(-s.W0*s.MuZ*s.Beta*(lc=="G_Wpressure"?s.Cp:s.Cs));
      railLoads[id][lc]=railLoads[id][lc]+force*(1.0/targets.Count);
     }
    }
   }
   foreach(var lc in cases){
    loads.Add(k.Load.GravityLoad(new KVector(0,0,-1),lc));
    foreach(var pair in railMembers){
     var f=railLoads[pair.Key][lc];if(f.Length<=1e-12)continue;var direction=new Vector3d(f.X,f.Y,f.Z);direction.Unitize();
     // Roof strips load their bounding real purlin spans, preserving purlin bending under roof loads.
     double length=pair.Value.A.Distance(pair.Value.B)*.001;
     loads.Add(k.Load.ConstantForceLoad(KV(direction),f.Length/length,0,1,LoadOrientation.global,lc,pair.Key+"_0"));expected[lc]+=new Vector3d(f.X,f.Y,f.Z);
    }
   }
   foreach(var wc in winds)foreach(var face in wc.Faces){var force=face.Force*(1.0/3);foreach(var pt in new[]{face.A,face.B,face.C}){
    loads.Add(k.Load.PointLoad(KP(TopologyRhino.Point(pt)),new KVector(force.X,force.Y,force.Z),new KVector(0,0,0),wc.Id));expected[wc.Id]+=new Vector3d(force.X,force.Y,force.Z);
   }}
   double volume=e.Members.Sum(m=>m.Area*m.A.DistanceTo(m.B)*.001);e.Mass=volume*s.Rho;
   foreach(var lc in cases)expected[lc]+=new Vector3d(0,0,-volume*gamma);
   var supports=g.Feet.Select(p=>k.Support.Support(KP(p),new[]{true,true,true,s.Support==1,s.Support==1,s.Support==1})).ToList();
   var builders=k.Part.LineToBeam(lines,ids,sections,new MessageLogger(),out var nodes,true,1e-6);
   var model=k.Model.AssembleModel(builders.Cast<BuilderElement>().ToList(),supports,loads,out var info,out var mass,out var cog,out var msg,out var warn,joints,null,null,1e-6);
   if(warn)e.Warnings.Add(info+" "+msg);if(!Karamba.Licenses.License.license_is_valid(model,out var why))throw new Exception("Karamba许可证限制："+why);
   Finish(e,k,model,cases,expected,ids);
  }
  public static Evaluation Best(IEnumerable<Evaluation> values)=>values.Where(e=>e.Solved&&e.Warnings.Count==0).OrderBy(e=>e.Pass?0:1).ThenBy(e=>e.Pass?e.Mass:e.Score).ThenBy(e=>e.Score).FirstOrDefault();
  public static Evaluation Worst(IEnumerable<Evaluation> values)=>values.Where(e=>e.Solved&&e.Warnings.Count==0).OrderByDescending(e=>e.Score).FirstOrDefault();
  public static string Scope=>"一阶线弹性等效圆管梁；无屋面壳刚度。未完成二阶/缺陷、连接滑移、束竹组合效率、剪扭强度、局部破坏、基础及施工阶段验算。屈曲开关仅作线性特征值诊断；不开启时稳定性未评估。没有规范全工况组合；本报告不能判断真实坍塌或施工安全。";
 }
}

namespace Bamboo45 {
using BambooStages123;



using System;
using System.Linq;
using System.Collections.Generic;

// User-defined net pressures on oriented triangles. This is a load model, not CFD
// or an automatic code zoning procedure. Geometry is in mm, pressure in kN/m².
public sealed class WindFace {
 public P3 A,B,C; public string Surface,Zone; public double Coefficient,Pressure;
 public P3 AreaVector=>P3.Cross((B-A)*.001,(C-A)*.001)*.5;
 public P3 Centroid=>(A+B+C)*(1.0/3);
 public P3 Force=>AreaVector*(-Pressure); // kN; positive net pressure is inward
}
public sealed class WindCase {
 public string Id,Label; public double Angle; public P3 Direction; public List<WindFace> Faces=new();
 public P3 Force=>Faces.Aggregate(new P3(),(sum,f)=>sum+f.Force);
 public P3 Moment=>Faces.Aggregate(new P3(),(sum,f)=>sum+P3.Cross(f.Centroid*.001,f.Force));
}
public static class WindLoads {
 public const string Scope="风向分区为研究简化：按外法向与水平吹向的点积划分迎风、背风、平行区，非规范边角分区或流场模拟。p=W0×MuZ×Beta×Cnet，按实际三角面面积沿法向作用；正值向内，负值向外。各区净压系数由用户给定，演示值不代表项目或规范取值。敞开屋面应使用上下表面合成的净压系数；端面开启时仍须自行考虑内外压差，不自动计算内压。端面仅承受荷载，不增加刚度或自重；未计侧墙、裸杆风阻、切向摩擦和遮挡效应。均布三角面风荷载等分到三顶点，保持合力与力矩；荷载传递仍是简化。风压为0时不生成任何风工况。";
 public static double Angle(Settings s)=>s.WindDirection switch {0=>0,1=>180,2=>90,3=>270,_=>s.WindAngle%360};
 public static P3 Vector(double angle){double a=angle*Math.PI/180;return new P3(Math.Cos(a),Math.Sin(a),0);}
 public static string Zone(P3 areaVector,P3 direction){double dot=P3.Dot(areaVector,direction)/areaVector.Length;return dot < -1e-6?"windward":dot > 1e-6?"leeward":"parallel";}
 static string Degrees(double a)=>a.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+"°";
 public static List<WindCase> Build(StructurePlan net,Settings s){
  var result=new List<WindCase>();if(s.WindModel!=1||s.W0==0)return result;
  var surfaces=net.Roof.Select(t=>new WindFace{A=t.A,B=t.B,C=t.C,Surface="roof"}).ToList();
  if(s.WindEnds==1){AddEnd(surfaces,net,0,-1);AddEnd(surfaces,net,net.RoofGrid.Count-1,1);}
  foreach(var angle in s.WindReverse?new[]{Angle(s),(Angle(s)+180)%360}:new[]{Angle(s)}){
   var direction=Vector(angle);var wc=new WindCase{Id="G_Wdir_"+Degrees(angle),Label="吹向 "+Degrees(angle)+" · "+(s.WindEnds==0?"敞开屋面":"屋面＋两端荷载面"),Angle=angle,Direction=direction};
   foreach(var tri in surfaces){if(tri.AreaVector.Length<1e-10)continue;string zone=Zone(tri.AreaVector,direction);bool roof=tri.Surface=="roof";
    double coefficient=zone=="windward"?(roof?s.WindRoofWindward:s.WindEndWindward):zone=="leeward"?(roof?s.WindRoofLeeward:s.WindEndLeeward):(roof?s.WindRoofParallel:s.WindEndParallel);
    wc.Faces.Add(new WindFace{A=tri.A,B=tri.B,C=tri.C,Surface=tri.Surface,Zone=zone,Coefficient=coefficient,Pressure=s.W0*s.MuZ*s.Beta*coefficient});
   }result.Add(wc);
  }return result;
 }
 static void AddEnd(List<WindFace> faces,StructurePlan net,int rowIndex,int sign){
  var row=net.RoofGrid[rowIndex];
  int left=net.Maps.FindIndex(m=>m.At(0).Distance(row[0])<.001),right=net.Maps.FindIndex(m=>m.At(1).Distance(row[row.Count-1])<.001);
  Settings.Require(left>=0&&right>=0,"端面边界未能对应拱脚");
  // Resolve end boundaries from their own rib maps; A2 has six feet per map.
  var lm=net.Maps[left];var rm=net.Maps[right];
  var lp=lm.Outer.Spec.CompoundPointed?lm.Outer.Return.B:lm.Outer.LeftFoot;
  var rp=rm.Outer.Spec.CompoundPointed?new V2(-rm.Outer.Return.B.X,rm.Outer.Return.B.Z):rm.Outer.RightFoot;
  var polygon=new List<P3>{lm.Map(lp)};polygon.AddRange(row);polygon.Add(rm.Map(rp));
  var outward=net.Maps[sign<0?left:right].Frame.Forward*sign;
  for(int i=1;i<polygon.Count-1;i++){
   var f=new WindFace{A=polygon[0],B=polygon[i],C=polygon[i+1],Surface=sign<0?"front-end":"back-end"};
   if(f.AreaVector.Length<1e-10)continue;if(P3.Dot(f.AreaVector,outward)<0)(f.B,f.C)=(f.C,f.B);faces.Add(f);
  }
 }
 public static object Pack(WindCase wc){
  object Point(P3 p)=>new{x=p.X,y=p.Y,z=p.Z};
  return new{id=wc.Id,label=wc.Label,angle=wc.Angle,direction=Point(wc.Direction),forceKN=Point(wc.Force),momentKNm=Point(wc.Moment),
   zones=wc.Faces.GroupBy(f=>(f.Surface,f.Zone)).Select(g=>new{surface=g.Key.Surface,zone=g.Key.Zone,areaM2=g.Sum(f=>f.AreaVector.Length),coefficient=g.First().Coefficient,pressureKNm2=g.First().Pressure}),
   faces=wc.Faces.Select(f=>new{points=new[]{Point(f.A),Point(f.B),Point(f.C)},surface=f.Surface,zone=f.Zone,pressure=f.Pressure,forceKN=Point(f.Force)})};
 }
}

}namespace BambooCompare {
using System;
using System.Linq;
using System.Collections.Generic;
using BambooStages123;


 public sealed class CompareMetric {
  public string Id,Reason="";public int Type;
  public bool Solved,Warning,Pass,Allowed=true;
  public double Factor,Diameter,Score,Mass,Stress,Displacement,PlanArea,SurfaceArea;
  public bool Eligible=>Allowed&&LocallyEligible;
  public bool LocallyEligible=>Solved&&!Warning&&FMath.IsFinite(Score)&&Score>=0&&FMath.IsFinite(Mass)&&Mass>0;
 }
 public static class ComparisonRules {
  public static readonly string[] Names={"单圆拱","单尖拱","交叉拱","侧部交叉尖拱","复合圆拱","复合尖拱"};
  public static int Code(Spec s)=>s.Structure*2+(s.Shape==1?0:1);
  public static Spec WithType(Spec source,int code){MathRules.Require(code>=0&&code<6,"原型编号须为0..5。");var s=source.Copy();s.Structure=code/2;s.Shape=code%2==0?1:0;return s;}
  public static List<int> Types(IEnumerable<int> codes){var result=codes.Distinct().OrderBy(x=>x).ToList();MathRules.Require(result.Count>0&&result.All(x=>x>=0&&x<6),"TypeSet至少包含一个0..5原型编号。");return result;}
  public static List<double> Factors(double lo,double hi,int samples){
   MathRules.Require(FMath.IsFinite(lo)&&FMath.IsFinite(hi)&&lo>0&&hi>=lo&&samples>=2&&samples<=9,"Hmin/Hmax或Samples无效。");
   var r=new List<double>();for(int i=0;i<samples;i++){double f=lo+(hi-lo)*i/(samples-1);if(r.All(x=>Math.Abs(x-f)>1e-9))r.Add(f);}
   if(lo<=1&&hi>=1&&r.All(x=>Math.Abs(x-1)>1e-9))r.Add(1);return r.OrderBy(x=>x).ToList();
  }
  public static bool AreaAllowed(double candidate,double baseline,double tolerance)=>FMath.IsFinite(candidate)&&FMath.IsFinite(baseline)&&candidate>0&&baseline>0&&FMath.IsFinite(tolerance)&&tolerance>=0&&Math.Abs(candidate/baseline-1)<=tolerance+1e-9;
  public static CompareMetric Best(IEnumerable<CompareMetric> values)=>values.Where(m=>m.Eligible&&m.Pass).OrderBy(m=>m.Pass?0:1).ThenBy(m=>m.Pass?m.Mass:m.Score).ThenBy(m=>m.Pass?m.Score:m.Mass).ThenBy(m=>m.Id,StringComparer.Ordinal).FirstOrDefault();
  public static CompareMetric Worst(IEnumerable<CompareMetric> values)=>values.Where(m=>m.Eligible).OrderByDescending(m=>m.Score).ThenByDescending(m=>m.Mass).ThenBy(m=>m.Id,StringComparer.Ordinal).FirstOrDefault();
  public static List<CompareMetric> Rank(IEnumerable<CompareMetric> values)=>values.Where(m=>m.Eligible).OrderBy(m=>m.Pass?0:1).ThenBy(m=>m.Pass?m.Mass:m.Score).ThenBy(m=>m.Pass?m.Score:m.Mass).ThenBy(m=>m.Id,StringComparer.Ordinal).ToList();
 }
}
namespace BambooCompare {
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using BambooStages123;


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
   MathRules.Require(values.All(x=>FMath.IsFinite(x)&&x>=min&&x<=max&&(!integer||x==Math.Truncate(x))&&(!even||x%2==0)),name+"超出允许范围"+(even?"，且必须为偶数":integer?"，且必须为整数":"")+"。");
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
namespace BambooCompare {
using Rhino.Geometry;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using BambooStages123;
using Bamboo45;


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
  public static ComparisonResult Search(Spec basis,List<Plane> planes,List<double> heights,int rails,Settings settings,List<int> types,List<double> factors,List<double> diameters,double tolerance,double areaTol,Evaluation baseline,CandidateSpace space,int maximum,Func<string,GeometrySet,Settings,Evaluation> solve=null){
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
  static Evaluation EvaluateCandidate(Spec spec,List<Plane> planes,List<double> heights,int rails,double factor,Settings settings,double tolerance,Func<string,GeometrySet,Settings,Evaluation> solve){
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
namespace BambooExchange {
using System;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Drawing;
using Rhino.Geometry;
using BambooStages123;
using Bamboo45;
using BambooCompare;
public sealed class Packet {public Dictionary<string,double> Before,After;public double[] Stations;}
public static class Exchange {
static string F(double v)=>v.ToString("R",CultureInfo.InvariantCulture);
static double Number(string x){double v=double.Parse(x,CultureInfo.InvariantCulture);Settings.Require(!double.IsNaN(v)&&!double.IsInfinity(v),"参数必须为有限数值");return v;}
public static readonly string[] Keys="type W H Foot LegH LegAngle CrossAngle TrussDepth Panels PointedVersion PointE PointSlope PointFootGap PointPanels PointLowerD PointLowerT PointWebD PointWebT SiteL SiteW SiteH SiteEnd L S End PurlinCount ArchD ArchT PurlinD PurlinT E Nu G Density Fc Ft Fb RoofG RoofQ W0 MuZ Beta Cp Cs SnowQ WindModel WindDirection WindEnds WindReverse WindAngle WindRoofWindward WindRoofLeeward WindRoofParallel WindEndWindward WindEndLeeward WindEndParallel Support Joints Buckle Mesh Limit Pmax".Split(' ');
public static Packet Parse(string text){
 var lines=text.Replace("\r","").Split('\n').Select(x=>x.Trim()).Where(x=>x.Length>0).ToArray();Settings.Require(lines.Length>2&&lines[0]=="BAMBOO_GH_EDIT_V1","请使用本版App导出的完整GH参数包");
 var sections=new Dictionary<string,Dictionary<string,double>>();var stations=new Dictionary<string,double[]>();string block="";
 foreach(var line in lines.Skip(1)){if(line=="[Before]"||line=="[After]"){block=line;Settings.Require(!sections.ContainsKey(block),"参数包区段重复");sections[block]=new Dictionary<string,double>();continue;}
  Settings.Require(sections.ContainsKey(block),"缺少Before/After区段");var pair=line.Split('=');Settings.Require(pair.Length==2,"参数包行必须为key=value");string key=pair[0];
  if(key=="Stations"){Settings.Require(!stations.ContainsKey(block),"Stations重复");stations[block]=pair[1].Split(',').Select(Number).ToArray();}
  else{Settings.Require(Keys.Contains(key)&&!sections[block].ContainsKey(key),"未知或重复参数："+key);sections[block][key]=Number(pair[1]);}
 }
 Settings.Require(sections.ContainsKey("[Before]")&&sections.ContainsKey("[After]")&&stations.Count==2,"缺少修改前/后或站位");
 var before=sections["[Before]"];var after=sections["[After]"];Settings.Require(Keys.All(k=>before.ContainsKey(k)&&after.ContainsKey(k)),"参数包不完整");
 Settings.Require(before["type"]==after["type"],"修改前后必须为同构型");Settings.Require(stations["[Before]"].SequenceEqual(stations["[After]"]),"方案编辑不改变拱架站位");
 foreach(string k in new[]{"SiteL","SiteW","SiteH","SiteEnd","L","S","End","PointedVersion"})Settings.Require(before[k]==after[k],"编辑期间固定参数不可变："+k);
 Validate(before);Validate(after);var ys=stations["[Before]"];Settings.Require(ys.Length>=2&&ys.Length<=48&&ys.All(v=>v>=0&&v<=before["SiteL"]+.001),"站位数量或范围无效");for(int i=1;i<ys.Length;i++)Settings.Require(ys[i]>ys[i-1],"站位必须沿Y递增");
 return new Packet{Before=before,After=after,Stations=ys};
}
public static string Key(Dictionary<string,double> p,double[] stations)=>string.Join(";",Keys.Select(k=>k+"="+F(p[k])))+";Stations="+string.Join(",",stations.Select(F));
public static string Encode(Dictionary<string,double> p,double[] stations){var lines=string.Join("\n",Keys.Select(k=>k+"="+F(p[k])))+"\nStations="+string.Join(",",stations.Select(F));return "BAMBOO_GH_EDIT_V1\n[Before]\n"+lines+"\n[After]\n"+lines;}
public static Dictionary<string,double> Edit(Dictionary<string,double> basis,string text){var p=new Dictionary<string,double>(basis);var seen=new HashSet<string>();foreach(var line in text.Split(new[]{'\r','\n',';'},StringSplitOptions.RemoveEmptyEntries)){if(line.TrimStart().StartsWith("#"))continue;var a=line.Split('=');Settings.Require(a.Length==2,"Overrides使用每行key=value；清空即可恢复原参数");string key=a[0].Trim();Settings.Require(Allowed((int)p["type"]).Contains(key)&&seen.Add(key),"此构型不支持、锁定或重复的编辑项："+key);p[key]=Number(a[1]);}Validate(p);return p;}
public static string[] Allowed(int type){switch(type){case 0:return "W H Foot ArchD ArchT PurlinCount PurlinD PurlinT E Nu Density G Fc Ft Fb RoofG RoofQ W0 MuZ Beta SnowQ WindModel Cp Cs Support Joints Buckle Mesh Limit Pmax WindDirection WindEnds WindReverse WindRoofWindward WindRoofLeeward WindRoofParallel WindEndWindward WindEndLeeward WindEndParallel WindAngle LegH LegAngle".Split(' ');case 1:return "W H Foot ArchD ArchT PurlinCount PurlinD PurlinT E Nu Density G Fc Ft Fb RoofG RoofQ W0 MuZ Beta SnowQ WindModel Cp Cs Support Joints Buckle Mesh Limit Pmax WindDirection WindEnds WindReverse WindRoofWindward WindRoofLeeward WindRoofParallel WindEndWindward WindEndLeeward WindEndParallel WindAngle LegH LegAngle".Split(' ');case 2:return "W H Foot CrossAngle ArchD ArchT PurlinCount PurlinD PurlinT E Nu Density G Fc Ft Fb RoofG RoofQ W0 MuZ Beta SnowQ WindModel Cp Cs Support Joints Buckle Mesh Limit Pmax WindDirection WindEnds WindReverse WindRoofWindward WindRoofLeeward WindRoofParallel WindEndWindward WindEndLeeward WindEndParallel WindAngle LegH LegAngle".Split(' ');case 4:return "W H Foot TrussDepth Panels ArchD ArchT PurlinCount PurlinD PurlinT E Nu Density G Fc Ft Fb RoofG RoofQ W0 MuZ Beta SnowQ WindModel Cp Cs Support Joints Buckle Mesh Limit Pmax WindDirection WindEnds WindReverse WindRoofWindward WindRoofLeeward WindRoofParallel WindEndWindward WindEndLeeward WindEndParallel WindAngle LegH LegAngle".Split(' ');case 5:return "W H PointE PointSlope PointFootGap PointPanels ArchD ArchT PointLowerD PointLowerT PointWebD PointWebT PurlinCount PurlinD PurlinT E Nu Density G Fc Ft Fb RoofG RoofQ W0 MuZ Beta SnowQ WindModel Cp Cs Support Joints Buckle Mesh Limit Pmax WindDirection WindEnds WindReverse WindRoofWindward WindRoofLeeward WindRoofParallel WindEndWindward WindEndLeeward WindEndParallel WindAngle".Split(' '); default:throw new ArgumentException("不支持此构型");}}
public static void Validate(Dictionary<string,double> p){Settings.Require(new[]{0.0,1,2,4,5}.Contains(p["type"]),"类型必须为0/1/2/4/5");foreach(string k in Keys)Settings.Require(!double.IsNaN(p[k])&&!double.IsInfinity(p[k]),"非有限参数："+k);
 Settings.Require(p["W"]>=2000&&p["W"]<=12000,"W超出范围或不是合法整数");
Settings.Require(p["H"]>=500&&p["H"]<=12000,"H超出范围或不是合法整数");
Settings.Require(p["Foot"]>=0&&p["Foot"]<=2&&p["Foot"]==Math.Truncate(p["Foot"]),"Foot超出范围或不是合法整数");
Settings.Require(p["ArchD"]>=40&&p["ArchD"]<=400,"ArchD超出范围或不是合法整数");
Settings.Require(p["ArchT"]>=1&&p["ArchT"]<=80,"ArchT超出范围或不是合法整数");
Settings.Require(p["PurlinCount"]>=3&&p["PurlinCount"]<=61&&p["PurlinCount"]==Math.Truncate(p["PurlinCount"]),"PurlinCount超出范围或不是合法整数");
Settings.Require(p["PurlinD"]>=40&&p["PurlinD"]<=300,"PurlinD超出范围或不是合法整数");
Settings.Require(p["PurlinT"]>=1&&p["PurlinT"]<=60,"PurlinT超出范围或不是合法整数");
Settings.Require(p["E"]>=1000&&p["E"]<=30000,"E超出范围或不是合法整数");
Settings.Require(p["Nu"]>=0&&p["Nu"]<=0.49,"Nu超出范围或不是合法整数");
Settings.Require(p["Density"]>=100&&p["Density"]<=1500,"Density超出范围或不是合法整数");
Settings.Require(p["G"]>=0&&p["G"]<=30000,"G超出范围或不是合法整数");
Settings.Require(p["Fc"]>=0&&p["Fc"]<=300,"Fc超出范围或不是合法整数");
Settings.Require(p["Ft"]>=0&&p["Ft"]<=300,"Ft超出范围或不是合法整数");
Settings.Require(p["Fb"]>=0&&p["Fb"]<=300,"Fb超出范围或不是合法整数");
Settings.Require(p["RoofG"]>=0&&p["RoofG"]<=3,"RoofG超出范围或不是合法整数");
Settings.Require(p["RoofQ"]>=0&&p["RoofQ"]<=3,"RoofQ超出范围或不是合法整数");
Settings.Require(p["W0"]>=0&&p["W0"]<=3,"W0超出范围或不是合法整数");
Settings.Require(p["MuZ"]>=0.1&&p["MuZ"]<=5,"MuZ超出范围或不是合法整数");
Settings.Require(p["Beta"]>=0.1&&p["Beta"]<=5,"Beta超出范围或不是合法整数");
Settings.Require(p["SnowQ"]>=0&&p["SnowQ"]<=3,"SnowQ超出范围或不是合法整数");
Settings.Require(p["WindModel"]>=0&&p["WindModel"]<=1&&p["WindModel"]==Math.Truncate(p["WindModel"]),"WindModel超出范围或不是合法整数");
Settings.Require(p["Cp"]>=0&&p["Cp"]<=5,"Cp超出范围或不是合法整数");
Settings.Require(p["Cs"]>=-5&&p["Cs"]<=0,"Cs超出范围或不是合法整数");
Settings.Require(p["Support"]>=0&&p["Support"]<=1&&p["Support"]==Math.Truncate(p["Support"]),"Support超出范围或不是合法整数");
Settings.Require(p["Joints"]>=0&&p["Joints"]<=3&&p["Joints"]==Math.Truncate(p["Joints"]),"Joints超出范围或不是合法整数");
Settings.Require(p["Buckle"]>=0&&p["Buckle"]<=1&&p["Buckle"]==Math.Truncate(p["Buckle"]),"Buckle超出范围或不是合法整数");
Settings.Require(p["Mesh"]>=1&&p["Mesh"]<=4&&p["Mesh"]==Math.Truncate(p["Mesh"]),"Mesh超出范围或不是合法整数");
Settings.Require(p["Limit"]>=0&&p["Limit"]<=1000,"Limit超出范围或不是合法整数");
Settings.Require(p["Pmax"]>=0&&p["Pmax"]<=3000,"Pmax超出范围或不是合法整数");
Settings.Require(p["WindDirection"]>=0&&p["WindDirection"]<=4&&p["WindDirection"]==Math.Truncate(p["WindDirection"]),"WindDirection超出范围或不是合法整数");
Settings.Require(p["WindEnds"]>=0&&p["WindEnds"]<=1&&p["WindEnds"]==Math.Truncate(p["WindEnds"]),"WindEnds超出范围或不是合法整数");
Settings.Require(p["WindReverse"]>=0&&p["WindReverse"]<=1&&p["WindReverse"]==Math.Truncate(p["WindReverse"]),"WindReverse超出范围或不是合法整数");
Settings.Require(p["WindRoofWindward"]>=-5&&p["WindRoofWindward"]<=5,"WindRoofWindward超出范围或不是合法整数");
Settings.Require(p["WindRoofLeeward"]>=-5&&p["WindRoofLeeward"]<=5,"WindRoofLeeward超出范围或不是合法整数");
Settings.Require(p["WindRoofParallel"]>=-5&&p["WindRoofParallel"]<=5,"WindRoofParallel超出范围或不是合法整数");
Settings.Require(p["WindEndWindward"]>=-5&&p["WindEndWindward"]<=5,"WindEndWindward超出范围或不是合法整数");
Settings.Require(p["WindEndLeeward"]>=-5&&p["WindEndLeeward"]<=5,"WindEndLeeward超出范围或不是合法整数");
Settings.Require(p["WindEndParallel"]>=-5&&p["WindEndParallel"]<=5,"WindEndParallel超出范围或不是合法整数");
Settings.Require(p["WindAngle"]>=0&&p["WindAngle"]<=360,"WindAngle超出范围或不是合法整数");
Settings.Require(p["LegH"]>=0&&p["LegH"]<=6000,"LegH超出范围或不是合法整数");
Settings.Require(p["LegAngle"]>=0&&p["LegAngle"]<=40,"LegAngle超出范围或不是合法整数");
Settings.Require(p["CrossAngle"]>=5&&p["CrossAngle"]<=45,"CrossAngle超出范围或不是合法整数");
Settings.Require(p["TrussDepth"]>=100&&p["TrussDepth"]<=1500,"TrussDepth超出范围或不是合法整数");
Settings.Require(p["Panels"]>=4&&p["Panels"]<=24&&p["Panels"]==Math.Truncate(p["Panels"]),"Panels超出范围或不是合法整数");
Settings.Require(p["PointE"]>=50&&p["PointE"]<=2000,"PointE超出范围或不是合法整数");
Settings.Require(p["PointSlope"]>=0.35&&p["PointSlope"]<=0.95,"PointSlope超出范围或不是合法整数");
Settings.Require(p["PointFootGap"]>=0&&p["PointFootGap"]<=500,"PointFootGap超出范围或不是合法整数");
Settings.Require(p["PointPanels"]>=6&&p["PointPanels"]<=12&&p["PointPanels"]==Math.Truncate(p["PointPanels"]),"PointPanels超出范围或不是合法整数");
Settings.Require(p["PointLowerD"]>=40&&p["PointLowerD"]<=400,"PointLowerD超出范围或不是合法整数");
Settings.Require(p["PointLowerT"]>=1&&p["PointLowerT"]<=80,"PointLowerT超出范围或不是合法整数");
Settings.Require(p["PointWebD"]>=30&&p["PointWebD"]<=300,"PointWebD超出范围或不是合法整数");
Settings.Require(p["PointWebT"]>=1&&p["PointWebT"]<=60,"PointWebT超出范围或不是合法整数");
 Settings.Require(p["PurlinCount"]%2==1,"檩条道数必须为奇数");Settings.Require(p["Panels"]%2==0,"复合圆拱分格必须为偶数");Settings.Require(p["PointedVersion"]==3,"本版参数包使用A2几何版本3");
 Settings.Require(p["SiteL"]>=2000&&p["SiteL"]<=42000&&p["SiteW"]>=2000&&p["SiteW"]<=30000&&p["SiteH"]>=1500&&p["SiteH"]<=16000,"场地边界无效");
 Settings.Require(p["SiteEnd"]>=0&&p["SiteEnd"]<=6000&&p["L"]>=2000&&Math.Abs(p["SiteL"]-2*p["SiteEnd"]-p["L"])<.001,"场地与阵列长度不一致");Settings.Require(p["S"]>=500&&p["S"]<=6000&&p["S"]<=p["L"]&&(p["End"]==0||p["End"]==1),"固定站位参数无效");
 Settings.Require(p["W"]<=p["SiteW"]&&p["H"]<=p["SiteH"],"尺寸超出场地边界");SettingsFor(p).Validate();StructureRules.Validate(SpecFor(p),.001);
}
public static Spec SpecFor(Dictionary<string,double> p)=>new Spec{Shape=(int)p["type"]%2==0?1:0,Structure=(int)p["type"]/2,Units=2,W=p["W"],H=p["H"],Foot=(int)p["Foot"],LegH=p["Foot"]==0?0:p["LegH"],Angle=p["Foot"]==2?p["LegAngle"]:0,CrossAngle=p["CrossAngle"],Depth=p["TrussDepth"],Panels=(int)p["Panels"],PointedVersion=3,PointE=p["PointE"],PointSlope=p["PointSlope"],PointFootGap=p["PointFootGap"],PointPanels=(int)p["PointPanels"],PointLowerD=p["PointLowerD"],PointWebD=p["PointWebD"]};
public static Settings SettingsFor(Dictionary<string,double> p)=>new Settings{HeightMax=p["SiteH"],SiteWidth=p["SiteW"],AxisLength=p["L"],EndMargin=p["SiteEnd"],SiteOrigin=p["SiteEnd"],LD=p["PointLowerD"],LT=p["PointLowerT"],WD=p["PointWebD"],WT=p["PointWebT"],AD=p["ArchD"],AT=p["ArchT"],PD=p["PurlinD"],PT=p["PurlinT"],Rho=p["Density"],Snow=p["SnowQ"],E=p["E"],Nu=p["Nu"],G=p["G"],Fc=p["Fc"],Ft=p["Ft"],Fb=p["Fb"],RoofG=p["RoofG"],RoofQ=p["RoofQ"],W0=p["W0"],MuZ=p["MuZ"],Beta=p["Beta"],Cp=p["Cp"],Cs=p["Cs"],WindModel=(int)p["WindModel"],WindDirection=(int)p["WindDirection"],WindEnds=(int)p["WindEnds"],WindReverse=p["WindReverse"]!=0,WindAngle=p["WindAngle"],WindRoofWindward=p["WindRoofWindward"],WindRoofLeeward=p["WindRoofLeeward"],WindRoofParallel=p["WindRoofParallel"],WindEndWindward=p["WindEndWindward"],WindEndLeeward=p["WindEndLeeward"],WindEndParallel=p["WindEndParallel"],Support=(int)p["Support"],Joints=(int)p["Joints"],Mesh=(int)p["Mesh"],Pmax=p["Pmax"],Buckle=p["Buckle"]!=0,Limit=p["Limit"]>0?p["Limit"]:p["W"]/250};
public static GeometrySet Geometry(Dictionary<string,double> p,double[] ys){var frames=ys.Select(y=>new Plane(new Point3d(0,y,0),Vector3d.XAxis,Vector3d.ZAxis)).ToList();return Engine.Geometry(SpecFor(p),frames,ys.Select(_=>p["H"]).ToList(),(int)p["PurlinCount"],1,.001);}
public static string FromSelection(TypeCandidate x,double[] site,int count,double requestedLimit){var sp=x.Spec;var s=x.Eval.Settings;var ys=x.Eval.Geometry.Frames.Select(f=>f.OriginY).ToArray();var p=new Dictionary<string,double>{{"type",x.Metric.Type},{"W",sp.W},{"H",x.Eval.Geometry.VerticalHeights[0]},{"Foot",sp.Foot},{"LegH",sp.LegH},{"LegAngle",sp.Angle},{"CrossAngle",sp.CrossAngle},{"TrussDepth",sp.Depth},{"Panels",sp.Panels},{"PointedVersion",sp.PointedVersion},{"PointE",sp.PointE},{"PointSlope",sp.PointSlope},{"PointFootGap",sp.PointFootGap},{"PointPanels",sp.PointPanels},{"PointLowerD",s.LD},{"PointLowerT",s.LT},{"PointWebD",s.WD},{"PointWebT",s.WT},{"SiteL",site[0]},{"SiteW",site[1]},{"SiteH",site[2]},{"SiteEnd",site[3]},{"L",site[0]-2*site[3]},{"S",ys[1]-ys[0]},{"End",1},{"PurlinCount",count},{"ArchD",s.AD},{"ArchT",s.AT},{"PurlinD",s.PD},{"PurlinT",s.PT},{"E",s.E},{"Nu",s.Nu},{"G",s.G},{"Density",s.Rho},{"Fc",s.Fc},{"Ft",s.Ft},{"Fb",s.Fb},{"RoofG",s.RoofG},{"RoofQ",s.RoofQ},{"W0",s.W0},{"MuZ",s.MuZ},{"Beta",s.Beta},{"Cp",s.Cp},{"Cs",s.Cs},{"SnowQ",s.Snow},{"WindModel",s.WindModel},{"WindDirection",s.WindDirection},{"WindEnds",s.WindEnds},{"WindReverse",s.WindReverse?1:0},{"WindAngle",s.WindAngle},{"WindRoofWindward",s.WindRoofWindward},{"WindRoofLeeward",s.WindRoofLeeward},{"WindRoofParallel",s.WindRoofParallel},{"WindEndWindward",s.WindEndWindward},{"WindEndLeeward",s.WindEndLeeward},{"WindEndParallel",s.WindEndParallel},{"Support",s.Support},{"Joints",s.Joints},{"Buckle",s.Buckle?1:0},{"Mesh",s.Mesh},{"Limit",requestedLimit},{"Pmax",s.Pmax}};return Encode(p,ys);}
public static List<Brep> Bases(GeometrySet g,Settings s){var result=new List<Brep>();if(!g.Network.Spec.CompoundPointed||g.Network.Spec.PointedVersion<2)return result;foreach(var m in g.Network.Maps)foreach(int side in new[]{-1,1}){var p=m.Map(new V2(side*m.Outer.Spec.W/2,0));var a=m.Frame.Across;var f=m.Frame.Forward;var pl=new Plane(TopologyRhino.Point(p),new Vector3d(a.X,a.Y,a.Z),new Vector3d(f.X,f.Y,f.Z));double w=2*m.Outer.FootOffset+s.LD+100,d=Math.Max(s.LD,s.WD)+100;result.Add(new Box(pl,new Interval(-w/2,w/2),new Interval(-d/2,d/2),new Interval(-160,0)).ToBrep());}return result;}
public static Color ColorFor(double v,double max){var colors=new[]{Color.FromArgb(36,86,223),Color.FromArgb(33,162,190),Color.FromArgb(140,180,72),Color.FromArgb(239,197,68),Color.FromArgb(227,72,56)};double u=Math.Max(0,Math.Min(1,v/Math.Max(.001,max)))*4;int i=Math.Min(3,(int)u);double t=u-i;var a=colors[i];var b=colors[i+1];return Color.FromArgb((int)(a.R+(b.R-a.R)*t),(int)(a.G+(b.G-a.G)*t),(int)(a.B+(b.B-a.B)*t));}
public static string Report(Evaluation e)=>e==null?"待计算":!e.Solved?"未生成数值："+e.Error:$"{e.State}；位移{e.Displacement:0.00}mm；应力{e.StressMax:0.00}MPa；质量{e.Mass:0.00}kg；最大应力工况{e.StressCase}\n"+string.Join("\n",e.Warnings.Concat(e.CaseReports));
public static string Difference(Dictionary<string,double> b,Dictionary<string,double> a)=>"修改项：\n"+string.Join("\n",Keys.Where(k=>b[k]!=a[k]).Select(k=>k+": "+F(b[k])+" → "+F(a[k])));
}
}

namespace Bamboo45 {
 public static class ScreeningNote {
  public static string Report(Settings s,double width,double inputLimit,Evaluation e=null) {
   string origin=inputLimit>0?"用户填写Limit":"默认W/250="+(width/250).ToString("0.###")+"mm（软件演示初筛值，不是原竹通用规范限值）";
   System.Func<double,double,bool,string> check=(v,b,reverse)=>e==null?"待计算":!e.Solved?"无有效结果":!FMath.IsFinite(v)?"未取得指标":(reverse?v>=b:v<=b)?"通过":"超限";
   string result="本次初筛依据：\n最大位移 ≤ "+s.Limit.ToString("0.###")+"mm；来源："+origin+"；计算值="+(e!=null&&e.Solved?e.Displacement.ToString("0.###"):"—")+"；"+check(e==null?double.NaN:e.Displacement,s.Limit,false);
   result+=s.Strength?"\n正应力组合比 ≤ 1.00；逐杆逐工况取最大 |轴向应力|/相应抗压或抗拉阈值 + 弯曲应力/抗弯阈值。Fc="+s.Fc+"，Ft="+s.Ft+"，Fb="+s.Fb+"MPa；组合比="+(e!=null&&e.Solved?e.Ratio.ToString("0.###"):"—")+"；"+check(e==null?double.NaN:e.Ratio,1,false):"\nFc/Ft/Fb均为0：强度未评估；有应力数值不等于强度合格。";
   result+=s.Buckle?"\n最小有效正线性屈曲因子 ≥ 1.00（软件固定诊断界限）；计算值="+(e!=null&&e.Solved?e.Buckling.ToString("0.###"):"—")+"；"+check(e==null?double.NaN:e.Buckling,1,true):"\n屈曲诊断关闭：稳定性未评估。";
   return result+"\n通过初筛只表示已启用检查项未超限；几何、求解有效性及警告另行检查。Karamba3D计算，软件按上述规则判定；未评估不等于通过，不等于完整规范安全验算。\n最大阈值比=max(位移/位移限值，启用时的正应力组合比，启用时的1/屈曲因子)。无有效指标不能据此排名。";
  }
 }
}

public static class FMath {public static bool IsFinite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x); }
