const number=(n,d=2)=>Number.isFinite(n)?n.toFixed(d):'—';

// A numerical solution, passing the checks, and eligibility for global ranking
// are separate states. This function must not change selection or solver data.
export function resultStatus({row,baseline,baselineView=false,complete=true}) {
 const x=baselineView?baseline?.result:row?.result||row?.reference;
 const d=baselineView?x:row?.diagnostic;
 const solved=x?.solved===true;
 const ok=baselineView?solved&&x.pass===true&&!(x.warnings?.length):complete&&row?.feasible===true;
 const warnings=x?.warnings||d?.warnings||[];
 const notes=[];
 let title,tone='neutral',label='';
 if(!complete&&!baselineView){title='搜索未完成 · 暂不推荐';label=solved?'已算候选 · 暂不排名':'';}
 else if(ok){title=baselineView?'同模数通过已启用初筛':'本型最优 · 通过已启用初筛';tone='pass';}
 else if(solved){
  title=warnings.length?'已计算 · 诊断未通过':'已计算 · 未通过初筛';
  label=warnings.length?'诊断参考结果 · 不可采用':'超限参考结果 · 可继续编辑';tone='warning';
 }else{
  title=d?.attempted?'求解未完成':'未计算 · 条件不满足';tone='warning';
 }
 if(solved){
  if(Number.isFinite(x.displacementMm)&&Number.isFinite(x.limitMm)&&x.limitMm>0&&x.displacementMm>x.limitMm)
   notes.push(`位移 ${number(x.displacementMm)} mm ＞ 限值 ${number(x.limitMm)} mm（${number(x.displacementMm/x.limitMm)} 倍）`);
  if(Number.isFinite(x.stressRatio)&&x.stressRatio>1)notes.push(`正应力组合比 ${number(x.stressRatio)} ＞ 限值 1.00`);
  if(x.parameters?.Buckle&&Number.isFinite(x.bucklingFactor)&&x.bucklingFactor<1)notes.push(`线性屈曲因子 ${number(x.bucklingFactor)} ＜ 限值 1.00`);
  notes.push(...warnings.slice(0,2));
  if(!ok&&!notes.length)notes.push(!complete?'搜索尚未完成，不能采用部分结果。':x.state||'未满足本次筛选条件。');
 }else{
  notes.push(d?.state||d?.error||(baselineView?baseline?.reason:row?.reason)||'尚未得到完整计算结果，请查看计算说明。');
 }
 if(solved&&(baselineView?baseline?.eligible===false:row?.diagnostic?.globallyComparable===false))
  notes.push('覆盖面积差超出比较范围，不参与全局推荐；本型能否采用仍按初筛判定。');
 return {x,ok,title,tone,label,notes,hasNumbers:solved};
}
