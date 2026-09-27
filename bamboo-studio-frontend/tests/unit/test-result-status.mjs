import assert from 'node:assert/strict';
import fs from 'node:fs';
import {resultStatus} from '../../ui/result-status.mjs';
const sample={solved:true,pass:false,massKg:465.1,displacementMm:288.8,limitMm:29.2,stressMPa:73.07,stressRatio:null,bucklingFactor:null,warnings:[],parameters:{Buckle:false}};
let checks=0;function need(test){assert.ok(test);checks++;}
let view=resultStatus({row:{feasible:false,reference:sample}});
need(view.hasNumbers&&!view.ok);need(view.title==='已计算 · 未通过初筛');need(view.label.includes('超限参考'));
need(view.notes[0].includes('288.80')&&view.notes[0].includes('29.20'));
need(!view.notes.join('').includes('正应力组合比'));
view=resultStatus({row:{feasible:false,diagnostic:{attempted:false,state:'超出场地横向边界'}}});
need(view.title==='未计算 · 条件不满足'&&!view.hasNumbers);need(view.notes[0]==='超出场地横向边界');
view=resultStatus({row:{feasible:false,diagnostic:{attempted:true,state:'求解未完成：刚度矩阵奇异'}}});
need(view.title==='求解未完成');
view=resultStatus({row:{feasible:false,reference:{...sample,pass:true,displacementMm:10,warnings:['屈曲未完成']}}});
need(view.title==='已计算 · 诊断未通过'&&view.notes.includes('屈曲未完成'));
view=resultStatus({row:{feasible:true,result:{...sample,pass:true,displacementMm:10}}});
need(view.ok&&view.title.includes('本型最优')&&view.label==='');
view=resultStatus({row:{feasible:true,result:{...sample,pass:true}},complete:false});
need(!view.ok&&view.title.includes('搜索未完成'));
view=resultStatus({row:{status:'当前范围无可行解'},baseline:{eligible:true,result:{...sample,pass:true,displacementMm:10}},baselineView:true});
need(view.ok&&view.title==='同模数通过已启用初筛');
view=resultStatus({baseline:{eligible:false,result:{...sample,pass:true,displacementMm:10}},baselineView:true});
need(view.ok&&view.notes.some(n=>n.includes('不参与全局推荐')));
view=resultStatus({row:{feasible:false,reference:{...sample,stressRatio:1.25,bucklingFactor:.8,parameters:{Buckle:true}}}});
need(view.notes.some(n=>n.includes('正应力组合比 1.25'))&&view.notes.some(n=>n.includes('屈曲因子 0.80')));
if(process.argv.length>=4){
 const old=JSON.parse(fs.readFileSync(process.argv[2],'utf8')),now=JSON.parse(fs.readFileSync(process.argv[3],'utf8'));
 assert.equal(now.B.type,old.B.type);checks++;
 for(let i=0;i<old.types.length;i++){
  assert.equal(now.types[i].feasible,old.types[i].feasible);checks++;
  for(const k of ['result','reference'])for(const f of ['displacementMm','stressMPa','massKg','limitMm','stressRatio','bucklingFactor']){
   assert.equal(now.types[i][k]?.[f],old.types[i][k]?.[f]);checks++;
  }
 }
 const a2=now.types.find(t=>t.type===5);need(a2.diagnostic.solvedCount===0&&a2.diagnostic.state.includes('超出场地横向边界'));
 need(a2.reason.includes('超出场地横向边界')&&a2.reason.includes('投影面积差'));
 need(resultStatus({row:a2}).notes[0].includes('4000'));
}
console.log(JSON.stringify({pass:true,checks}));
