export const displayKeys=['RoofType','SideEave','EndEave','RoofOffset','Facets','RidgeWidth','RidgeRise'];
export const parameterKey=p=>JSON.stringify(Object.keys(p).filter(k=>!displayKeys.includes(k)).sort().map(k=>[k,p[k]]));
export class AnalysisClient {
 constructor({fetcher=globalThis.fetch?.bind(globalThis),token='',mode='compare',onChange=()=>{},delay=ms=>new Promise(r=>setTimeout(r,ms))}={}){Object.assign(this,{fetcher,token,mode,onChange,delay,connected:false,busy:false,message:'正在检查本地连接',result:null,resultKey:'',currentKey:'',jobId:null,error:'',serial:0});}
 emit(){this.onChange(this);}
 get fresh(){return !!this.result&&this.resultKey===this.currentKey;}
 setParameters(p){this.currentKey=parameterKey(p);}
 async api(path,method='GET',body){if(!this.fetcher||!this.token)throw Error('请从竹拱工作室 EXE 打开；单独 HTML 不含计算后端。');const response=await this.fetcher(path,{method,headers:{'X-Bamboo-Session':this.token,...(body?{'Content-Type':'application/json'}:{})},body:body?JSON.stringify(body):undefined,signal:AbortSignal.timeout(15000)});const data=await response.json();if(!response.ok){const e=Error(data.error||'服务请求失败');e.status=response.status;throw e;}return data;}
 async connect(){try{const status=await this.api('/api/status');if(!status.connected||status.engineId!=='karamba3d')throw Error('未连接 Karamba3D 后端');this.connected=true;this.message='Karamba3D '+(status.engineVersion||'')+' 已连接 · 提交模型时校验许可证';this.error='';}catch(e){this.connected=false;this.message='尚未连接 Karamba3D';this.error=e.message;}this.emit();}
 async submit(p){if(this.busy)return;const submitted=structuredClone(p),key=parameterKey(submitted),generation=++this.serial;this.setParameters(p);this.busy=true;this.cancelRequested=false;this.result=null;this.error='';this.message='正在提交参数';this.emit();
  try{const {id}=await this.api('/api/jobs','POST',{schema:'bamboo-local/v1',mode:this.mode,parameters:submitted});this.jobId=id;this.connected=true;if(this.cancelRequested)await this.api('/api/jobs/'+id+'/cancel','POST',{});
   while(generation===this.serial){const job=await this.api('/api/jobs/'+id);this.message=job.message+(job.total>1?`（${job.done}/${job.total}）`:``);this.emit();
    if(job.state==='complete'){if(job.result?.engineId!=='karamba3d'||job.result?.schema!=='bamboo-local-result/v1')throw Error('拒绝非 Karamba3D 计算结果');if(parameterKey(job.result.parameters||{})!==key)throw Error('计算结果与提交参数不一致');this.result=job.result;this.resultKey=key;break;}
    if(job.state==='failed')throw Error(job.error||job.message);
    if(job.state==='cancelled'){this.message='已取消，未采用部分搜索结果';break;}
    await this.delay(900);
   }
  }catch(e){if(e.status===503||e.status===502||e.message.includes('非 Karamba3D'))this.connected=false;this.error=e.message;this.message='计算未完成';}
  finally{if(generation===this.serial){this.busy=false;this.jobId=null;this.emit();}}
 }
 async cancel(){this.cancelRequested=true;if(!this.jobId)return;try{await this.api('/api/jobs/'+this.jobId+'/cancel','POST',{});this.message='已申请取消，等待当前候选结束';this.emit();}catch(e){this.error=e.message;this.emit();}}
}
