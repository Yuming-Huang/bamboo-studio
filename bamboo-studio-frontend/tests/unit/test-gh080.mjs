import assert from 'node:assert/strict';
import fs from 'node:fs';
import core from '../../ui/core.js';
import {searchDefaults,parseCandidates,searchPlan,heightFactors,migrateSearchParameters} from '../../ui/search-space.mjs';
import {fitModule,validateModule} from '../../ui/module-fit.mjs';
import {SchemeEditor} from '../../ui/scheme-editor.mjs';
import {workflowGuide} from '../../ui/workflow-guide.mjs';
import {ghExchange} from '../../ui/gh-exchange.mjs';
import {parameterKey} from '../../ui/client.mjs';
const checks=[];async function check(name,fn){await fn();checks.push(name);}
const p={...structuredClone(core.defaults),...fitModule(core.defaults),Run:true};
await check('GH defaults produce 135 candidates and 45 with height locked',()=>{
 assert.deepEqual(searchPlan(p).byType.map(t=>t.count),[9,9,27,81,9]);assert.equal(searchPlan(p).total,135);assert.equal(searchPlan({...p,HeightSearch:false}).total,45);
});
await check('Candidate budget is evaluated on the complete Cartesian product',()=>{assert.equal(searchPlan({...p,MaxCandidates:134}).overBudget,true);assert.equal(searchPlan({...p,MaxCandidates:135}).overBudget,false);assert.equal(searchPlan({...p,PointEs:[1200,900],PointSlopes:[.9,.8],PointFootGaps:[10,20],PointPanelCounts:[6,8]}).byType[4].count,144);});
await check('Lists preserve first value and reject invalid syntax, ranges, and discrete counts',()=>{
 assert.deepEqual(parseCandidates('25，15 35;25\n20','CrossAngles'),[25,15,35,20]);
 for(const raw of ['', '15x', 'Infinity', '46', '0x10'])assert.throws(()=>parseCandidates(raw,'CrossAngles'));
 assert.throws(()=>parseCandidates('7','PanelCounts'));assert.throws(()=>parseCandidates('6.5','PointPanelCounts'));
 assert.throws(()=>parseCandidates('100 110 120 130 140 150 160','Diameters'));
 assert.throws(()=>searchPlan({...p,ArchT:50}));
});
await check('Height factors include initial height and deduplicate matching limits',()=>{
 assert.deepEqual(heightFactors({...p,Hmin:1,Hmax:1}),[1]);assert.deepEqual(heightFactors({...p,Samples:2}),[.9,1,1.1]);
 assert.deepEqual(heightFactors({...p,HeightSearch:false,Hmin:1.4,Hmax:.5}),[1]);assert.throws(()=>heightFactors({...p,Hmin:1.4,Hmax:.5}));
});
await check('Module fit matches GH and is independent of pointed overhang candidates',()=>{
 assert.equal(p.W,5600);assert.equal(p.H,4040);assert.deepEqual(fitModule({...p,PointE:600,PointEs:[600,800]}),fitModule(p));
 const f=fitModule({...p,SideReserve:600});assert.equal(f.W,6800);assert.equal(f.H,4520);
 assert.throws(()=>validateModule({...p,W:6000}));assert.throws(()=>fitModule({...p,SideReserve:4000}));validateModule(p);
});
await check('Legacy parameters retain deliberate type geometry as the first and fixed candidate',()=>{
 const q=core.normalize(migrateSearchParameters({W:6000,H:4200,ArchD:120,CrossAngle:32,TrussDepth:550,Panels:12,PointE:900}));
 assert.deepEqual(q.CrossAngles,[32]);assert.deepEqual(q.TrussDepths,[550]);assert.deepEqual(q.Diameters,[120,150,300]);assert.deepEqual(q.PointEs,[900]);
 const fresh=core.normalize(p);fresh.CrossAngles.push(40);assert.deepEqual(core.defaults.CrossAngles,searchDefaults.CrossAngles);
 assert.notEqual(parameterKey(p),parameterKey({...p,CrossAngles:[15,25,35]}));
});
const result=params=>({type:params.type,parameters:structuredClone(params),solved:true,pass:params.Limit!==.01,warnings:[],stressMPa:params.CrossAngle===20?7:12,displacementMm:8,massKg:500,members:[],limitMm:params.Limit||params.W/250});
let last,posts=[];
const fetcher=async(path,opts)=>{
 if(path==='/api/jobs'){last=JSON.parse(opts.body);posts.push(last);return {ok:true,json:async()=>({id:'synthetic'})};}
 return {ok:true,json:async()=>({state:'complete',message:'TEST ONLY',result:{schema:'bamboo-local-result/v1',engineId:'karamba3d',parameters:last.parameters,C:result(last.parameters),mode:'single'}})};
};
const ep={...p,type:2,CrossAngle:20,ArchD:150,PMode:1,PurlinCount:9},e=new SchemeEditor({fetcher,token:'test',delay:async()=>{}});
await check('Adoption uses exact candidate angle and requires isolated verification',async()=>{
 e.start(result(ep),{parameters:p},parameterKey(p),'manual');assert.equal(e.parameters.CrossAngle,20);assert.equal(e.changes.length,0);assert.equal(e.usable,null);
 await e.calculate();assert.equal(posts.length,1);assert.equal(posts[0].mode,'single');assert.equal(e.verified,true);assert.equal(e.usable.pass,true);
});
await check('Edits invalidate after-cloud, retain before-cloud and compare shared scale',async()=>{
 e.edit('CrossAngle',30);assert.equal(e.current,null);assert.equal(e.before.stressMPa,7);await e.calculate();
 assert.equal(posts.length,2);assert.equal(e.current.stressMPa,12);assert.equal(e.sharedMax,12);assert.equal(e.before.stressMPa,7);
 e.edit('Limit',.01);await e.calculate();assert.equal(e.current.solved,true);assert.equal(e.usable,null);
 e.reset();assert.equal(e.current.stressMPa,7);assert.equal(e.changes.length,0);
});
await check('Restored editor draft must rebuild verification, no saved stale analysis',async()=>{
 e.edit('CrossAngle',25);const r=new SchemeEditor({fetcher,token:'test',delay:async()=>{}});r.restore(e.snapshot());assert.equal(r.verified,false);assert.equal(r.current,null);
 const n=posts.length;await r.calculate();assert.equal(posts.length,n+2);assert.equal(r.verified,true);
 assert.equal(workflowGuide({stage:5,editor:true,verified:false}).label,'复核所选方案');
});
await check('GH exchange carries actual chosen type parameters, not list defaults',()=>{
 for(const [type,extra]of [[2,{CrossAngle:20}],[4,{TrussDepth:600,Panels:10}],[5,{PointE:1000,PointSlope:.8,PointFootGap:20,PointPanels:8}]]){
  const chosen={...p,type,...extra,PMode:1,PurlinCount:9},text=ghExchange(chosen,chosen,core);
  for(const [k,v]of Object.entries(extra))assert.ok(text.includes('\n'+k+'='+v+'\n'));
 }
});
fs.writeFileSync('test-gh080-results.json',JSON.stringify({pass:true,checks,numericalResults:'synthetic fixtures only; no live Karamba verification'},null,2));
console.log(JSON.stringify({pass:true,checks:checks.length}));
