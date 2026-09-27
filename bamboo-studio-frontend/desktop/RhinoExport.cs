using System.Drawing;
using System.Globalization;
using System.Text.Json;
using Rhino;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

namespace BambooStudio;

public static class RhinoExport {
 static readonly JsonSerializerOptions Options=new(){PropertyNameCaseInsensitive=true};
 public sealed class Data {
  public string Schema{get;set;} public string SelectedCase{get;set;} public string CreatedAt{get;set;}
  public JsonElement Parameters{get;set;} public JsonElement Analysis{get;set;} public string Scope{get;set;}
  public Foundation[] Foundations{get;set;}=Array.Empty<Foundation>();
  public Member[] Members{get;set;} public Roof[] Roofs{get;set;} public double[][][] Supports{get;set;}
 }
 public sealed class Foundation {public double[] Point{get;set;}public double Width{get;set;}public double Depth{get;set;}public double Height{get;set;}public double Angle{get;set;}}
 public sealed class Member {public string Id{get;set;} public string Role{get;set;} public int Frame{get;set;} public double Diameter{get;set;} public double Wall{get;set;} public Segment[] Segments{get;set;}}
 public sealed class Segment {public string Kind{get;set;} public double[][] Points{get;set;}}
 public sealed class Roof {public string Name{get;set;} public double[][][] Triangles{get;set;}}
 static Point3d Point(double[] p){if(p==null||p.Length!=3||p.Any(x=>!double.IsFinite(x)||Math.Abs(x)>1e7))throw new ArgumentException("模型包含无效坐标。");return new Point3d(p[0],p[1],p[2]);}
 static void Need(bool ok,string message){if(!ok)throw new ArgumentException(message);}
 static string Number(double x)=>x.ToString("R",CultureInfo.InvariantCulture);
 public static Data Parse(string json){
  Need(json!=null&&json.Length<=12*1024*1024,"导出数据过大。");var d=JsonSerializer.Deserialize<Data>(json,Options);
  Need(d!=null&&d.Schema=="bamboo-rhino-export/v1"&&d.SelectedCase is "C" or "B","导出方案格式无效。");
  Need(d.Members is {Length:>0 and <=10000}&&d.Roofs is {Length:>0 and <=200}&&d.Supports is {Length:<=10000},"模型对象数量无效。");
  Need(d.Parameters.ValueKind==JsonValueKind.Object,"缺少设计参数。");
  if(d.SelectedCase=="B")Need(d.Analysis.TryGetProperty("solved",out var solved)&&solved.GetBoolean(),"B 没有完成求解。");
  Need(d.Members.Sum(m=>m.Segments?.Length??0)<=20000&&d.Roofs.Sum(r=>r.Triangles?.Length??0)<=100000,"模型过大，请减少阵列或檩条数量。");
  return d;
 }
 public static object Write(string json,string path){
  var d=Parse(json);Need(Path.GetExtension(path).Equals(".3dm",StringComparison.OrdinalIgnoreCase),"文件扩展名必须为 .3dm。");
  using var file=new File3dm();file.ApplicationName="竹拱工作室";file.ApplicationDetails="0.8.0 / 屋面选型修订 / Rhino3dm 8.17.0";
  file.Settings.ModelUnitSystem=UnitSystem.Millimeters;file.Settings.ModelAbsoluteTolerance=.001;file.Settings.ModelAngleToleranceRadians=Math.PI/180;
  file.Strings.SetString("BambooStudio","SelectedCase",d.SelectedCase);file.Strings.SetString("BambooStudio","Parameters",d.Parameters.GetRawText());
  file.Strings.SetString("BambooStudio","Analysis",d.Analysis.GetRawText());file.Strings.SetString("BambooStudio","Scope",d.Scope??"");file.Strings.SetString("BambooStudio","CreatedAt",d.CreatedAt??"");
  int Layer(string name,Color color,bool visible=true){var l=new Layer{Name=d.SelectedCase+"_"+name,Color=color,IsVisible=visible};int index=file.AllLayers.Count;file.AllLayers.Add(l);return index;}
  int arches=Layer("01_拱肋中心线",Color.FromArgb(47,88,105)),purlins=Layer("02_檩条中心线",Color.FromArgb(47,110,89)),webs=Layer("03_腹杆中心线",Color.FromArgb(105,85,121));
  int bamboo=Layer("04_竹竿外形",Color.FromArgb(181,144,90),false),roofLayer=Layer("05_屋面网格",Color.FromArgb(168,182,173),false),supportLayer=Layer("06_屋面次支承示意",Color.FromArgb(200,124,48),false);
  // Open with clear editable centerlines. Other layers are present and can be shown in Rhino.
  var bounds=BoundingBox.Empty;int curves=0,surfaces=0,meshes=0;
  ObjectAttributes Attributes(int layer,string name,Member m=null){var a=new ObjectAttributes{LayerIndex=layer,Name=name,ColorSource=ObjectColorSource.ColorFromLayer};a.SetUserString("BambooStudio.Case",d.SelectedCase);
   if(m!=null){a.SetUserString("MemberId",m.Id);a.SetUserString("Role",m.Role);a.SetUserString("FrameIndex",m.Frame.ToString());a.SetUserString("Diameter_mm",Number(m.Diameter));a.SetUserString("Wall_mm",Number(m.Wall));}return a;}
  void Added(Guid id){Need(id!=Guid.Empty,"写入 Rhino 对象失败。");}
  foreach(var m in d.Members){
   Need(m.Role is "arch" or "web" or "purlin","未知杆件类型。");Need(double.IsFinite(m.Diameter)&&m.Diameter>0&&m.Diameter<=600&&m.Wall>0&&2*m.Wall<m.Diameter,"杆件截面无效。");
   Need(m.Segments is {Length:>0 and <=100},"杆件线段数量无效。");using var curve=new PolyCurve();int segmentIndex=0;
   foreach(var s in m.Segments){
    Need(s.Points!=null&&s.Points.Length==(s.Kind=="arc"?3:2),"中心线控制点无效。");var p=s.Points.Select(Point).ToArray();Curve segment;Surface tube;
    if(s.Kind=="arc"){
     var arc=new Arc(p[0],p[1],p[2]);Need(arc.IsValid&&arc.Radius>m.Diameter/2,"圆弧或竹径无效。");segment=new ArcCurve(arc);
     // Revolve a circular section about the exact arc axis; preserves analytic curvature.
     var section=new Circle(new Plane(arc.StartPoint,arc.TangentAt(0)),m.Diameter/2).ToNurbsCurve();
     using(section){using var revolved=RevSurface.Create(section,new Line(arc.Center,arc.Center+arc.Plane.Normal),0,arc.Angle);Need(revolved!=null&&revolved.IsValid,"弧形竹竿外形生成失败。");tube=revolved.ToNurbsSurface();}
    }else{
     Need(s.Kind=="line"&&p[0].DistanceTo(p[1])>.001,"直杆中心线无效。");segment=new LineCurve(p[0],p[1]);
     using var revolved=new Cylinder(new Circle(new Plane(p[0],p[1]-p[0]),m.Diameter/2),p[0].DistanceTo(p[1])).ToRevSurface();tube=revolved.ToNurbsSurface();
    }
    using(segment){Need(curve.Append(segment),"中心线无法连接。");bounds.Union(segment.GetBoundingBox(true));}
    using(tube){Need(tube!=null&&tube.IsValid,"竹竿曲面无效。");Added(file.Objects.AddSurface(tube,Attributes(bamboo,m.Id+"_外形_"+(++segmentIndex),m)));surfaces++;}
   }
   Need(curve.IsValid,"中心线无效。");Added(file.Objects.AddCurve(curve,Attributes(m.Role=="purlin"?purlins:m.Role=="web"?webs:arches,m.Id,m)));curves++;
  }
  foreach(var r in d.Roofs){
   Need(r.Triangles is {Length:>0},"屋面缺少网格。");using var mesh=new Mesh();
   foreach(var t in r.Triangles){Need(t?.Length==3,"屋面三角形无效。");var points=t.Select(Point).ToArray();Need(Vector3d.CrossProduct(points[1]-points[0],points[2]-points[0]).Length>1e-6,"屋面存在退化三角形。");int start=mesh.Vertices.Count;foreach(var p in points){mesh.Vertices.Add(p);bounds.Union(p);}mesh.Faces.AddFace(start,start+1,start+2);}
   mesh.Vertices.CombineIdentical(true,true);mesh.Normals.ComputeNormals();mesh.Compact();Need(mesh.IsValid,"屋面网格无效。");Added(file.Objects.AddMesh(mesh,Attributes(roofLayer,r.Name)));meshes++;
  }
  foreach(var s in d.Supports){Need(s?.Length==2,"次支承示意坐标无效。");Added(file.Objects.AddLine(Point(s[0]),Point(s[1]),Attributes(supportLayer,"次支承待设计")));}
  Need(d.Foundations!=null&&d.Foundations.Length<=100,"底座数量无效。");
  if(d.Foundations.Length>0){int foundationLayer=Layer("07_分脚底座示意",Color.FromArgb(170,179,173));foreach(var f in d.Foundations){
   Need(new[]{f.Width,f.Depth,f.Height}.All(x=>double.IsFinite(x)&&x>0&&x<10000)&&double.IsFinite(f.Angle),"底座尺寸无效。");
   var plane=new Plane(Point(f.Point),new Vector3d(Math.Cos(f.Angle),Math.Sin(f.Angle),0),new Vector3d(-Math.Sin(f.Angle),Math.Cos(f.Angle),0));
   using var box=new Box(plane,new Interval(-f.Width/2,f.Width/2),new Interval(-f.Depth/2,f.Depth/2),new Interval(-f.Height,0)).ToBrep();Need(box.IsValid,"底座几何无效。");
   Added(file.Objects.AddBrep(box,Attributes(foundationLayer,"同底座分脚_非节点详图")));bounds.Union(box.GetBoundingBox(true));}}
  using(var view=new ViewInfo{Name=d.SelectedCase+" 竹拱轴测"}){
   var vp=view.Viewport;vp.IsParallelProjection=true;double span=Math.Max(1000,bounds.Diagonal.Length);
   vp.SetScreenPort(0,1200,900,0,0,1);vp.SetCameraLocation(bounds.Center+new Vector3d(span,-span,span));
   vp.SetCameraDirection(new Vector3d(-1,1,-1));vp.SetCameraUp(Vector3d.ZAxis);vp.TargetPoint=bounds.Center;
   vp.SetFrustum(-span,span,-span*.75,span*.75,1,span*8);
   Need(vp.DollyExtents(file.Objects.Select(o=>o.Geometry),1.1)&&vp.IsValidCamera&&vp.IsValidFrustum,"模型初始视图无效。");
   file.Views.Add(view);
  }
  var target=Path.GetFullPath(path);var folder=Path.GetDirectoryName(target);Need(Directory.Exists(folder),"保存目录不存在。");
  // Write and validate before replacing the chosen destination, so failures preserve existing files.
  string temporary=Path.Combine(folder,".bamboo-"+Guid.NewGuid().ToString("N")+".3dm");
  try{
   Need(file.Write(temporary,7),"3DM 文件写入失败。");using(var reopened=File3dm.Read(temporary)){
    Need(reopened!=null&&reopened.Settings.ModelUnitSystem==UnitSystem.Millimeters&&reopened.Objects.Count==file.Objects.Count,"3DM 回读校验失败。");
    Need(reopened.Objects.All(o=>o.Geometry.IsValid),"3DM 含无效几何对象。");
   }
   File.Move(temporary,target,true);
  }finally{if(File.Exists(temporary))File.Delete(temporary);}
  return new{path=target,selectedCase=d.SelectedCase,curves,surfaces,meshes,supports=d.Supports.Length,units="mm",bytes=new FileInfo(target).Length};
 }
}
