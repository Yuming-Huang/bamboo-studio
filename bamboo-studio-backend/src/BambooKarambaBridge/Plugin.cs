using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Rhino;
using Rhino.Commands;
using Rhino.PlugIns;
using BambooStudio;
[assembly: AssemblyVersion("0.8.0.0")]
[assembly: AssemblyFileVersion("0.8.0.0")]
[assembly: AssemblyTitle("Bamboo Karamba3D Bridge")]
[assembly: AssemblyDescription("Local Karamba3D analysis bridge for Bamboo Studio")]
[assembly: Guid("15C952B1-2C56-47A9-BA63-522DF7680F77")]
namespace BambooBridge;
public sealed class BambooPlugin : PlugIn {
 internal static LoopbackServer Server;
 protected override void OnShutdown(){Server?.Dispose();Server=null;base.OnShutdown();}
}
public sealed class StartCommand : Command {
 public override string EnglishName=>"BambooKarambaStart";
 protected override Result RunCommand(RhinoDoc doc,RunMode mode){
  try{
   if(BambooPlugin.Server!=null){RhinoApp.WriteLine("Bamboo Karamba3D 连接插件 v0.8.0 已启动；在 App 点击重新连接。");return Result.Success;}
   if(!AppDomain.CurrentDomain.GetAssemblies().Any(a=>a.GetName().Name=="Grasshopper")){RhinoApp.WriteLine("请先运行 Grasshopper 命令，并确保 Karamba3D 插件与许可证已安装。");return Result.Failure;}
   RuntimeResolver.Install();
   StartService();
   RhinoApp.WriteLine("Bamboo Karamba3D 连接插件 v0.8.0 已启动，本机端口 "+BambooPlugin.Server.Port+"。保持 Rhino 打开，在 App 点击重新连接。");
   return Result.Success;
  }catch(Exception e){RhinoApp.WriteLine("Karamba3D 连接未启动："+e.GetBaseException().Message);return Result.Failure;}
 }
 [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
 static void StartService(){
  var version=typeof(KarambaCommon.Toolkit).Assembly.GetName().Version.ToString();
  _=new KarambaCommon.Toolkit();
  var jobs=new Jobs(action=>RhinoApp.InvokeOnUiThread(action),Solver.Run,version);
  var server=new LoopbackServer(jobs);try{server.Start();BambooPlugin.Server=server;}catch{server.Dispose();throw;}
 }
}
public sealed class StopCommand : Command {
 public override string EnglishName=>"BambooKarambaStop";
 protected override Result RunCommand(RhinoDoc doc,RunMode mode){BambooPlugin.Server?.Dispose();BambooPlugin.Server=null;RhinoApp.WriteLine("Bamboo Karamba3D 连接已停止。");return Result.Success;}
}
static class RuntimeResolver {
 static bool installed;static string folder;
 public static void Install(){
  if(installed)return;
  var loaded=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a.GetName().Name=="karambaCommon");
  folder=loaded!=null?Path.GetDirectoryName(loaded.Location):Path.Combine(Path.GetDirectoryName(typeof(RhinoApp).Assembly.Location),"..","..","Plug-ins","Karamba");
  folder=Path.GetFullPath(folder);
  if(!File.Exists(Path.Combine(folder,"karambaCommon.dll")))throw new FileNotFoundException("找不到已安装的 Karamba3D。请在 Grasshopper 中先加载 Karamba3D，再重试。");
  AssemblyLoadContext.Default.Resolving+=(_,name)=>{var path=Path.Combine(folder,name.Name+".dll");return File.Exists(path)?AssemblyLoadContext.Default.LoadFromAssemblyPath(path):null;};
  AssemblyLoadContext.Default.ResolvingUnmanagedDll+=(assembly,name)=>{
   var path=Path.Combine(folder,name.EndsWith(".dll",StringComparison.OrdinalIgnoreCase)?name:name+".dll");
   return File.Exists(path)?LoadLibraryEx(path,IntPtr.Zero,0x100|0x1000):IntPtr.Zero;
  };
  installed=true;
 }
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr LoadLibraryEx(string path,IntPtr file,uint flags);
}

