using System.Text.Json;
namespace BambooStudio;
public sealed partial class DesktopHost {
 bool imageSaveBusy;
 async Task<object> SaveImage(string body){
  if(imageSaveBusy)throw new InvalidOperationException("已有图片正在保存，请稍候。");
  using var data=JsonDocument.Parse(body);var root=data.RootElement;
  string name=Path.GetFileName(root.GetProperty("name").GetString());
  string uri=root.GetProperty("dataUrl").GetString();const string prefix="data:image/png;base64,";
  if(!name.EndsWith(".png",StringComparison.OrdinalIgnoreCase)||uri==null||!uri.StartsWith(prefix,StringComparison.Ordinal))throw new ArgumentException("仅支持 PNG 图片。");
  byte[] bytes=Convert.FromBase64String(uri[prefix.Length..]);
  if(bytes.Length>12*1024*1024||bytes.Length<24||!bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}))throw new ArgumentException("PNG 图片无效或过大。");
  using(var stream=new MemoryStream(bytes))using(var bitmap=System.Drawing.Image.FromStream(stream)){
   if(bitmap.Width<64||bitmap.Height<64||(long)bitmap.Width*bitmap.Height>32_000_000)throw new ArgumentException("图片尺寸无效。");
  }
  imageSaveBusy=true;
  try{
   string path;
   if(args.Length==2&&args[0]=="--ui-test"){
    var folder=Path.Combine(Path.GetFullPath(args[1]),"exports");Directory.CreateDirectory(folder);path=Path.Combine(folder,name);
   }else{
    using var dialog=new SaveFileDialog{Title="保存纯净模型图片",FileName=name,Filter="PNG 图片 (*.png)|*.png",DefaultExt="png",AddExtension=true,OverwritePrompt=true,CheckPathExists=true};
    if(dialog.ShowDialog(this)!=DialogResult.OK)return new{cancelled=true};path=dialog.FileName;
   }
   string temp=Path.Combine(Path.GetDirectoryName(path),".bamboo-image-"+Guid.NewGuid().ToString("N")+".tmp");
   try{await File.WriteAllBytesAsync(temp,bytes);File.Move(temp,path,true);}finally{if(File.Exists(temp))File.Delete(temp);}
   testSavedFiles[name]=path;return new{saved=true,cancelled=false,path};
 }finally{imageSaveBusy=false;}
 }

 readonly List<object> imageChecks=new();
 async Task CheckPng(string label){
  var before=testSavedFiles.Keys.Where(k=>k.EndsWith(".png")).ToHashSet();
  await web.CoreWebView2.ExecuteScriptAsync("document.querySelector('#savePng').click();");
  await RhinoWait("document.querySelector('#captureStatus').textContent.startsWith('PNG 已保存')&&!document.querySelector('#savePng').disabled");
  string name=testSavedFiles.Keys.Single(k=>k.EndsWith(".png")&&!before.Contains(k));
  using var image=new System.Drawing.Bitmap(testSavedFiles[name]);
  if(image.Width<1000||image.Height<500||image.Width>3840||image.Height>2500)throw new Exception("PNG 分辨率不正确");
  if(image.GetPixel(0,0).A!=255)throw new Exception("PNG 背景不能透明");
  int changed=0;var bg=image.GetPixel(0,0).ToArgb();for(int x=0;x<image.Width;x+=20)for(int y=0;y<image.Height;y+=20)if(image.GetPixel(x,y).ToArgb()!=bg)changed++;
  if(changed<40)throw new Exception("PNG 模型为空");
  imageChecks.Add(new{label,file=name,width=image.Width,height=image.Height,opaque=true,nonBackgroundSamples=changed});
 }
 async Task CaptureUiTest(string folder){
  var core=web.CoreWebView2;
  await core.ExecuteScriptAsync("window.__captureDraft=localStorage.getItem('bamboo-v2-'+document.body.dataset.variant);window.__captureGrid=document.querySelector('#gridToggle').getAttribute('aria-pressed');document.querySelector('#toggleInfo').click();");
  await RhinoCheck("getComputedStyle(document.querySelector('.compare-values')).display==='none'&&document.querySelector('#toggleInfo').textContent==='显示信息'","信息卡片没有收起");
  await RhinoCapture(folder,"capture-hidden-info.png");
  await core.ExecuteScriptAsync("document.querySelector('#captureMode').click();");await Task.Delay(300);
  await RhinoCheck("document.body.classList.contains('capture-mode')&&getComputedStyle(document.querySelector('.control-panel')).display==='none'&&getComputedStyle(document.querySelector('.workflow')).display==='none'&&getComputedStyle(document.querySelector('#comparisonOverlay')).display==='none'&&!document.querySelector('#captureGrid').checked","截图模式没有移除编辑界面");
  await RhinoCheck("document.querySelector('#canvasHost').clientWidth===document.querySelector('#viewport').clientWidth&&document.querySelector('#captureCaption').children.length===2&&!document.querySelector('#captureLegend').hidden","B/C 截图布局不正确");
  await RhinoCapture(folder,"capture-annotated-light.png");await CheckPng("BC light with labels and stress legend");
  await core.ExecuteScriptAsync("document.querySelector('#captureAnnotations').click();document.querySelector('#toast').hidden=true;");await Task.Delay(200);
  await RhinoCheck("getComputedStyle(document.querySelector('#captureCaption')).display==='none'&&document.querySelector('#captureLegend').hidden&&document.querySelector('#canvasHost').clientHeight===document.querySelector('#viewport').clientHeight","纯净截图仍有标注遮挡");
  await RhinoCapture(folder,"capture-pure-light.png");await CheckPng("BC pure light");
  await core.ExecuteScriptAsync("document.querySelector('#themeToggle').click();document.querySelector('#canvasHost canvas').dispatchEvent(new KeyboardEvent('keydown',{key:'ArrowLeft',bubbles:true}));");
  await CheckPng("BC pure dark after camera rotation");
  await core.ExecuteScriptAsync("document.querySelector('#captureAnnotations').click();document.querySelector('#toast').hidden=true;");
  Width=1050;Height=730;await RhinoCapture(folder,"capture-compact-dark.png");Width=1440;Height=960;await Task.Delay(200);
  await core.ExecuteScriptAsync("document.dispatchEvent(new KeyboardEvent('keydown',{key:'Escape',bubbles:true}));");await Task.Delay(200);
  await RhinoCheck("!document.body.classList.contains('capture-mode')&&document.activeElement===document.querySelector('#captureMode')&&document.querySelector('#gridToggle').getAttribute('aria-pressed')===window.__captureGrid&&localStorage.getItem('bamboo-v2-'+document.body.dataset.variant)===window.__captureDraft&&document.querySelector('#viewport').classList.contains('view-info-hidden')","退出截图模式没有恢复界面或修改了设计");
  await core.ExecuteScriptAsync("document.querySelector('#toggleInfo').click();document.querySelector('#themeToggle').click();document.querySelector('#fit').click();window.__badImage=null;fetch('/api/save-image',{method:'POST',body:JSON.stringify({name:'bad.png',dataUrl:'data:image/png;base64,eA=='})}).then(async r=>{window.__badImage={ok:r.ok,result:await r.json()};});");
  await RhinoWait("window.__badImage!==null");await RhinoCheck("!window.__badImage.ok&&!!window.__badImage.result.error","无效 PNG 没有正确返回失败");
 }
 async Task SingleCaptureUiTest(string folder){
  var core=web.CoreWebView2;await core.ExecuteScriptAsync("document.querySelector('#captureMode').click();");await Task.Delay(200);
  await RhinoCheck("document.querySelector('#captureCaption').children.length===1&&document.querySelector('#captureCaption').textContent.startsWith('B')&&document.querySelector('#captureLegend').hidden","第五步 B 单模型截图混入 C 或旧应力图例");
  await CheckPng("selected B Rhino preview single model");
  await core.ExecuteScriptAsync("document.querySelector('#exitCapture').click();");
  File.WriteAllText(Path.Combine(folder,"capture-ui-test.json"),JsonSerializer.Serialize(new{pass=true,checks=new[]{"result cards hidden independently","fullscreen viewport layout","labels and legend opt-out","opaque PNG from real native bridge","light and dark themes","rotate camera in capture mode","single B and BC comparison","Esc restores focus/grid/data","invalid PNG fails without hanging"},images=imageChecks},new JsonSerializerOptions{WriteIndented=true}));
 }
}
