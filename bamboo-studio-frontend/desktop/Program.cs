using System.Text.Json;
namespace BambooStudio;
internal static class Program {
 [STAThread]static int Main(string[] args){
  try{
   var localTemp=Path.Combine(AppContext.BaseDirectory,"运行数据","temp");
   if(new DriveInfo(Path.GetPathRoot(Path.GetTempPath())).AvailableFreeSpace<200_000_000){Directory.CreateDirectory(localTemp);Environment.SetEnvironmentVariable("TEMP",localTemp);Environment.SetEnvironmentVariable("TMP",localTemp);}

   if(args.Length==3&&args[0]=="--rhino-export"){try{var report=RhinoExport.Write(File.ReadAllText(args[1]),args[2]);File.WriteAllText(args[2]+".report.json",JsonSerializer.Serialize(report));return 0;}catch(Exception e){File.WriteAllText(args[2]+".report.json",JsonSerializer.Serialize(new{pass=false,error=e.ToString()}));return 1;}}
   ApplicationConfiguration.Initialize();Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);Application.ThreadException+=(_,e)=>MessageBox.Show("运行异常："+e.Exception.Message,"竹拱工作室");
   Application.Run(new DesktopHost(args));return 0;
  }catch(Exception e){MessageBox.Show(e.Message,"竹拱工作室启动失败");return 1;}
 }
}