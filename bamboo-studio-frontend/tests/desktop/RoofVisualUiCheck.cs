using System.Text.Json;
using Microsoft.Web.WebView2.Core;
namespace BambooStudio;
public sealed partial class DesktopHost {
 async Task RoofVisualUiCheck(string folder){
  var checks=new List<string>();Directory.CreateDirectory(folder);
  async Task Run(string js)=>_ = await web.CoreWebView2.ExecuteScriptAsync(js);
  async Task Check(string js,string label){if(await web.CoreWebView2.ExecuteScriptAsync(js)!="true")throw new Exception(label+" | "+await web.CoreWebView2.ExecuteScriptAsync("JSON.stringify({error:window.bambooState().error,stage:window.bambooState().stage,backend:document.querySelector('#backendStatus')?.innerText})"));checks.Add(label);}
  async Task Ready(string js){for(int n=0;n<100;n++){if(await web.CoreWebView2.ExecuteScriptAsync(js)=="true")return;await Task.Delay(200);}throw new Exception("Timed out: "+js);}
  async Task Capture(string name){await Task.Delay(350);using var f=File.Create(Path.Combine(folder,name+".png"));await web.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png,f);}
  try{
   await Ready("typeof window.bambooState==='function'");
   foreach(int stage in new[]{1,2,3,4,5}){
    await Run($"document.querySelector('[data-step=\"{stage}\"]').click();");
    await Check("!/[五5](?:型|种|类)|福州/.test(document.body.innerText)",$"stage {stage} copy has no fixed type count or city branding");
   }
   await Check("document.querySelector('.project-name').textContent.trim().startsWith('竹构原型研究')","project header contains research name without location");
   await Run("document.querySelector('#toast').hidden=true;");
   await Capture("00-step-five-normal");
   Width=1050;Height=730;await Task.Delay(300);
   await Run("document.querySelector('[data-step=\"4\"]').click();");
   await Capture("00-step-four-small");
   await Run("document.querySelector('[data-step=\"5\"]').click();");
   await Capture("00-step-five-small");
   Width=1440;Height=960;await Task.Delay(300);
   await Run("document.querySelector('#solveNow').click();");await Ready("window.bambooState().fresh&&!window.bambooState().busy");
   await Run("document.querySelector('[data-improve=\"5\"]').click();document.querySelector('#next').click();");
   await Check("window.bambooState().stage===6&&!window.bambooState().error&&window.bambooState().previewType===5","compound pointed roof opens with recorded selected scheme");
   await Run("window.roofCheckBefore=JSON.stringify(window.bambooState().editor.roofCandidate);document.querySelector('#toast').hidden=true;");
   await Check("!/[五5](?:型|种|类)|福州/.test(document.body.innerText)","roof and selection copy has no fixed type count or city branding");
   await Capture("01-compound-pointed-follow-light");
   await Run("document.querySelector('#roofToggle').click();");await Capture("02-roof-hidden-light");
   await Run("document.querySelector('#roofToggle').click();document.querySelector('[data-roof=\"1\"]').click();");
   await Capture("03-compound-pointed-gable-light");
   await Run("document.querySelector('#themeToggle').click();");await Capture("04-compound-pointed-gable-dark");
   await Run("document.querySelector('[data-roof=\"0\"]').click();");await Capture("05-compound-pointed-follow-dark");
   await Check("JSON.stringify(window.bambooState().editor.roofCandidate)===window.roofCheckBefore","roof style, theme and visibility do not alter selected analysis result");
   await Run("document.querySelector('#themeToggle').click();");
   foreach(int type in new[]{0,1,2,4}){
    await Run($"document.querySelector('[data-editor-reopen]').click();document.querySelector('#editorBack').click();document.querySelector('[data-improve=\"{type}\"]').click();document.querySelector('#next').click();");
    await Check($"window.bambooState().stage===6&&!window.bambooState().error&&window.bambooState().previewType==={type}",$"type {type} selected roof remains available");
    await Capture("type-"+type+"-follow-light");
   }
   File.WriteAllText(Path.Combine(folder,"visual-check.json"),JsonSerializer.Serialize(new{pass=true,checks,newStructuralSolve=false,transport="existing actual Karamba3D result replay in isolated UI profile; roof display checks only"},new JsonSerializerOptions{WriteIndented=true}));
  }catch(Exception e){await Capture("failure");File.WriteAllText(Path.Combine(folder,"visual-check.json"),JsonSerializer.Serialize(new{pass=false,checks,error=e.ToString()},new JsonSerializerOptions{WriteIndented=true}));}
  finally{BeginInvoke(Close);}
 }
}
