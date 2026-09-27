using System.Net.Http;
using System.Text;
using System.Text.Json;
namespace BambooStudio;
// Desktop has no numerical solver. Every analysis request goes to the licensed Rhino host.
public sealed class KarambaClient : IDisposable {
 readonly HttpClient http=new(new SocketsHttpHandler{UseProxy=false,AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(12)};
 string jobConnection;readonly string discovery;
 public KarambaClient(string discoveryFile=null){discovery=discoveryFile??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BambooStudio","KarambaBridge","connection.json");}
 public async Task<(int status,object data)> Request(string path,string method,string body){
  try{
   if(!(path=="/api/status"&&method=="GET"||path=="/api/jobs"&&method=="POST"||System.Text.RegularExpressions.Regex.IsMatch(path,@"^/api/jobs/[a-f0-9]{32}(/cancel)?$")&&(method=="GET"||method=="POST")))return(404,new{error="接口不存在"});
   if(!File.Exists(discovery))return(503,new{error="未发现正在运行的 Karamba3D 连接。"});
   using var d=JsonDocument.Parse(await File.ReadAllTextAsync(discovery));var root=d.RootElement;
   var endpoint=new Uri(root.GetProperty("url").GetString());
   if(endpoint.Scheme!="http"||endpoint.Host!="127.0.0.1"||endpoint.AbsolutePath!="/"||endpoint.Port<=0)throw new Exception("本机连接配置无效");
   string token=root.GetProperty("token").GetString();
   if(string.IsNullOrEmpty(token)||token.Length!=64)throw new Exception("本机连接凭据无效");
   string connection=endpoint+token;
   if(path.StartsWith("/api/jobs/")&&jobConnection!=connection)throw new Exception("Rhino 连接已重启，请重新提交计算");
   using var request=new HttpRequestMessage(new HttpMethod(method),new Uri(endpoint,path));
   request.Headers.Add("X-Bamboo-Token",token);
   if(body!=null)request.Content=new StringContent(body,Encoding.UTF8,"application/json");
   using var response=await http.SendAsync(request);
   var json=await response.Content.ReadAsStringAsync();using var result=JsonDocument.Parse(json);
   if(response.IsSuccessStatusCode&&path=="/api/status"&&result.RootElement.GetProperty("engineId").GetString()!="karamba3d")throw new Exception("连接的后端不是 Karamba3D");
   if(response.IsSuccessStatusCode&&path=="/api/status"){
    var bridge=result.RootElement.TryGetProperty("bridgeVersion",out var v)?v.GetString():null;
    if(!Version.TryParse(bridge,out var version)||version<new Version(0,8,0))return(503,new{error="已找到 Rhino，但正在运行旧连接插件（"+(bridge??"版本未知")+"）。请保存 Rhino / GH 文件，换用配套 0.8.0 插件并重启 Rhino，再运行 BambooKarambaStart。旧插件不支持本版多参数候选列表。",code="RHINO_BRIDGE_UPDATE_REQUIRED"});
   }
   if(response.IsSuccessStatusCode&&path=="/api/jobs")jobConnection=connection;
   return((int)response.StatusCode,result.RootElement.Clone());
  }catch(TaskCanceledException){return(503,new{error="等待 Rhino 响应超时（12 秒）。这不表示 Karamba3D 未安装；Rhino 可能仍在计算。请确认已换用 0.8.0 或更新的连接插件。",code="RHINO_TIMEOUT"});}
  catch(HttpRequestException e){return(503,new{error="无法访问 Rhino 本机接口："+e.GetBaseException().Message+"。请确认 BambooKarambaStart 已启动。",code="RHINO_NETWORK_ERROR"});}
  catch(Exception e){return(503,new{error="Karamba3D 连接不可用："+e.Message,code="RHINO_CONNECTION_ERROR"});}
 }
 public void Dispose()=>http.Dispose();
}

