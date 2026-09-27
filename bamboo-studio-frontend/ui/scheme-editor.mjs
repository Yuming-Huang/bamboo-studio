import {AnalysisClient, parameterKey} from './client.mjs';

const copy=x=>structuredClone(x);
const exact=p=>({...copy(p),Run:true,Optimize:false,CrossCompare:false});
const numeric=(key,label,min,max,step=1,unit='mm',help='')=>({key,label,min,max,step,unit,help});
const options=(key,label,values,help='')=>({key,label,options:values,help});
const toggle=(key,label,help='')=>({key,label,toggle:true,help});
const strengths=['Fc','Ft','Fb'].map((k,i)=>numeric(k,['抗压阈值','抗拉阈值','抗弯阈值'][i],0,300,1,'MPa','三项全 0 表示强度未评估。'));

// Only parameters with an implemented effect on the selected topology are exposed.
export function editorGroups(p){
 const a2=p.type===5;
 const geometry=[numeric('W','拱脚跨度',2000,12000,100),numeric('H','拱顶总高',500,12000,50)];
 if(!a2){geometry.push(options('Foot','拱脚形式',['曲线落地','直立下部','倾斜下部']));if(p.Foot)geometry.push(numeric('LegH','起拱高度',100,6000,50));if(p.Foot===2)geometry.push(numeric('LegAngle','下部内倾角',0,40,1,'°'));}
 if(p.type===2)geometry.push(numeric('CrossAngle','交叉拱倾角',5,45,1,'°','保持真实圆弧与三维交点。'));
 if(p.type===4)geometry.push(numeric('TrussDepth','上下弦高差',100,1500,50),numeric('Panels','腹杆分格',4,24,2,'格','采用偶数分格。'));
 if(a2)geometry.push(numeric('PointE','每侧结构外挑',50,2000,50),numeric('PointSlope','上弦坡度系数',.35,.95,.05,'','挑端高度由几何联动确定。'),numeric('PointFootGap','拱脚杆件净间距',0,500,10),numeric('PointPanels','每侧腹杆分格',6,12,1,'格'));
 const sections=[numeric('ArchD',a2?'上弦外径':'主杆外径',40,400,10),numeric('ArchT',a2?'上弦壁厚':'主杆壁厚',1,80,1)];
 if(a2)sections.push(numeric('PointLowerD','下弦／回接弧／直柱外径',40,400,10),numeric('PointLowerT','下弦／直柱壁厚',1,80),numeric('PointWebD','腹杆外径',30,300,10),numeric('PointWebT','腹杆壁厚',1,60));
 const wind=[options('WindModel','风荷载模式',['整面下压／吸力','风向分区'])];
 if(p.WindModel===0)wind.push(numeric('Cp','整面下压系数',0,5,.1,''),numeric('Cs','整面吸力系数',-5,0,.1,''));
 else{
  wind.push(options('WindDirection','水平吹向',['−X → +X','+X → −X','−Y → +Y','+Y → −Y','自定义角度']),options('WindEnds','端面受风',['敞开屋面','加两端荷载面']),toggle('WindReverse','同时校核反向风'));
  if(p.WindDirection===4)wind.push(numeric('WindAngle','吹向角',0,360,5,'°'));
  for(const [k,label] of [['WindRoofWindward','屋面迎风区'],['WindRoofLeeward','屋面背风区'],['WindRoofParallel','屋面平行区'],...(p.WindEnds===1?[['WindEndWindward','端面迎风区'],['WindEndLeeward','端面背风区'],['WindEndParallel','端面平行区']]:[])])wind.push(numeric(k,label,-5,5,.1,'','正压向内，负压为吸力。'));
 }
 return [
  {title:'形态与构造',open:true,fields:geometry,help:a2?'A2 保留两条落地弧、中央直柱与向外下斜连接杆。':p.type===1?'两段真实圆弧在拱顶相接；没有另设独立的尖角参数。':''},
  {title:'杆件截面',open:true,fields:sections,help:p.type===4?'本模型上下弦和腹杆共用主杆截面，不能独立指定。':'壁厚必须小于对应外径的一半。'},
  {title:'檩条',fields:[numeric('PurlinCount','檩条道数',3,61,2,'道','奇数道；修改后与新模型一起计算。'),numeric('PurlinD','檩条外径',40,300,10),numeric('PurlinT','檩条壁厚',1,60)]},
  {title:'材料与强度',fields:[numeric('E','等效弹性模量',1000,30000,50,'MPa'),numeric('Nu','等效泊松比',0,.49,.01,''),numeric('Density','密度',100,1500,1,'kg/m³'),numeric('G','剪切模量',0,30000,50,'MPa','0 时按 E 与泊松比推导，用于扭转刚度。'),...strengths]},
  {title:'荷载',fields:[numeric('RoofG','屋面附加恒荷载',0,3,.05,'kN/m²','按分析屋面实际面积；杆件自重另计。'),numeric('RoofQ','屋面活荷载',0,3,.05,'kN/m²'),numeric('W0','基本风压',0,3,.05,'kN/m²','0 时不施加风荷载。'),numeric('MuZ','高度系数',.1,5,.1,''),numeric('Beta','风振系数',.1,5,.1,''),numeric('SnowQ','屋面积雪荷载',0,3,.05,'kN/m²')]},
  {title:'风向与分区',fields:wind},
  {title:'支座、节点与计算',fields:[options('Support','拱脚支座',['固定平动 · 转动自由','全部固定']),options('Joints','理想节点',['理想刚接','拱顶弯曲释放','檩条端部弯曲释放','两者均释放']),toggle('Buckle','增加线性屈曲诊断','由 Karamba3D 计算线性屈曲因子。'),numeric('Mesh','单元细分',1,4,1,'级'),numeric('Limit','位移初筛阈值',0,1000,1,'mm','0 使用跨度 W / 250（软件演示初筛值，非通用规范限值）；正值为自定限值。'),numeric('Pmax','最大檩距阈值',0,3000,100,'mm','0 不限制。')]}
 ];
}

export class SchemeEditor {
 constructor({onChange=()=>{},...clientOptions}={}){
  this.onChange=onChange;this.verified=false;this.active=false;this.record=null;this.before=null;this.beforeBundle=null;this.running=false;this.compare=true;this.stress=true;this.history=[];
  this.client=new AnalysisClient({...clientOptions,mode:'single',onChange:()=>onChange()});
 }
 start(result,bundle,parentKey,origin){
  if(this.busy)throw Error('请先等待当前计算完成。');
  if(!result?.solved)throw Error('请先选择已计算的方案。');
  this.verified=false;this.record={type:result.type,base:exact(result.parameters),draft:exact(result.parameters),parentKey,origin,selectedSide:"current"};
  this.before=copy(result);this.beforeBundle=copy(bundle);this.client.result=null;this.client.error='';this.client.setParameters(this.record.draft);
  this.active=true;this.compare=true;this.stress=true;this.history=[];
 }
 restore(snapshot){
  if(!snapshot||![0,1,2,4,5].includes(snapshot.type)||snapshot.base?.type!==snapshot.type||snapshot.draft?.type!==snapshot.type)return;
  this.verified=false;this.record={type:snapshot.type,base:exact(snapshot.base),draft:exact(snapshot.draft),parentKey:snapshot.parentKey,origin:['recommended','improvement'].includes(snapshot.origin)?snapshot.origin:'manual',selectedSide:snapshot.selectedSide==='before'?'before':'current'};
  this.before=null;this.beforeBundle=null;this.client.result=null;this.client.setParameters(this.record.draft);this.active=false;this.history=[];
 }
 snapshot(){return this.record?copy(this.record):null;}
 get parameters(){return this.record?.draft;}
 get busy(){return this.running||this.client.busy;}
 get current(){
  if(!this.record||this.busy)return null;
  if(this.client.fresh)return this.client.result?.C||null;
  return this.before&&parameterKey(this.record.draft)===parameterKey(this.record.base)?this.before:null;
 }
 get bundle(){return this.client.fresh?this.client.result:this.beforeBundle;}
 get usable(){return this.verified&&this.current?.solved&&this.current.pass&&!(this.current.warnings?.length)?this.current:null;}
 // Roof selection is independent of structural screening. Never relabel a failed result as passed.
 get selectedSide(){return this.record?.selectedSide||'current';}
 get roofCandidate(){if(this.busy)return null;const r=this.selectedSide==='before'?this.before:this.current;return r?.solved?r:null;}
 get roofBundle(){return this.selectedSide==='before'?this.beforeBundle:this.bundle;}
 selectForRoof(side){if(this.busy||!this.record||!['before','current'].includes(side))return false;const r=side==='before'?this.before:this.current;if(!r?.solved)return false;this.record.selectedSide=side;return true;}
 get changes(){
  if(!this.record)return [];
  const labels=new Map(editorGroups(this.parameters).flatMap(g=>g.fields).map(f=>[f.key,f]));
  return Object.keys(this.record.draft).filter(k=>JSON.stringify(this.record.draft[k])!==JSON.stringify(this.record.base[k])).map(key=>({key,label:labels.get(key)?.label||key,before:this.record.base[key],after:this.record.draft[key],unit:labels.get(key)?.unit||''}));
 }
 get sharedMax(){return Math.max(this.before?.solved?this.before.stressMPa||0:0,this.current?.solved?this.current.stressMPa||0:0,.001);}
 canResume(parentKey){return !!this.record&&this.record.parentKey===parentKey;}
 checkpoint(){if(this.record&&!this.busy){this.history.push(copy(this.parameters));if(this.history.length>30)this.history.shift();}}
 edit(key,value){
  if(this.busy||!this.record)return;
  const f=editorGroups(this.parameters).flatMap(g=>g.fields).find(f=>f.key===key);if(!f)throw Error('此参数不属于当前构型的编辑项。');
  this.record.selectedSide="current";this.record.draft[key]=value;
  if(key==='PurlinCount')this.record.draft.PMode=1;
  this.client.error='';this.client.setParameters(this.parameters);
 }
 undo(){if(this.busy||!this.history.length)return;this.record.selectedSide="current";this.record.draft=this.history.pop();this.client.error='';this.client.setParameters(this.parameters);}
 reset(){if(this.busy||!this.record)return;this.checkpoint();this.record.selectedSide="current";this.record.draft=copy(this.record.base);this.client.error='';this.client.setParameters(this.parameters);}
 clear(){if(this.busy)return;this.record=null;this.before=null;this.beforeBundle=null;this.client.result=null;this.active=false;this.history=[];}
 validate(core,parameters=this.parameters){
  if(!this.record)return '尚未开始编辑';
  const p=parameters;
  for(const f of editorGroups(p).flatMap(g=>g.fields)){
   const v=p[f.key];if(f.toggle){if(typeof v!=='boolean')return f.label+'须为开关';}
   else if(!Number.isFinite(v)||v<(f.options?0:f.min)||v>(f.options?f.options.length-1:f.max))return f.label+'超出允许范围';
  }
  if(p.PurlinCount%2!==1)return '檩条道数须为奇数';
  if(p.type===4&&p.Panels%2!==0)return '复合圆拱腹杆分格须为偶数';
  for(const k of ['Mesh','PointPanels'])if(!Number.isInteger(p[k]))return '单元细分与腹杆分格须为整数';
  if(p.W>p.SiteW||p.H>p.SiteH)return '跨度或拱高超过场地边界';
  if(p.type===5&&p.W+2*p.PointE>p.SiteW)return '跨度加两侧结构外挑超过场地宽度';
  try{core.build(p);}catch(e){return e.message;}
  return '';
 }
 async calculate(){
  if(this.busy||!this.record)return;
  this.running=true;this.stopped=false;this.onChange();
  try{
   if(!this.verified||!this.before?.solved){
    await this.client.submit(this.record.base);
    if(this.stopped||!this.client.fresh||!this.client.result?.C?.solved)return;
    this.before=copy(this.client.result.C);this.beforeBundle=copy(this.client.result);this.verified=true;
   }
   if(!this.stopped&&parameterKey(this.record.draft)!==parameterKey(this.record.base))await this.client.submit(this.record.draft);
  }finally{this.client.setParameters(this.parameters);this.running=false;this.onChange();}
 }
 async cancel(){this.stopped=true;await this.client.cancel();}
}

export function stressModel(core,parameters,result,{stress=false,max=.001,roof=false}={}){
 const g=core.build(parameters);
 if(!roof){g.patches=[];g.supports=[];}
 if(stress&&result?.solved){
  g.members=result.members.map(m=>({points:[m.a,m.b],role:m.role,diameterMm:m.diameterMm,index:m.role==='purlin'?-1:+(m.id.match(/^[A-Z]+(\d+)/)?.[1]??-1),stress:m.stressMPa}));g.stressMax=max;
 }
 return g;
}
