namespace BambooStudio;
public sealed class Job {
 public string Id=Guid.NewGuid().ToString("N");public volatile string State="queued",Message="等待 Rhino / Karamba3D";public volatile int Done,Total=1;public volatile bool Cancel;public object Result;public string Error;public readonly DateTime Started=DateTime.UtcNow;
 public object Snapshot()=>new{id=Id,state=State,message=Message,done=Done,total=Total,error=Error,startedAt=Started,result=State=="complete"?Result:null};
}
public sealed class Jobs : IDisposable {
 readonly object sync=new();readonly Dictionary<string,Job> all=new();Job active;bool disposed;
 readonly Action<Action> dispatch;readonly Func<Input,Job,object> solve;readonly string engineVersion;
 public Jobs(Action<Action> dispatch,Func<Input,Job,object> solve,string version){this.dispatch=dispatch;this.solve=solve;engineVersion=version;}
 public (int status,object data) Request(string path,string method,string body){
  lock(sync){
   if(disposed)return(503,new{error="Rhino 连接正在关闭"});
   if(path=="/api/status"&&method=="GET")return(200,new{connected=true,engineId="karamba3d",engine="Karamba3D",engineVersion,bridgeVersion="0.8.0",busy=active!=null,license="每个实际模型提交时校验许可证"});
   if(path=="/api/jobs"&&method=="POST"){
    if(body==null||body.Length>32768)return(413,new{error="参数 JSON 过大"});
    Input input;try{input=Input.Parse(body);}catch(Exception e){return(400,new{error=e.Message});}
    if(active!=null)return(409,new{error="Rhino 已有计算任务，请等待或取消"});
    var job=new Job();active=job;if(all.Count>=8)all.Remove(all.Keys.First());all[job.Id]=job;
    _=Task.Run(()=>{
     try{dispatch(()=>{
      try{if(job.Cancel)throw new OperationCanceledException();job.State="running";job.Result=solve(input,job);job.Message=job.Cancel?"已取消":"Karamba3D 计算完成";job.State=job.Cancel?"cancelled":"complete";}
      catch(OperationCanceledException){job.Message="计算已取消或超时，未采用部分搜索结果";job.State="cancelled";}
      catch(Exception e){job.Error=e.GetBaseException().Message;job.Message="Karamba3D 未完成求解";job.State="failed";}
      finally{lock(sync)active=null;}
     });}catch(Exception e){job.Error=e.Message;job.State="failed";lock(sync)active=null;}
    });
    return(202,new{id=job.Id});
   }
   var parts=path.Split('/',StringSplitOptions.RemoveEmptyEntries);
   if(parts.Length>=3&&parts[0]=="api"&&parts[1]=="jobs"){
    if(!all.TryGetValue(parts[2],out var job))return(404,new{error="任务不存在或已过期"});
    if(parts.Length==3&&method=="GET")return(200,job.Snapshot());
    if(parts.Length==4&&parts[3]=="cancel"&&method=="POST"){job.Cancel=true;return(200,new{message="已申请取消，将在当前 Karamba 调用返回后停止"});}
   }
   return(404,new{error="接口不存在"});
  }
 }
 public void Dispose(){lock(sync){disposed=true;if(active!=null)active.Cancel=true;}}
}

