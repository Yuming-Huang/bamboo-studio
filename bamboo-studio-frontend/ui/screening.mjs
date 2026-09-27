// Presentation only: never changes solver values, pass flags, or ranking.
const finite=Number.isFinite;
const n=v=>finite(v)?v.toLocaleString('zh-CN',{maximumFractionDigits:3}):'—';
const esc=v=>String(v??'').replace(/[&<>"']/g,x=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[x]));
export const screeningScope='“通过初筛”只表示已启用的检查项未超限。未评估不等于通过，也不等于完整规范验算。Karamba3D 提供计算结果，本软件按下列规则判定。';
export function screeningChecks(parameters={},result=null){
 const p=result?result.parameters||{}:parameters;
 const automatic=p.Limit===0&&finite(p.W)&&p.W>0;
 const expected=finite(p.Limit)&&p.Limit>0?p.Limit:automatic?p.W/250:null;
 const limit=result?(finite(result.limitMm)&&result.limitMm>0?result.limitMm:null):expected;
 const source=automatic?`默认 W / 250：${n(p.W)} / 250 = ${n(expected)} mm；这是本软件的演示初筛值，不是原竹通用规范限值。`:expected?`用户填写 Limit = ${n(expected)} mm。应依据项目采用的标准与使用要求确定。`:'位移阈值来源缺失，无法核对。';
 const state=(v,bound,reverse=false)=>!result?'待计算':!result.solved?'无有效结果':!finite(v)||!finite(bound)?'未取得指标':(reverse?v>=bound:v<=bound)?'通过':'超限';
 const strength=[p.Fc,p.Ft,p.Fb].every(v=>finite(v)&&v>0),off=[p.Fc,p.Ft,p.Fb].every(v=>v===0);
 return [
  {id:'displacement',name:'最大位移',bound:`≤ ${n(limit)} mm`,value:result?.solved?`${n(result.displacementMm)} mm`:'—',status:state(result?.displacementMm,limit),detail:source+(result&&finite(limit)&&finite(expected)&&Math.abs(limit-expected)>1e-6?' 当前结果记录的限值与参数推导值不同；本表保留计算时记录的限值，请核查结果来源。':'')},
  {id:'strength',name:'正应力组合比',bound:strength?'≤ 1.00':off?'未启用':'参数不完整',value:strength&&result?.solved?n(result.stressRatio):'—',status:strength?state(result?.stressRatio,1):off?'未评估':'需补全参数',detail:strength?`Fc 抗压 ${n(p.Fc)}、Ft 抗拉 ${n(p.Ft)}、Fb 抗弯 ${n(p.Fb)} MPa。逐杆、逐工况取最大值：|轴向应力| / 相应抗压或抗拉阈值 + 弯曲应力 / 抗弯阈值 ≤ 1。不是把云图最大 MPa 值直接与 1 比较。`:off?'Fc / Ft / Fb 均为 0，未做强度初筛。可显示应力数值，但不能据此称为强度合格。':'Fc / Ft / Fb 必须全部为正值，或全部为 0；不得只填一部分。'},
  {id:'buckling',name:'线性屈曲因子',bound:p.Buckle?'≥ 1.00':'未启用',value:p.Buckle&&result?.solved?n(result.bucklingFactor):'—',status:p.Buckle?state(result?.bucklingFactor,1,true):'未评估',detail:p.Buckle?'取所计算工况中的最小有效正屈曲因子，1.00 是本软件固定的诊断界限。它表示理想线性屈曲临界荷载相对当前工况的倍率；不包含真实缺陷、节点与完整二阶稳定验算。':'屈曲诊断关闭，稳定性未评估。'}
 ];
}
export function screeningText(p,r=null){
 return ['本次初筛依据',...screeningChecks(p,r).map(x=>`${x.name}：${x.bound}；计算值 ${x.value}；${x.status}。${x.detail}`),screeningScope].join('\n');
}
export function screeningHtml(p,r=null,title='本次初筛依据',open=false){
 const rows=screeningChecks(p,r);
 return `<details class="screening-guide" ${open?'open':''}><summary>${esc(title)}</summary><div class="screening-rows">${rows.map(x=>`<div class="screening-row" data-check="${x.id}"><div><strong>${x.name}</strong><span>${esc(x.bound)}</span></div>${r?`<div><span>计算值 ${esc(x.value)}</span><b class="screening-${x.status==='通过'?'pass':x.status==='超限'?'fail':'neutral'}">${x.status}</b></div>`:x.status==='未评估'||x.status==='需补全参数'?`<small>${x.status}</small>`:''}</div>`).join('')}</div><details class="screening-explanation"><summary>限值从哪里来，怎样判定？</summary>${rows.map(x=>`<p><strong>${x.name}：</strong>${esc(x.detail)}</p>`).join('')}<p>最大阈值比 = max（位移 / 位移限值，已启用的正应力组合比，已启用的 1 / 屈曲因子）。1 是界限；在有效结果中，超过 1 表示至少一项超限。</p><p>${screeningScope}</p><p>几何、求解有效性与诊断警告另行检查；本表单项通过不代表方案已可采用。</p></details></details>`;
}
export function comparisonChecksHtml(before,after){
 const a=screeningChecks({},before),b=after?screeningChecks({},after):null;
 return `<details class="screening-guide"><summary>修改前后 · 逐项阈值对比</summary>${a.map((x,i)=>`<div class="screening-row"><strong>${x.name}</strong><p>前：${x.value} / ${x.bound} · ${x.status}</p><p>后：${b?`${b[i].value} / ${b[i].bound} · ${b[i].status}`:'待重新计算'}</p></div>`).join('')}<p class="hint">各自采用计算时记录的阈值；修改阈值、荷载或材料后，通过状态变化不能单独证明构型改善。</p></details>`;
}
