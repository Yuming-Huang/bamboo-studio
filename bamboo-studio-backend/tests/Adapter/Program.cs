using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Nodes;
using BambooStudio;
var checks=new List<string>();
void Check(bool ok,string message){if(!ok)throw new Exception(message);checks.Add(message);}
var inputNode=JsonNode.Parse(File.ReadAllText(args[0])).AsObject();
string Body(JsonObject p,string mode="compare")=>JsonSerializer.Serialize(new{schema="bamboo-local/v1",mode,parameters=p});
var input=Input.Parse(Body(inputNode));
Check(input.List("CrossAngles",Array.Empty<double>()).SequenceEqual(new[]{25.0,15,35}),"new array parameters accepted by production Input");
var invalid=inputNode.DeepClone().AsObject();invalid["W"]=6000;
try{Input.Parse(Body(invalid));throw new Exception("Expected side reserve rejection");}catch(ArgumentException){checks.Add("manual span respects side reserve before job starts");}
Check(Input.Parse(Body(invalid,"single")).Single,"single edit does not reapply initial layout side reserve");
invalid=inputNode.DeepClone().AsObject();invalid["HeightSearch"]=false;invalid["Hmin"]=1.5;invalid["Hmax"]=.5;
Check(!Input.Parse(Body(invalid)).B("HeightSearch"),"locked height ignores search interval order");
// Reflection loads managed code only; never starts Rhino, Karamba, or an analysis job.
string rhino=args[2];
AssemblyLoadContext.Default.Resolving+=(ctx,name)=>{
 foreach(string dir in new[]{Path.Combine(rhino,"System","netcore"),Path.Combine(rhino,"System"),Path.Combine(rhino,"Plug-ins","Grasshopper"),(args.Length>3?args[3]:Path.Combine(rhino,"Plug-ins","Karamba"))}){
  var path=Path.Combine(dir,name.Name+".dll");if(File.Exists(path))return ctx.LoadFromAssemblyPath(path);
 }return null;
};
var assembly=AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[1]));
var bridgeInput=assembly.GetType("BambooStudio.Input");var actualInput=bridgeInput.GetMethod("Parse").Invoke(null,new object[]{Body(inputNode)});
var specType=assembly.GetType("BambooStages123.Spec");var evalType=assembly.GetType("Bamboo45.Evaluation");var settingsType=assembly.GetType("Bamboo45.Settings");
void Set(object o,string k,object v)=>o.GetType().GetField(k).SetValue(o,v);
var pack=assembly.GetType("BambooStudio.Solver").GetMethod("Pack",BindingFlags.Static|BindingFlags.NonPublic);
foreach(int type in new[]{2,4,5}){
 var spec=Activator.CreateInstance(specType);Set(spec,"Shape",type%2==0?1:0);Set(spec,"Structure",type/2);Set(spec,"LegH",1800.0);
 Set(spec,"CrossAngle",35.0);Set(spec,"Depth",600.0);Set(spec,"Panels",10);Set(spec,"PointE",1000.0);Set(spec,"PointSlope",.8);Set(spec,"PointFootGap",20.0);Set(spec,"PointPanels",8);
 var settings=Activator.CreateInstance(settingsType);Set(settings,"AD",150.0);
 var evaluation=Activator.CreateInstance(evalType);Set(evaluation,"Settings",settings);Set(evaluation,"Solved",false);Set(evaluation,"Name","serialization test, not solved");
 var encoded=JsonSerializer.SerializeToElement(pack.Invoke(null,new[]{evaluation,spec,actualInput}));var selected=encoded.GetProperty("parameters");
 Check(selected.GetProperty("type").GetInt32()==type,"Pack keeps candidate type "+type);
 Check(selected.GetProperty("CrossAngle").GetDouble()==35&&selected.GetProperty("TrussDepth").GetDouble()==600&&selected.GetProperty("Panels").GetInt32()==10,"Pack transfers selected angle/depth/panels "+type);
 Check(selected.GetProperty("PointE").GetDouble()==1000&&selected.GetProperty("PointSlope").GetDouble()==.8&&selected.GetProperty("PointFootGap").GetDouble()==20&&selected.GetProperty("PointPanels").GetInt32()==8,"Pack transfers all pointed parameters "+type);
 Check(selected.GetProperty("ArchD").GetDouble()==150&&selected.GetProperty("CrossAngles")[0].GetDouble()==25,"actual section and candidate-list baseline remain distinct "+type);
 Check(!encoded.GetProperty("solved").GetBoolean()&&encoded.GetProperty("massKg").ValueKind==JsonValueKind.Null,"unsolved evaluation never emits numerical result "+type);
}
string report=JsonSerializer.Serialize(new{pass=true,checks,rhinoRuntimeVerified=false,note="Production bridge serializer tested with synthetic unsolved records; no Karamba calls."},new JsonSerializerOptions{WriteIndented=true});
File.WriteAllText("adapter-test.json",report);Console.WriteLine(report);
