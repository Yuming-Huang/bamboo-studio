/* Browser geometry only. Adapted from the local ArchRoofLibrary math rules.
 * This is NOT a finite-element solver or a round-trip Grasshopper exporter. */
const ArchCore = (() => {
 'use strict';
 const defaults={"SideReserve":1200,"HeightSearch":true,"MaxCandidates":300,"Diameters":[100,150,300],"CrossAngles":[25,15,35],"TrussDepths":[450,300,600],"PanelCounts":[8,6,10],"PointEs":[1200],"PointSlopes":[0.9],"PointFootGaps":[10],"PointPanelCounts":[6],PointedVersion:3,PointE:1200,PointFootGap:10,PointSlope:.9,PointTip:1800,PointDepth:450,PointPanels:6,PointLowerD:120,PointLowerT:10,PointWebD:60,PointWebT:6,SiteL:17000,SiteW:8000,SiteH:6500,SiteEnd:2500,AreaTol:.15,SearchD2:150,SearchD3:300,type:0,W:6000,H:4200,Foot:1,LegH:1800,LegAngle:12,CrossAngle:25,TrussDepth:450,Panels:8,L:12000,S:2000,End:true,Axis:0,Bend:16,HeightMode:0,Amplitude:240,PMode:0,P:1000,PurlinCount:9,RoofType:0,SideEave:0,EndEave:0,RoofOffset:0,Facets:8,RidgeWidth:1200,RidgeRise:450,ArchD:100,ArchT:10,PurlinD:100,PurlinT:10,E:10850,Nu:.4,Density:644,RoofG:.2,RoofQ:.5,W0:.7,WindModel:1,WindDirection:0,WindEnds:0,WindReverse:false,WindAngle:0,WindRoofWindward:.8,WindRoofLeeward:-.5,WindRoofParallel:-.7,WindEndWindward:.8,WindEndLeeward:-.5,WindEndParallel:-.7,Run:false,Optimize:false,Buckle:false,G:0,Fc:0,Ft:0,Fb:0,MuZ:1,Beta:1,Cp:.8,Cs:-1,SnowQ:0,Support:0,Joints:0,Mesh:2,Limit:0,Pmax:0,Hmin:.9,Hmax:1.1,Samples:3,CrossCompare:false};
 const names=['单圆拱','单尖拱','交叉拱','侧部交叉尖拱','复合圆拱','复合尖拱'];
 const roofNames=['随拱外挑','切线双坡'];
 const v=(x,y,z)=>({x,y,z}),add=(a,b)=>v(a.x+b.x,a.y+b.y,a.z+b.z),sub=(a,b)=>v(a.x-b.x,a.y-b.y,a.z-b.z),mul=(a,s)=>v(a.x*s,a.y*s,a.z*s),dot=(a,b)=>a.x*b.x+a.y*b.y+a.z*b.z;
 const cross=(a,b)=>v(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x),len=a=>Math.hypot(a.x,a.y,a.z),dist=(a,b)=>len(sub(a,b));
 const range=(n,a=0,b=1)=>Array.from({length:n+1},(_,i)=>a+(b-a)*i/n),need=(ok,msg)=>{if(!ok)throw Error(msg);};
 const unique=xs=>xs.sort((a,b)=>a-b).filter((x,i,a)=>!i||Math.abs(x-a[i-1])>1e-9);
 function circle2(a,m,b){
  const d=2*(a.x*(m.z-b.z)+m.x*(b.z-a.z)+b.x*(a.z-m.z));need(Math.abs(d)>1e-6,'下弦圆弧退化，请调整肩部弦间距。');
  const aa=a.x*a.x+a.z*a.z,mm=m.x*m.x+m.z*m.z,bb=b.x*b.x+b.z*b.z;
  const x=(aa*(m.z-b.z)+mm*(b.z-a.z)+bb*(a.z-m.z))/d,z=(aa*(b.x-m.x)+mm*(a.x-b.x)+bb*(m.x-a.x))/d,r=Math.hypot(a.x-x,a.z-z),start=Math.atan2(a.z-z,a.x-x);
  const positive=t=>(t%(2*Math.PI)+2*Math.PI)%(2*Math.PI),end=positive(Math.atan2(b.z-z,b.x-x)-start),mid=positive(Math.atan2(m.z-z,m.x-x)-start),sweep=mid<=end?end:end-2*Math.PI;
  return t=>t===0?a:t===1?b:{x:x+r*Math.cos(start+sweep*t),z:z+r*Math.sin(start+sweep*t)};
 }
 function spreadProfile(c,h){
  const gap=c.PointFootGap,offset=c.PointLowerD+gap,a=c.W/2-offset,half=c.W/2+c.PointE;
  need(gap>=0&&gap<=500&&a>100&&c.PointE>offset,'拱脚净间距或杆径过大：请增加跨度、外挑，或减小间距。');
  need(c.PointSlope>=.35&&c.PointSlope<=.95,'上弦坡度系数须为0.35—0.95。');
  need(Number.isInteger(c.PointPanels)&&c.PointPanels>=3&&c.PointPanels<=12,'每侧分格须为3—12。');
  const cx=Math.max(.25*a,(h*h-a*a)/(2*a)),cz=(h*h-a*a-2*a*cx)/(2*h),radius=Math.hypot(a+cx,cz);
  const start=(Math.atan2(-cz,-a-cx)+2*Math.PI)%(2*Math.PI),end=Math.atan2(h-cz,-cx),ang=(start+end)/2;
  const lower=circle2({x:-a,z:0},{x:cx+radius*Math.cos(ang),z:cz+radius*Math.sin(ang)},{x:0,z:h});
  const slope=c.PointSlope*cx/(h-cz),rise=slope*half,tipZ=h-rise,outerFoot=-c.W/2-offset,run=c.PointE-offset;
  need(tipZ>run+.001,'外挑过长或拱高过低，回接弧无法保持单调；请缩短外挑或提高拱高。');
  const rr=(run*run+tipZ*tipZ)/(2*run),rc=outerFoot-rr,theta=Math.atan2(tipZ,rr-run);
  const tip={x:-half,z:tipZ},ret=circle2(tip,{x:rc+rr*Math.cos(theta/2),z:rr*Math.sin(theta/2)},{x:outerFoot,z:0});
  const at=u=>u===.5?{x:0,z:h}:{x:(2*u-1)*half,z:h-Math.abs(2*u-1)*rise};
  // The outer truss boundary includes the complete column before turning along the upper chord.
  const columnHead={x:-c.W/2,z:h-rise/half*c.W/2},columnLength=columnHead.z,total=columnLength+Math.hypot(c.W/2,h-columnHead.z),knee=columnLength/total,n=c.PointPanels,k=Math.max(1,Math.min(n-1,Math.floor(knee*n+.5)));
  const outerNodes=range(n).map((_,j)=>{const t=j===k?knee:j/n;if(j===k)return columnHead;if(t<knee)return {x:-c.W/2,z:t*total};const f=(t-knee)/(1-knee);return {x:-c.W/2*(1-f),z:columnHead.z+(h-columnHead.z)*f};});
  return {at,lower,ret,outerNodes,columnHead,half,rise,leg:0,h,halfLength:Math.hypot(half,rise),offset,tipZ,lowerCircle:{x:cx,z:cz,r:radius},footXs:[outerFoot,-c.W/2,-a]};
 }
 function compoundProfile(c,h){
  if(c.PointedVersion>=2)return spreadProfile(c,h);
  need(c.W>0&&h>0&&c.PointE>.01&&c.PointTip>.01&&c.PointTip<h-.01&&c.PointDepth>.01,'复合尖拱要求外挑、挑端高度和肩部间距为正，且挑端低于拱顶。');
  need(Number.isInteger(c.PointPanels)&&c.PointPanels>=3&&c.PointPanels<=12,'复合尖拱每侧分格为3—12。');
  const half=c.W/2+c.PointE,rise=h-c.PointTip,foot={x:-c.W/2,z:0},tip={x:-half,z:c.PointTip},crown={x:0,z:h};
  need(h-rise/half*c.W/4-c.PointDepth>h/2+.001,"肩部间距过大，下弦已不再向上拱起，请减小肩部间距。");
  const lower=circle2(foot,{x:-c.W/4,z:h-rise/half*c.W/4-c.PointDepth},crown),ret=circle2(tip,{x:(tip.x+foot.x)/2+.2*c.PointE,z:c.PointTip/2},foot);
  let prev=lower(0);for(let i=1;i<=200;i++){const p=lower(i/200);need(p.x>prev.x&&p.z>prev.z&&p.x<=.001&&p.z<=h+.001&&(i===200||p.z<h+rise/half*p.x-.001),'肩部弦间距使下弦回折或穿过上弦，请调整挑端高度或肩部间距。');prev=p;}
  for(let i=1;i<200;i++){const p=ret(i/200);need(p.x>=-half-.001&&p.x<=foot.x+.001&&p.z>=-.001&&p.z<=c.PointTip+.001,'挑端回接弧超出边界，请减小外挑或提高挑端。');}
  const at=u=>u===0?tip:u===1?{x:half,z:c.PointTip}:u===.5?crown:{x:(2*u-1)*half,z:h-Math.abs(2*u-1)*rise};
  return {at,lower,ret,half,rise,leg:0,h,halfLength:Math.hypot(half,rise)};
 }
 // A2: two rods per wing. Both descend outward at the same 15 degree angle.
 function wingLinks(c,p){
  if(c.PointedVersion<3)return [];
  const slope=Math.tan(Math.PI/12),eligible=p.outerNodes.slice(1,-1).filter(q=>Math.abs(q.x+c.W/2)<.001&&q.z<p.columnHead.z-.001&&p.ret(0).z-q.z+slope*Math.abs(p.ret(0).x-q.x)>.001);
  need(eligible.length>=2,'A2外挑区需要两处直柱腹杆节点，请增加每侧分格或调整拱高／外挑。');
  const indices=[Math.round((eligible.length-1)/3),Math.round(2*(eligible.length-1)/3)];if(indices[0]===indices[1])indices[1]=Math.min(eligible.length-1,indices[0]+1);
  return indices.map(i=>{const inner=eligible[i];let lo=0,hi=1;for(let k=0;k<60;k++){const mid=(lo+hi)/2,q=p.ret(mid),value=q.z-inner.z+slope*Math.abs(q.x-inner.x);if(value>0)lo=mid;else hi=mid;}const t=(lo+hi)/2,outer=p.ret(t);need(outer.z>0&&outer.z<inner.z-.001&&outer.x<inner.x-.001,'A2连接杆不能向外下斜，请调整分格或形态。');return {inner,outer,t};});
 }
 function pointedMembers(c,h){
  const p=compoundProfile(c,h),members=[],n=c.PointPanels;
  const add=(points,role,kind='line')=>{const diameterMm=(role==='web'||role==='wing')?c.PointWebD:role==='lower'||role==='return'||role==='column'?c.PointLowerD:c.ArchD,wallMm=(role==='web'||role==='wing')?c.PointWebT:role==='lower'||role==='return'||role==='column'?c.PointLowerT:c.ArchT;members.push({points,role:(role==='web'||role==='wing')?'web':'arch',part:role,diameterMm,wallMm,segments:[{kind,points:kind==='arc'?[points[0],points[(points.length-1)/2],points.at(-1)]:points}]});};
  add([p.at(0),p.at(.5)],'upper');add([p.at(.5),p.at(1)],'upper');
  for(let side=0;side<2;side++){
   const map=q=>({x:side?-q.x:q.x,z:q.z}),lower=j=>map(p.lower(j/n)),upper=j=>{const q=p.lower(j/n),x=c.PointedVersion>=2&&j===0?-c.W/2:q.x;return map({x,z:h+p.rise/p.half*x});};
   add(range(32).map(t=>map(p.lower(t))),'lower','arc');add(range(32).map(t=>map(p.ret(t))),'return','arc');
   if(c.PointedVersion>=2){
    const u=j=>map(p.outerNodes[j]);
    if(c.PointedVersion>=3)for(const link of wingLinks(c,p))add([map(link.inner),map(link.outer)],'wing');
    add([map({x:-c.W/2,z:0}),map(p.columnHead)],'column');
    const center=map(p.lowerCircle),clear=(a,b)=>{const x=b.x-a.x,z=b.z-a.z,t=Math.max(0,Math.min(1,((center.x-a.x)*x+(center.z-a.z)*z)/(x*x+z*z)));return Math.hypot(a.x+t*x-center.x,a.z+t*z-center.z)>=p.lowerCircle.r-.001;};
    for(let j=1;j<n;j++){need(clear(lower(j),u(j)),'当前分格连杆穿过下弦，请调整分格或上弦坡度。');add([lower(j),u(j)],'web');if(j<n-1){let top=(j+n)%2===0,pts=()=>[top?u(j):lower(j),top?lower(j+1):u(j+1)];if(!clear(...pts()))top=!top;need(clear(...pts()),'当前分格斜杆穿过下弦，请调整分格或上弦坡度。');add(pts(),'web');}}
   }else for(let j=0;j<n;j++){add([lower(j),upper(j)],'web');if(j<n-1)add([j%2===0?upper(j):lower(j),j%2===0?lower(j+1):upper(j+1)],'web');}
  }
  return members;
 }
 function profile(c,h,inner=false,physical=false){
  if(c.type===5&&c.PointedVersion>=1)return compoundProfile(c,h);
  const pointed=c.type%2===1,co=physical?Math.cos(c.CrossAngle*Math.PI/180):1;
  const leg=c.Foot===0?0:c.LegH/co,angle=c.Foot===2?Math.atan(Math.tan(c.LegAngle*Math.PI/180)*co):0;
  h=(h-(inner?c.TrussDepth:0))/co;
  const half=c.W/2-leg*Math.tan(angle),rise=h-leg;
  need(c.W>0&&h>0&&rise>0&&half>1,'尺寸无效：请检查总高、下部高度和脚点跨度。');
  need(pointed||rise<=half,'圆拱矢高不能超过上部半跨；请减小总高或提高起拱高度。');
  const cx=pointed?Math.max(half,(rise*rise-half*half)/(2*half)):0;
  const cz=pointed?leg+(rise*rise-half*half-2*half*cx)/(2*rise):leg+(rise*rise-half*half)/(2*rise);
  const radius=Math.hypot(-half-cx,leg-cz),start=Math.atan2(leg-cz,-half-cx),sweep=Math.atan2(h-cz,-cx)-start;
  function at(u){if(u===0)return {x:-half,z:leg};if(u===1)return {x:half,z:leg};if(u===.5)return {x:0,z:h};const a=start+sweep*(u>.5?2*(1-u):2*u);return {x:(u>.5?-1:1)*(cx+radius*Math.cos(a)),z:cz+radius*Math.sin(a)};}
  return {at,half,rise,leg,h,halfLength:radius*Math.abs(sweep)};
 }
 function frameAt(c,s){const t=c.Axis?c.Bend*Math.PI/180*s/c.L:0,r=c.Axis?c.L/(c.Bend*Math.PI/180):0;return {origin:c.Axis?v(r*(1-Math.cos(t)),r*Math.sin(t)+(c.SiteL>0?c.SiteEnd:0),0):v(0,s+(c.SiteL>0?c.SiteEnd:0),0),across:v(Math.cos(t),-Math.sin(t),0),forward:v(Math.sin(t),Math.cos(t),0)};}
 function mapAt(p,f,c,i,u){const q=p.at(u),t=Math.floor(c.type/2)===1?c.CrossAngle*Math.PI/180:0;return add(add(f.origin,mul(f.across,q.x)),add(mul(f.forward,q.z*Math.sin(t)*(i%2?-1:1)),v(0,0,q.z*Math.cos(t))));}
 function normalize(raw){const c=Object.fromEntries(Object.entries(defaults).map(([k,v])=>[k,Array.isArray(v)?[...v]:v]));if(raw.SiteL===undefined)c.SiteL=0;if(raw.PointedVersion===undefined)c.PointedVersion=0;if(raw.WindModel===undefined)c.WindModel=0;for(const key of Object.keys(defaults)){if(raw[key]===undefined)continue;if(Array.isArray(defaults[key])){need(Array.isArray(raw[key])&&raw[key].every(x=>typeof x==='number'&&Number.isFinite(x)),key+'须为数值列表。');c[key]=[...raw[key]];}else if(typeof defaults[key]==='boolean'){need(typeof raw[key]==='boolean',key+'必须为布尔值。');c[key]=raw[key];}else{need(typeof raw[key]==='number'&&Number.isFinite(raw[key]),key+'必须为有限数值。');c[key]=raw[key];}}if(c.SiteL>0)c.L=c.SiteL-2*c.SiteEnd;return c;}
 function validate(c){
  need([0,1].includes(c.WindModel)&&[0,1].includes(c.WindEnds)&&[0,1,2,3,4].includes(c.WindDirection),"风荷载模式无效。");need(c.WindAngle>=0&&c.WindAngle<=360,"风向角应为0—360度。");for(const k of ["WindRoofWindward","WindRoofLeeward","WindRoofParallel","WindEndWindward","WindEndLeeward","WindEndParallel"])need(c[k]>=-5&&c[k]<=5,"净压系数应为−5至5。");
  for(const k of ['type','Foot','Axis','HeightMode','PMode','RoofType','Panels','Facets','PurlinCount'])need(Number.isInteger(c[k]),k+'必须为整数。');
  need(c.type>=0&&c.type<6&&c.Foot>=0&&c.Foot<3,'未知原型或下部形式。');
  need([0,1].includes(c.Axis)&&[0,1].includes(c.HeightMode)&&[0,1].includes(c.PMode),'未知布置模式。');
  need(c.SiteL===0||(c.SiteL>=2000&&c.SiteL<=42000&&c.SiteEnd>=0&&c.SiteEnd<=6000),'场地长度须为2—42m，端部预留为每端0—6m。');need(c.L>=2000&&c.L<=42000,'场地长度扣除两端预留后，阵列可用长度须为2—42m；请在第三步调整端部预留。');need(c.S>=300&&c.S<=12000&&c.S<=c.L,'榀距不得超过扣除两端预留后的阵列可用长度。');
  need(c.W>=1000&&c.W<=20000&&c.H>=500&&c.H<=16000,'跨度应为1—20m，总高为0.5—16m。');
  need(c.LegH>=0&&(!c.Foot||c.LegH<c.H)&&c.LegAngle>=0&&c.LegAngle<=45,'下部高度须低于总高，腿角为0—45°。');
  need(c.RoofType>=0&&c.RoofType<=2,'屋面类型只能是0、1或2。');
  need(c.CrossAngle>0&&c.CrossAngle<=45&&c.TrussDepth>0,'交叉角为0—45°，桁架高差必须为正。');
  need(c.Panels>=4&&c.Panels<=24&&c.Panels%2===0,'桁架分格数须为4—24偶数。');
  need(c.P>=200&&c.P<=3000&&c.PurlinCount>=3&&c.PurlinCount<=61&&c.PurlinCount%2===1,'檩条间距应为200—3000mm，道数为3—61奇数。');
  need(c.Amplitude>=0&&c.Amplitude<=2000&&c.Bend>0&&c.Bend<=60,'起伏幅度为0—2000mm，曲轴总转角为0—60°。');
  need(c.E>0&&c.Density>0&&c.Nu>-.99&&c.Nu<.5,'材料初值无效。');
  for(const role of ['Arch','Purlin','PointLower','PointWeb'])need(c[role+'D']>0&&c[role+'D']<=600&&c[role+'T']>0&&2*c[role+'T']<c[role+'D'],'外径必须大于两倍壁厚且不超过600mm。');
  need([0,1,2,3].includes(c.PointedVersion),'未知复合尖拱几何版本。');
  need(c.RoofG>=0&&c.RoofQ>=0&&c.W0>=0,'荷载初值不能为负数。');
 }
 function build(raw){
  const c=normalize(raw);validate(c);const structure=Math.floor(c.type/2),isCross=structure===1;
  need(!isCross||(!c.Axis&&!c.HeightMode),'交叉拱首版只支持直轴、等高。请将轴线与高度模式都改为统一直线布置。');
  const stations=[0];for(let s=c.S;s<=c.L+.001;s+=c.S)stations.push(Math.min(c.L,s));if(c.End&&c.L-stations.at(-1)>.001)stations.push(c.L);
  need(stations.length<=48,'网页演示最多显示48榀，请增加榀距。');
  const heights=stations.map(s=>c.H+(c.HeightMode?c.Amplitude*Math.sin(Math.PI*2*s/c.L):0)),frames=stations.map(s=>frameAt(c,s));
  const ps=heights.map(h=>profile(c,h,false,isCross)),inners=structure===2?heights.map(h=>profile(c,h,true,false)):[];
  const railCount=c.PMode?c.PurlinCount:2*Math.max(1,Math.ceil(Math.max(...ps.map(p=>p.halfLength))/c.P))+1;
  need(railCount<=61,'网页檩条超过61道，请增大间距。');
  const point=(i,u,inner=false)=>mapAt(inner?inners[i]:ps[i],frames[i],c,i,u);
  const members=[],feet=[],foundations=[],cuts=[0,.5,1],crossNodes=[];
  const addMember=(points,role,index=-1)=>members.push({points,role,index});
  for(let i=0;i<frames.length;i++){
   if(c.type===5&&c.PointedVersion>=1){
    const f=frames[i],map=p=>add(f.origin,add(mul(f.across,p.x),v(0,0,p.z)));
    for(const m of pointedMembers(c,heights[i]))members.push({...m,index:i,points:m.points.map(map),segments:m.segments.map(s=>({...s,points:s.points.map(map)}))});
    if(c.PointedVersion>=2){const p=ps[i];for(const side of [-1,1]){for(const x of p.footXs)feet.push(map({x:side===-1?x:-x,z:0}));foundations.push({point:map({x:side*c.W/2,z:0}),width:2*p.offset+c.PointLowerD+100,depth:Math.max(c.PointLowerD,c.PointWebD)+100,height:160,angle:Math.atan2(f.across.y,f.across.x),index:i});}}else feet.push(add(f.origin,mul(f.across,-c.W/2)),add(f.origin,mul(f.across,c.W/2)));continue;
   }
   addMember(range(64).map(u=>point(i,u)),'arch',i);
   const f=frames[i];let fl=add(f.origin,mul(f.across,-c.W/2)),fr=add(f.origin,mul(f.across,c.W/2));feet.push(fl,fr);
   if(c.Foot){addMember([fl,point(i,0)],'arch',i);addMember([point(i,1),fr],'arch',i);}
   if(structure===2){addMember(range(64).map(u=>point(i,u,true)),'arch',i);for(let j=1;j<c.Panels;j++)addMember([point(i,j/c.Panels),point(i,j/c.Panels,true)],'web',i);for(let j=1;j<c.Panels-1;j++)addMember([point(i,j/c.Panels,j%2!==0),point(i,(j+1)/c.Panels,j%2===0)],'web',i);}
  }
  if(isCross){for(let i=0;i<frames.length;i++)for(let j=i+1;j<frames.length;j++){
   const slope=(i%2?-1:1)-(j%2?-1:1);if(slope<=0)continue;
   const z=(stations[j]-stations[i])/(slope*Math.tan(c.CrossAngle*Math.PI/180));if(z<=(c.Foot?c.LegH:0)+.001||z>=c.H-.001)continue;
   let a=0,b=.5;for(let k=0;k<60;k++){let u=(a+b)/2;if(point(i,u).z<z)a=u;else b=u;}
   for(const u of [(a+b)/2,1-(a+b)/2]){cuts.push(u);crossNodes.push(point(i,u));}
  }need(crossNodes.length>0,'拱身未真实相交。请增大 CrossAngle、减小榀距，或增加拱高。');}
  const row=u=>{const r=frames.map((_,i)=>point(i,u));return isCross?r.sort((a,b)=>a.y-b.y):r;};
  for(let j=0;j<railCount;j++)addMember(row(j/(railCount-1)),'purlin');
  const g={c,frames,heights,ps,point,row,members,feet,foundations,stations,railCount,crossNodes,cuts:unique(cuts),patches:[],supports:[],roofError:'',area:0,planArea:0,autoLift:0};
  try{roof(g);}catch(e){g.roofError=e.message;g.patches=[];g.supports=[];g.area=0;g.planArea=0;g.autoLift=0;}
  g.memberLength=members.reduce((total,m)=>total+m.points.slice(1).reduce((s,p,i)=>s+dist(p,m.points[i]),0),0)/1000;
  return g;
 }
 function roof(g){
  const {c,frames,ps,point,row}=g;
  need(c.RoofType>=0&&c.RoofType<=2,'未知屋面类型。');
  for(const k of ['SideEave','EndEave','RoofOffset'])need(c[k]>=0&&c[k]<=3000,'出檐与屋面抬高须在0—3000mm内。');

  const lift=(p,h)=>add(p,v(0,0,h));
  const supports=(p,h)=>{if(h>.001)g.supports.push({points:[p,lift(p,h)],role:'support',index:-1});};
  const toGrid=columns=>frames.map((_,i)=>columns.map(col=>col[i]));
  function make(grid,name,left,right){
   function ends(a){if(!c.EndEave)return a;return [a[0].map(p=>sub(p,mul(frames[0].forward,c.EndEave))),...a,a.at(-1).map(p=>add(p,mul(frames.at(-1).forward,c.EndEave)))];}
   patch(ends(grid),name,g);
   if(left&&c.SideEave)patch(ends(grid.map((r,i)=>[sub(r[0],mul(frames[i].across,c.SideEave)),r[0]])),name+'出檐',g);
   if(right&&c.SideEave)patch(ends(grid.map((r,i)=>[r.at(-1),add(r.at(-1),mul(frames[i].across,c.SideEave))])),name+'出檐',g);
  }
  if(c.RoofType===0){
   for(const [a,b] of [[0,.5],[.5,1]]){const us=unique([...range(24,a,b),...g.cuts.filter(u=>u>a&&u<b)]);make(toGrid(us.map(u=>row(u).map(p=>lift(p,c.RoofOffset)))),'随形',a===0,b===1);}
   frames.forEach((_,i)=>range(g.railCount-1).forEach(u=>supports(point(i,u),c.RoofOffset)));
  }else if(c.RoofType===1){
   // Tangent planes at symmetric arc-length positions; visual roof only.
   const grids=[[],[]];
   frames.forEach((f,i)=>{
    const p=ps[i],u=.22,a=p.at(u),b=p.at(u+1e-5),slope=(b.z-a.z)/(b.x-a.x);
    const co=Math.floor(c.type/2)===1?Math.cos(c.CrossAngle*Math.PI/180):1;
    const ridge=(a.z-slope*a.x)*co+c.RoofOffset;
    const edge=(a.z+slope*(-p.half-a.x))*co+c.RoofOffset;
    const map=(x,z)=>add(f.origin,add(mul(f.across,x),v(0,0,z)));
    grids[0].push([map(-p.half,edge),map(0,ridge)]);
    grids[1].push([map(0,ridge),map(p.half,edge)]);
   });
   make(grids[0],'切线双坡左',true,false);make(grids[1],'切线双坡右',false,true);
  }else{
   need(c.RidgeWidth>0&&ps.every(p=>c.RidgeWidth<1.6*p.half),'脊带宽度须小于每榀上部跨度的80%。');need(c.RidgeRise>0&&c.RidgeRise<=3000,'中央抬高须为0—3000mm的正数。');
   const uAtX=(p,x)=>{let a=0,b=1;for(let i=0;i<60;i++){let u=(a+b)/2;if(p.at(u).x<x)a=u;else b=u;}return (a+b)/2;};
   for(let zone=0;zone<4;zone++){
    let grid=ps.map((p,i)=>{let cuts=[-p.half,-c.RidgeWidth/2,0,c.RidgeWidth/2,p.half];return range(20,cuts[zone],cuts[zone+1]).map(x=>lift(point(i,uAtX(p,x)),c.RoofOffset+((zone===1||zone===2)?c.RidgeRise:0)));});
    make(grid,zone===1||zone===2?'脊盖':'侧屋面',zone===0,zone===3);
   }
   ps.forEach((p,i)=>{[-c.RidgeWidth/2,c.RidgeWidth/2].forEach(x=>supports(point(i,uAtX(p,x)),c.RoofOffset+c.RidgeRise));range(g.railCount-1).forEach(u=>{if(Math.abs(p.at(u).x)>c.RidgeWidth/2+.001)supports(point(i,u),c.RoofOffset);});});
  }
 }
 function patch(grid,name,g){
  const faces=[];for(let i=0;i<grid.length-1;i++)for(let j=0;j<grid[0].length-1;j++)for(const ps of [[grid[i][j],grid[i][j+1],grid[i+1][j+1]],[grid[i][j],grid[i+1][j+1],grid[i+1][j]]]){
   const n=cross(sub(ps[1],ps[0]),sub(ps[2],ps[0]));if(len(n)<.000001)continue;need(n.z>.000001,'屋面局部翻折。请减小出檐或曲轴转角。');faces.push({points:ps,normal:n});g.area+=len(n)*.5e-6;g.planArea+=n.z*.5e-6;
  }
  g.patches.push({name,faces,borders:[grid[0],grid.at(-1),grid.map(r=>r[0]),grid.map(r=>r.at(-1))]});
 }
 function preset(c,type){const n={...c,type};if(type%2!==c.type%2){const leg=c.Foot?c.LegH:0,half=c.W/2-(c.Foot===2?leg*Math.tan(c.LegAngle*Math.PI/180):0);n.H=Math.round((leg+half*(type%2?1.3:.8))/10)*10;}return n;}
 return {compoundProfile,pointedMembers,wingLinks,defaults,names,roofNames,build,normalize,preset,profile,frameAt,v,add,sub,mul,dot,cross,len,dist,range};
})();
if(typeof globalThis!=='undefined')globalThis.ArchCore=ArchCore;
if(typeof module!=='undefined')module.exports=ArchCore;
