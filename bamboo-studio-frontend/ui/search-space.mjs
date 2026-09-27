// Mirrors GH v0.8.0 CandidateSpace and ComparisonRules. Planning only; no FE solver.
export const searchDefaults={SideReserve:1200,HeightSearch:true,MaxCandidates:300,Diameters:[100,150,300],CrossAngles:[25,15,35],TrussDepths:[450,300,600],PanelCounts:[8,6,10],PointEs:[1200],PointSlopes:[.9],PointFootGaps:[10],PointPanelCounts:[6]};
export const listSpecs={
 Diameters:{label:'主杆候选外径',unit:'mm',min:40,max:400,limit:6},
 CrossAngles:{label:'交叉倾角',unit:'°',min:5,max:45,scalar:'CrossAngle'},
 TrussDepths:{label:'上下弦高差',unit:'mm',min:100,max:1500,scalar:'TrussDepth'},
 PanelCounts:{label:'腹杆分格',unit:'格',min:4,max:24,even:true,scalar:'Panels'},
 PointEs:{label:'每侧结构外挑',unit:'mm',min:50,max:2000,scalar:'PointE'},
 PointSlopes:{label:'上弦坡度系数',unit:'',min:.35,max:.95,scalar:'PointSlope'},
 PointFootGaps:{label:'拱脚杆件净间距',unit:'mm',min:0,max:500,scalar:'PointFootGap'},
 PointPanelCounts:{label:'每侧腹杆分格',unit:'格',min:6,max:12,integer:true,scalar:'PointPanels'}
};
export function parseCandidates(raw,key){
 const s=listSpecs[key];if(!s)throw Error('未知候选参数');
 const tokens=Array.isArray(raw)?raw:String(raw).trim().split(/[\s,，;；]+/).filter(Boolean);
 if(!tokens.length)throw Error('至少填写一个候选值。');
 const values=[];
 for(const token of tokens){
  if(typeof token!=='number'&&!/^[+-]?(?:\d+\.?\d*|\.\d+)(?:e[+-]?\d+)?$/i.test(token))throw Error('请用空格、逗号或换行分隔数值。');
  const v=Number(token);
  if(!Number.isFinite(v)||v<s.min||v>s.max||(s.integer&&!Number.isInteger(v))||(s.even&&v%2!==0))throw Error(`须为 ${s.min}—${s.max} ${s.unit}${s.even?'，且为偶数':s.integer?'，且为整数':''}。`);
  if(!values.includes(v))values.push(v);
 }
 if(values.length>(s.limit||12))throw Error(`最多 ${s.limit||12} 个不同候选值。`);
 return values;
}
export function heightFactors(p){
 if(!p.HeightSearch)return [1];
 const {Hmin:lo,Hmax:hi,Samples:n}=p;
 if(!Number.isFinite(lo)||!Number.isFinite(hi)||lo<=0||hi<lo||!Number.isInteger(n)||n<2||n>9)throw Error('高度倍率须大于 0，最大值不小于最小值；采样数须为 2—9。');
 const a=Array.from({length:n},(_,i)=>lo+(hi-lo)*i/(n-1));
 if(lo<=1&&hi>=1)a.push(1);
 return a.sort((x,y)=>x-y).filter((x,i,a)=>!i||Math.abs(x-a[i-1])>1e-9);
}
export function searchPlan(p){
 const lists=Object.fromEntries(Object.keys(listSpecs).map(k=>[k,parseCandidates(p[k],k)]));
 if(lists.Diameters.some(d=>d<=2*p.ArchT))throw Error('每个候选外径必须大于两倍主杆壁厚。');
 if(!Number.isInteger(p.MaxCandidates)||p.MaxCandidates<1||p.MaxCandidates>2000)throw Error('组合预算须为 1—2000 的整数。');
 const factors=heightFactors(p),count=k=>lists[k].length;
 const variants=[1,1,count('CrossAngles'),count('TrussDepths')*count('PanelCounts'),count('PointEs')*count('PointSlopes')*count('PointFootGaps')*count('PointPanelCounts')];
 const byType=[0,1,2,4,5].map((type,i)=>({type,variants:variants[i],count:variants[i]*factors.length*count('Diameters')}));
 const total=byType.reduce((a,t)=>a+t.count,0);
 return {lists,factors,byType,total,maximum:p.MaxCandidates,overBudget:total>p.MaxCandidates};
}
export function migrateSearchParameters(raw){
 const p={...raw};
 for(const [key,s] of Object.entries(listSpecs)){
  if(p[key]!==undefined)continue;
  if(key==='Diameters'&&Number.isFinite(p.ArchD))p[key]=[...new Set([p.ArchD,p.SearchD2??150,p.SearchD3??300])];
  else p[key]=s.scalar&&Number.isFinite(p[s.scalar])?[p[s.scalar]]:[...searchDefaults[key]];
 }
 if(p.SideReserve===undefined)p.SideReserve=1200;
 if(p.HeightSearch===undefined)p.HeightSearch=true;
 if(p.MaxCandidates===undefined)p.MaxCandidates=300;
 return p;
}
export function selectedParametersText(p){
 const keys=p.type===2?['CrossAngles']:p.type===4?['TrussDepths','PanelCounts']:p.type===5?['PointEs','PointSlopes','PointFootGaps','PointPanelCounts']:[];
 return keys.map(k=>{const s=listSpecs[k];return `${s.label} ${p[s.scalar]}${s.unit}`;}).join(' · ');
}
