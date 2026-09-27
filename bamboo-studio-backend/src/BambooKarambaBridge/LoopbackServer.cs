using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace BambooStudio;
public sealed class LoopbackServer : IDisposable {
 FileStream ownership;readonly TcpListener listener=new(IPAddress.Loopback,0);readonly CancellationTokenSource stop=new();readonly Jobs jobs;readonly SemaphoreSlim slots=new(16);
 readonly string token=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
 readonly string file;
 public int Port=>((IPEndPoint)listener.LocalEndpoint).Port;
 public LoopbackServer(Jobs jobs,string discoveryFile=null){this.jobs=jobs;file=discoveryFile??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"BambooStudio","KarambaBridge","connection.json");}
 public void Start(){
  Directory.CreateDirectory(Path.GetDirectoryName(file));ownership=new FileStream(file+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);listener.Start(16);
  string tmp=file+".tmp";File.WriteAllText(tmp,JsonSerializer.Serialize(new{url=$"http://127.0.0.1:{Port}/",token,pid=Environment.ProcessId,engineId="karamba3d",version="0.8.0"}),new UTF8Encoding(false));File.Move(tmp,file,true);
  // Rhino commands start on the UI thread. Never capture that context for HTTP.
  _=Task.Run(Listen);
 }
 async Task Listen(){
  while(!stop.IsCancellationRequested){
   try{var client=await listener.AcceptTcpClientAsync(stop.Token).ConfigureAwait(false);if(!slots.Wait(0)){client.Dispose();continue;}_=Handle(client);}
   catch(OperationCanceledException){break;}catch(ObjectDisposedException){break;}catch(SocketException){if(stop.IsCancellationRequested)break;}
  }
 }
 async Task Handle(TcpClient client){
  using(client)using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(stop.Token)){
   timeout.CancelAfter(TimeSpan.FromSeconds(15));
   try{
    var stream=client.GetStream();var header=new List<byte>();var one=new byte[1];
    while(header.Count<8192){if(await stream.ReadAsync(one,timeout.Token).ConfigureAwait(false)==0)return;header.Add(one[0]);int n=header.Count;if(n>=4&&header[n-4]==13&&header[n-3]==10&&header[n-2]==13&&header[n-1]==10)break;}
    if(header.Count>=8192){await Reply(stream,431,new{error="请求头过大"},timeout.Token);return;}
    var lines=Encoding.ASCII.GetString(header.ToArray()).Split("\r\n");var request=lines[0].Split(' ');
    if(request.Length!=3||request[2]!="HTTP/1.1"){await Reply(stream,400,new{error="请求格式错误"},timeout.Token);return;}
    var headers=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
    foreach(var line in lines.Skip(1).Where(x=>x.Length>0)){int colon=line.IndexOf(':');if(colon<=0||!headers.TryAdd(line[..colon],line[(colon+1)..].Trim())){await Reply(stream,400,new{error="请求头重复或无效"},timeout.Token);return;}}
    if(headers.ContainsKey("Origin")||headers.ContainsKey("Transfer-Encoding")||!headers.TryGetValue("X-Bamboo-Token",out var supplied)||!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied),Encoding.UTF8.GetBytes(token))){await Reply(stream,403,new{error="拒绝未授权请求"},timeout.Token);return;}
    int length=0;if(headers.TryGetValue("Content-Length",out var raw)&&(!int.TryParse(raw,out length)||length<0||length>32768)){if(length>32768&&length<=65536)await stream.ReadExactlyAsync(new byte[length],timeout.Token).ConfigureAwait(false);await Reply(stream,413,new{error="请求体过大"},timeout.Token);return;}
    var bytes=new byte[length];await stream.ReadExactlyAsync(bytes,timeout.Token).ConfigureAwait(false);
    var result=jobs.Request(request[1],request[0],length>0?Encoding.UTF8.GetString(bytes):null);
    await Reply(stream,result.status,result.data,timeout.Token);
   }catch(OperationCanceledException){}catch(IOException){}catch(Exception e){try{await Reply(client.GetStream(),500,new{error=e.Message},timeout.Token);}catch{}}
   finally{slots.Release();}
  }
 }
 static async Task Reply(NetworkStream stream,int status,object data,CancellationToken ct){
  var body=JsonSerializer.SerializeToUtf8Bytes(data);var header=Encoding.ASCII.GetBytes($"HTTP/1.1 {status} Response\r\nContent-Type: application/json; charset=utf-8\r\nContent-Length: {body.Length}\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n");
  await stream.WriteAsync(header,ct).ConfigureAwait(false);await stream.WriteAsync(body,ct).ConfigureAwait(false);
 }
 public void Dispose(){
  stop.Cancel();listener.Stop();jobs.Dispose();
  try{using var d=JsonDocument.Parse(File.ReadAllText(file));if(d.RootElement.GetProperty("token").GetString()==token)File.Delete(file);}catch{}
  ownership?.Dispose();
 }
}

