using System.Text.Json;
using Microsoft.Web.WebView2.Core;
namespace BambooStudio;
public sealed partial class DesktopHost {
 bool rhinoExportBusy;
 async Task<object> ExportRhino(string body){
  if(rhinoExportBusy)throw new InvalidOperationException("已有模型正在导出，请稍候。");
  var data=RhinoExport.Parse(body);rhinoExportBusy=true;
  try{
   string name=$"竹拱_{data.SelectedCase}_{(data.SelectedCase=="B"?"搜索推荐":"当前设计")}.3dm",path;
   if(args.Length==2&&args[0]=="--ui-test"){
    string folder=Path.Combine(Path.GetFullPath(args[1]),"exports");Directory.CreateDirectory(folder);path=Path.Combine(folder,name);
   }else{
    using var dialog=new SaveFileDialog{Title=$"导出 {data.SelectedCase} 的 Rhino 模型",FileName=name,Filter="Rhino 模型 (*.3dm)|*.3dm",DefaultExt="3dm",AddExtension=true,OverwritePrompt=true,CheckPathExists=true};
    if(dialog.ShowDialog(this)!=DialogResult.OK)return new{cancelled=true};path=dialog.FileName;
   }
   var report=await Task.Run(()=>RhinoExport.Write(body,path));testSavedFiles[name]=path;
   return new{cancelled=false,saved=true,path,report};
 }finally{rhinoExportBusy=false;}
 }

 async Task RhinoCheck(string expression,string error){if(await web.CoreWebView2.ExecuteScriptAsync(expression)!="true")throw new Exception(error);}
 async Task RhinoWait(string expression){for(int i=0;i<150;i++){await Task.Delay(200);if(await web.CoreWebView2.ExecuteScriptAsync(expression)=="true")return;}throw new Exception("Rhino 导出界面等待超时："+expression);}
 async Task RhinoCapture(string folder,string name){await Task.Delay(250);using var f=File.Create(Path.Combine(folder,name));await web.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,f);}
 async Task RhinoUiBeforeAnalysis(string folder){
  var core=web.CoreWebView2;
  await core.ExecuteScriptAsync("document.querySelector('[data-step=\"5\"]').click();");
  await RhinoCheck("document.querySelector('#saveB').disabled&&!document.querySelector('#rhinoExport').disabled&&document.querySelector('#saveC').checked","未分析 C 必须可导出，缺少 B 必须禁用");
  await core.ExecuteScriptAsync("document.querySelector('#rhinoExport').click();");await RhinoWait("document.querySelector('#rhinoStatus').textContent.startsWith('已导出 C')");
  using(var file=Rhino.FileIO.File3dm.Read(testSavedFiles["竹拱_C_当前设计.3dm"])){
   using var a=JsonDocument.Parse(file.Strings.GetValue("BambooStudio","Analysis"));
   if(a.RootElement.GetProperty("executed").GetBoolean())throw new Exception("未分析 C 的 Rhino 文件不应标记已分析");
  }
  await RhinoCapture(folder,"rhino-before-analysis.png");
 }
 async Task RhinoUiWithResult(string folder){
  var core=web.CoreWebView2;
  await core.ExecuteScriptAsync("document.querySelector('[data-step=\"5\"]').click();window.__rhinoDraft=JSON.parse(localStorage.getItem('bamboo-v2-'+document.body.dataset.variant)).parameters;");
  await RhinoWait("!document.querySelector('#saveB').disabled");
  await core.ExecuteScriptAsync("document.querySelector('#saveB').click();");
  await RhinoCheck("document.querySelector('#saveB').checked&&document.querySelector('#modelTag').textContent.startsWith('B /')&&!document.querySelector('#rhinoExport').disabled","第五步 B 预览或导出状态错误");
  await RhinoCapture(folder,"rhino-B-dark.png");
  await core.ExecuteScriptAsync("document.querySelector('#rhinoExport').click();document.querySelector('#rhinoExport').click();");
  await RhinoWait("document.querySelector('#rhinoStatus').textContent.startsWith('已导出 B')");
  await core.ExecuteScriptAsync("document.querySelector('#saveSnapshot').click();window.__rhinoSnapshot=JSON.parse(localStorage.getItem('bamboo-v2-'+document.body.dataset.variant)).snapshot;");
  await RhinoCheck("window.__rhinoSnapshot.selectedCase==='B'&&JSON.stringify(JSON.parse(localStorage.getItem('bamboo-v2-'+document.body.dataset.variant)).parameters)===JSON.stringify(window.__rhinoDraft)","保存 B 快照修改了 C");
  await core.ExecuteScriptAsync("document.querySelector('#saveC').click();document.querySelector('#rhinoExport').click();");
  await RhinoWait("document.querySelector('#rhinoStatus').textContent.startsWith('已导出 C')");
  await RhinoCheck("JSON.stringify(JSON.parse(localStorage.getItem('bamboo-v2-'+document.body.dataset.variant)).snapshot)===JSON.stringify(window.__rhinoSnapshot)","导出 C 修改了 B 快照");
  foreach(var caseId in new[]{"C","B"}){
   var suffix=caseId=="B"?"搜索推荐":"当前设计";
   using var f=Rhino.FileIO.File3dm.Read(testSavedFiles[$"竹拱_{caseId}_{suffix}.3dm"]);
   using var actual=JsonDocument.Parse(f.Strings.GetValue("BambooStudio","Parameters"));
   using var expected=JsonDocument.Parse(File.ReadAllText(testSavedFiles[$"竹拱_{caseId}_{suffix}_参数.json"]));
   static string Signature(JsonElement x)=>JsonSerializer.Serialize(x.EnumerateObject().OrderBy(p=>p.Name,StringComparer.Ordinal).ToDictionary(p=>p.Name,p=>p.Value));
   if(Signature(actual.RootElement)!=Signature(expected.RootElement.GetProperty("parameters")))throw new Exception(caseId+" Rhino 参数与所选方案不一致");
   if(f.Strings.GetValue("BambooStudio","SelectedCase")!=caseId||f.Views.Count!=1)throw new Exception("Rhino 方案标识或初始视图错误");
  }
  await core.ExecuteScriptAsync("window.__rhinoFetch=window.fetch;window.fetch=async(path,options)=>path==='/api/export-rhino'?{ok:true,json:async()=>({cancelled:true})}:window.__rhinoFetch(path,options);document.querySelector('#rhinoExport').click();");
  await RhinoWait("document.querySelector('#rhinoStatus').textContent.startsWith('已取消')&&!document.querySelector('#rhinoExport').disabled");
  await core.ExecuteScriptAsync("window.fetch=async(path,options)=>path==='/api/export-rhino'?{ok:false,json:async()=>({error:'测试写入失败'})}:window.__rhinoFetch(path,options);document.querySelector('#rhinoExport').click();");
  await RhinoWait("document.querySelector('#rhinoStatus').textContent.includes('导出失败：测试写入失败')&&!document.querySelector('#rhinoExport').disabled");
  await core.ExecuteScriptAsync("window.fetch=window.__rhinoFetch;document.querySelector('#toast').hidden=true;document.querySelector('#saveB').click();if(document.documentElement.dataset.theme!=='light')document.querySelector('#themeToggle').click();");
  await RhinoCapture(folder,"rhino-B-light.png");Width=1050;Height=730;await RhinoCapture(folder,"rhino-compact.png");Width=1440;Height=960;
  File.WriteAllText(Path.Combine(folder,"rhino-ui-test.json"),JsonSerializer.Serialize(new{pass=true,checks=new[]{"unanalysed C exports native geometry","missing B disabled","C/B native 3dm parameter equality","B preview follows selection","B snapshot preserves C","C export preserves B snapshot","cancel response recovery","failure response recovery","light/dark/minimum window"}}));
 }
 async Task RhinoUiStale(){
  var core=web.CoreWebView2;await core.ExecuteScriptAsync("document.querySelector('[data-step=\"5\"]').click();");
  await RhinoCheck("document.querySelector('#saveB').checked&&document.querySelector('#saveB').disabled&&document.querySelector('#rhinoExport').disabled","过期 B 必须停用，不能偷偷改为 C 导出");
  await core.ExecuteScriptAsync("document.querySelector('#saveC').click();");await RhinoCheck("!document.querySelector('#rhinoExport').disabled","修改参数后的 C 不能导出");
 }
}
