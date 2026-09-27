using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace BambooStudio;
public sealed partial class DesktopHost : Form {
 readonly Dictionary<string,string> testSavedFiles=new();
 readonly WebView2 web=new(){Dock=DockStyle.Fill};readonly KarambaClient jobs=new();readonly string[] args;readonly string origin="https://bamboo.local";readonly Label loading=new(){Dock=DockStyle.Fill,Text="竹拱工作室\n正在启动Karamba3D 联动界面…",TextAlign=System.Drawing.ContentAlignment.MiddleCenter,Font=new System.Drawing.Font("Microsoft YaHei UI",16)};
 public DesktopHost(string[] args){this.args=args;Text="竹拱工作室 · Karamba3D 联动版 0.8.0 · 屋面显示增强";Width=1440;Height=960;MinimumSize=new System.Drawing.Size(1050,730);StartPosition=FormStartPosition.CenterScreen;Controls.Add(web);Controls.Add(loading);Shown+=async(_,_)=>await Start();FormClosed+=(_,_)=>{jobs.Dispose();web.Dispose();};}
 async Task Start(){try{
  string version;try{version=CoreWebView2Environment.GetAvailableBrowserVersionString();}catch{version=null;}
  if(version==null){loading.Text="缺少 Microsoft WebView2 界面运行组件。\n请安装微软官方 WebView2 Runtime 后重开。\n结构计算需要本机 Rhino 与 Karamba3D。";var link=new LinkLabel{Text="打开微软官方下载页",AutoSize=true,Left=40,Top=80};link.LinkClicked+=(_,_)=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://developer.microsoft.com/microsoft-edge/webview2/"){UseShellExecute=true});Controls.Add(link);link.BringToFront();return;}
  var profile=args.Length==2&&args[0]=="--ui-test"?Path.Combine(Path.GetFullPath(args[1]),"profile"):Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BambooStudio","Profile-six-stage-v03");if(new DriveInfo(Path.GetPathRoot(profile)).AvailableFreeSpace<200_000_000)profile=Path.Combine(AppContext.BaseDirectory,"运行数据","profile");var env=await CoreWebView2Environment.CreateAsync(null,profile);await web.EnsureCoreWebView2Async(env);var core=web.CoreWebView2;core.Settings.AreDefaultContextMenusEnabled=false;core.Settings.AreDevToolsEnabled=false;core.Settings.IsStatusBarEnabled=false;
  core.NewWindowRequested+=(_,e)=>e.Handled=true;core.PermissionRequested+=(_,e)=>e.State=CoreWebView2PermissionState.Deny;
  core.NavigationStarting+=(_,e)=>{if(!e.Uri.StartsWith(origin+"/",StringComparison.Ordinal))e.Cancel=true;};
  core.AddWebResourceRequestedFilter("*",CoreWebView2WebResourceContext.All);
  core.WebResourceRequested+=(_,e)=>{
   var uri=new Uri(e.Request.Uri);if(uri.GetLeftPart(UriPartial.Authority)==origin&&uri.AbsolutePath=="/"&&e.Request.Method=="GET"){
    var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("BambooStudio.index.html");e.Response=env.CreateWebResourceResponse(stream,200,"OK","Content-Type: text/html; charset=utf-8\r\nCache-Control: no-store\r\nContent-Security-Policy: default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; img-src data: blob:; connect-src 'none'; base-uri 'none'; frame-src 'none';\r\n");
   }else e.Response=env.CreateWebResourceResponse(new MemoryStream(),403,"Blocked","Content-Type: text/plain");
  };
  await core.AddScriptToExecuteOnDocumentCreatedAsync(BridgeScript);if(args.Length==2&&args[0]=="--ui-test"){
   var init="localStorage.clear();";var fixture=Path.Combine(args[1],"diagnostic-input.json");
   if(File.Exists(fixture)){using var input=JsonDocument.Parse(File.ReadAllText(fixture));var draft=JsonSerializer.Serialize(new{parameters=input.RootElement.GetProperty("parameters"),moduleFit=new{mode="manual",legacy=false}});init+="localStorage.setItem('bamboo-six-stage-v032',"+JsonSerializer.Serialize(draft)+");";}
   var editorDraft=Path.Combine(args[1],"editor-draft.json");if(File.Exists(editorDraft))init="localStorage.clear();localStorage.setItem('bamboo-six-stage-v032',"+JsonSerializer.Serialize(File.ReadAllText(editorDraft))+");";
   await core.AddScriptToExecuteOnDocumentCreatedAsync(init);var testScript=Path.Combine(args[1],"ui-fixture.js");if(File.Exists(testScript))await core.AddScriptToExecuteOnDocumentCreatedAsync(File.ReadAllText(testScript));
  }core.WebMessageReceived+=OnMessage;
  core.NavigationCompleted+=async(_,e)=>{loading.Visible=false;if(!e.IsSuccess)MessageBox.Show("界面加载失败："+e.WebErrorStatus,"竹拱工作室");else if(args.Length==2&&args[0]=="--ui-test")await RoofVisualUiCheck(args[1]);};
  core.Navigate(origin+"/#session=desktop");
 }catch(Exception ex){loading.Text="启动失败："+ex.Message+"\n请检查 WebView2 是否安装完整。";}}
 async void OnMessage(object sender,CoreWebView2WebMessageReceivedEventArgs e){
  if(!e.Source.StartsWith(origin+"/",StringComparison.Ordinal))return;
  int requestId=-1;
  try{var json=e.WebMessageAsJson;if(json.Length>16*1024*1024)throw new ArgumentException("消息过大");using var doc=JsonDocument.Parse(json);var m=doc.RootElement;if(m.TryGetProperty("id",out var rid))requestId=rid.GetInt32();
   if(m.TryGetProperty("kind",out var info)&&info.GetString()=="licenses"){
    var assembly=Assembly.GetExecutingAssembly();var text=new StringBuilder("竹拱工作室 0.8.0 · 第三方组件许可\r\n\r\n");foreach(string resource in assembly.GetManifestResourceNames().Where(n=>n.StartsWith("BambooStudio.Licenses."))){using var reader=new StreamReader(assembly.GetManifestResourceStream(resource));text.AppendLine(resource).AppendLine(reader.ReadToEnd()).AppendLine();}using var dialog=new Form{Text="第三方组件许可",Width=850,Height=650,StartPosition=FormStartPosition.CenterParent};dialog.Controls.Add(new TextBox{Text=text.ToString(),ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,Font=new System.Drawing.Font("Consolas",10)});dialog.ShowDialog(this);return;
   }
   if(m.TryGetProperty("kind",out var kind)&&kind.GetString()=="save"){
    var name=Path.GetFileName(m.GetProperty("name").GetString());if(Path.GetExtension(name) is not (".json" or ".txt"))throw new ArgumentException("只允许导出参数或文本报告");
    if(args.Length==2&&args[0]=="--ui-test"){
     var testFolder=Path.Combine(Path.GetFullPath(args[1]),"exports");Directory.CreateDirectory(testFolder);var file=Path.Combine(testFolder,name);File.WriteAllText(file,m.GetProperty("text").GetString(),new UTF8Encoding(false));testSavedFiles[name]=file;return;
    }
    using var dialog=new SaveFileDialog{FileName=name,Filter="参数/报告 (*.json;*.txt)|*.json;*.txt",OverwritePrompt=true};if(dialog.ShowDialog(this)==DialogResult.OK)File.WriteAllText(dialog.FileName,m.GetProperty("text").GetString(),new UTF8Encoding(false));return;
   }
   int id=m.GetProperty("id").GetInt32();var path=m.GetProperty("path").GetString();var method=m.GetProperty("method").GetString();var body=m.TryGetProperty("body",out var b)&&b.ValueKind==JsonValueKind.String?b.GetString():null;
   if(path=="/api/export-rhino"&&method=="POST"){var export=await ExportRhino(body);web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new{id,ok=true,status=200,data=export}));return;}
   if(path=="/api/save-image"&&method=="POST"){var image=await SaveImage(body);web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new{id,ok=true,status=200,data=image}));return;}
   var result=await jobs.Request(path,method,body);web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new{id,ok=result.status<400,status=result.status,data=result.data}));
  }catch(Exception ex){if(!IsDisposed)web.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(new{id=requestId,ok=false,status=400,data=new{error=ex.Message}}));}
 }
 const string BridgeScript="""
 (()=>{const pending=new Map();let serial=0;window.__bambooDesktop=true;
 window.chrome.webview.addEventListener('message',e=>{const p=pending.get(e.data.id);if(!p)return;pending.delete(e.data.id);p.resolve({ok:e.data.ok,status:e.data.status,json:async()=>e.data.data});});
 window.fetch=(path,options={})=>new Promise((resolve,reject)=>{if(typeof path!=='string'||!path.startsWith('/api/')){reject(Error('独立版不允许外部网络请求'));return;}const id=++serial;pending.set(id,{resolve,reject});options.signal?.addEventListener('abort',()=>{pending.delete(id);reject(Error('请求超时'));},{once:true});window.chrome.webview.postMessage({id,path,method:options.method||'GET',body:options.body||null});});
 })();
 """;
}
