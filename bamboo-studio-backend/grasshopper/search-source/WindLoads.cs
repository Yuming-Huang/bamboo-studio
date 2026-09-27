using BambooStages123;
using BambooFE;

namespace Bamboo45;

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
  int left=net.Maps.FindIndex(m=>m.At(0).Distance(row[0])<.001),right=net.Maps.FindIndex(m=>m.At(1).Distance(row[^1])<.001);
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
 public static double[] NodalLoads(WindCase wc,FrameModel model){
  var loads=new double[model.Nodes.Count*6];
  foreach(var face in wc.Faces){var f=face.Force*(1000.0/3);foreach(var p in new[]{face.A,face.B,face.C}){
   int node=model.Nodes.FindIndex(n=>n.Distance(p)<.001);Settings.Require(node>=0,"风荷载面顶点未连接梁节点；计算已停止");int i=node*6;loads[i]+=f.X;loads[i+1]+=f.Y;loads[i+2]+=f.Z;
  }}return loads;
 }
 public static object Pack(WindCase wc){
  object Point(P3 p)=>new{x=p.X,y=p.Y,z=p.Z};
  return new{id=wc.Id,label=wc.Label,angle=wc.Angle,direction=Point(wc.Direction),forceKN=Point(wc.Force),momentKNm=Point(wc.Moment),
   zones=wc.Faces.GroupBy(f=>(f.Surface,f.Zone)).Select(g=>new{surface=g.Key.Surface,zone=g.Key.Zone,areaM2=g.Sum(f=>f.AreaVector.Length),coefficient=g.First().Coefficient,pressureKNm2=g.First().Pressure}),
   faces=wc.Faces.Select(f=>new{points=new[]{Point(f.A),Point(f.B),Point(f.C)},surface=f.Surface,zone=f.Zone,pressure=f.Pressure,forceKN=Point(f.Force)})};
 }
}
