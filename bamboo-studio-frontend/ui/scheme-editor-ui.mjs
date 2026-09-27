import {screeningHtml,comparisonChecksHtml} from './screening.mjs';
import {roofStatus} from './roof-choice.mjs';
import {editorGroups} from './scheme-editor.mjs';
const esc=v=>String(v??'').replace(/[&<>"']/g,x=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[x]));
const fmt=v=>Number.isFinite(v)?v.toFixed(2):'—';
const value=v=>typeof v==='boolean'?(v?'开启':'关闭'):String(v);
export function editorControls(editor,names){
 const p=editor.parameters;
 const field=f=>{
  const id='edit-'+f.key,v=p[f.key];
  const input=f.toggle?`<input id="${id}" data-edit="${f.key}" type="checkbox" ${v?'checked':''}>`:f.options?`<select id="${id}" data-edit="${f.key}">${f.options.map((s,i)=>`<option value="${i}" ${i===v?'selected':''}>${s}</option>`).join('')}</select>`:`<div class="value-row"><input id="${id}" data-edit="${f.key}" type="number" min="${f.min}" max="${f.max}" step="${f.step}" value="${v}" aria-describedby="hint-${id}"><span>${f.unit}</span></div>`;
  return `<div class="field editor-field"><div class="parameter-row"><label class="field-label" for="${id}">${f.label}</label>${input}</div><small id="hint-${id}">${f.help||''}</small><small class="field-baseline"></small></div>`;
 };
 return `<div class="editor-heading"><div><small>${editor.record.origin==='recommended'?'来自系统推荐':editor.record.origin==='improvement'?'改进起点 · 初筛状态另列':'来自用户选型'}</small><h2>${names[p.type]}</h2></div><div class="editor-heading-links"><button id="editorBack" class="text-action">更换构型 ↗</button><button id="editorRecommend" title="返回选型时的推荐与原始条件；当前编辑草稿仍可恢复">切回系统推荐</button></div></div>
 <div id="roofChoice" class="roof-choice" aria-label="选择下一步采用的方案"></div>
 <div id="editorState" class="pending-preview-note" role="status"></div>
 <div class="editor-actions"><div class="editor-view-actions"><button id="editorCompare" aria-pressed="true">前后并排</button><button id="editorStress" aria-pressed="true">应力云图</button></div></div>
 <div class="editor-jumps">${editorGroups(p).slice(0,3).map((g,i)=>`<button data-editor-jump="${i}">${g.title}</button>`).join('')}</div>
 ${editorGroups(p).map((g,i)=>`<details class="control-fold editor-group" data-editor-group="${i}" ${g.open?'open':''}><summary>${g.title}</summary>${g.help?`<p class="hint">${g.help}</p>`:''}${g.fields.map(field).join('')}</details>`).join('')}
 <div id="editorDelta" class="delta-summary"></div>
 <details class="editor-changes"><summary>本次修改 <span id="editorChangeCount"></span></summary><div id="editorChanges"></div></details>
 <details class="editor-tools"><summary>撤销、恢复与导出</summary><div class="editor-view-actions"><button id="editorUndo">撤销修改</button><button id="editorReset">恢复修改前</button></div><div class="editor-view-actions"><button id="editorExport">导出对比记录</button></div><button id="editorGh" class="quiet wide">导出配套 GH 参数包</button></details>
 <p class="hint">场地边界、阵列长度与榀距沿用选型条件。修改后只校核本方案，不重新搜索其他构型；未通过也可继续编辑或选择生成屋面。</p><p class="hint">云图为各工况的逐梁正应力绝对值包络。两侧采用同一色标、视角与尺度。改变荷载、材料或支座时，对比反映全部条件变化。</p>`;
}
export function editorStatus(editor,error){
 if(error)return `<strong>参数需调整</strong><p>${esc(error)}</p>`;
 if(editor.busy)return `<strong>${editor.before?'正在计算当前修改':'正在重建修改前基准'}</strong><p>${esc(editor.client.message)}</p><progress aria-label="当前方案计算中"></progress>`;
 if(editor.client.error)return `<strong>求解未完成</strong><p>${esc(editor.client.error)}</p><p>修改前基准已保留，当前云图未生成。</p>`;
 if(!editor.verified)return `<strong>所选方案 · 待单独复核</strong><p>已带入本候选的完整参数。当前基准来自候选构型搜索，可直接选择并生成屋面；也可单独复核或修改后再计算。初筛结论会保留。</p>`;
 const r=editor.current;
 if(!r)return `<strong>修改后 · 待重新计算</strong><p>${editor.before?'左侧保留修改前云图；右侧仅显示新几何。':'上次编辑参数已恢复；计算时先重建基准，再计算当前修改。'}</p>`;
 const coverage=`${r.parameters?.Fc>0&&r.parameters?.Ft>0&&r.parameters?.Fb>0?'含强度初筛':'强度未评估'} · ${r.parameters?.Buckle?'含屈曲诊断':'稳定性未评估'}`;
 return `<strong>${r.solved?(editor.usable?'已计算 · 通过已启用初筛项':'已计算 · 未通过初筛'):'未生成数值结果'}</strong>${!r.solved?`<p>${esc(r.error||r.state||'')}</p>`:''}${r.solved?`<p>位移 ${fmt(r.displacementMm)} / 限值 ${fmt(r.limitMm)} mm</p><small>${coverage}</small>`:''}${screeningHtml(r.parameters,r,'本方案的计算值与限值')}${editor.before?comparisonChecksHtml(editor.before,r):''}${r.warnings?.length?`<p>${r.warnings.map(esc).join('；')}</p>`:''}`;
}
export function changesHtml(editor){return editor.changes.length?`<dl>${editor.changes.map(x=>`<div><dt>${esc(x.label)}</dt><dd>${esc(value(x.before))} → ${esc(value(x.after))} ${esc(x.unit)}</dd></div>`).join('')}</dl>`:'<p class="hint">尚未修改，当前与基准相同。</p>';}
export function roofChoiceButtons(editor){
 return ['before','current'].map(side=>{
  const r=side==='before'?editor.before:editor.current,selected=editor.selectedSide===side;
  return `<button data-roof-choice="${side}" class="scheme-choice-button ${selected?'is-selected':''}" aria-pressed="${selected}" ${editor.busy||!r?.solved?'disabled':''}>${selected?'已选：': '采用'}${side==='before'?'修改前方案':'当前方案'}</button>`;
 }).join('');
}
export function roofChoiceHtml(editor){const r=editor.roofCandidate,s=roofStatus(r,editor.verified,editor.selectedSide);return `<strong>下一步采用哪个方案？</strong><div class="roof-choice-buttons">${roofChoiceButtons(editor)}</div><p role="status">${r?`已选${s.selectedLabel} · ${s.screeningStatus}`:'当前修改待计算；也可采用修改前方案'}。未通过初筛也可生成屋面。</p>`;}
export function comparisonHtml(editor,names){return [[editor.before,'前','修改前 · 固定基准','before'],[editor.current,'后','当前方案 · '+editedStateLabel(editor),'current']].map(([r,id,title,side])=>{
 const s=roofStatus(r,editor.verified,side),selected=editor.selectedSide===side;
 return `<section class="compare-pane ${selected?'chosen-pane':''}"><header><span class="compare-letter">${id}</span><div><h3>${title}</h3><p>${names[editor.record.type]} · ${s.screeningStatus}</p><small>${r?.solved?'最大应力工况：'+esc(r.stressCase||'包络'):'几何预览，暂无有效应力'}</small><button data-roof-choice="${side}" class="scheme-choice-button ${selected?'is-selected':''}" aria-pressed="${selected}" ${editor.busy||!r?.solved?'disabled':''}>${selected?'已选：':'采用'}${side==='before'?'修改前方案':'当前方案'}</button></div></header><div class="compare-values">${[['最大位移',r?.displacementMm,'mm'],['最大正应力',r?.stressMPa,'MPa'],['杆件质量',r?.massKg,'kg']].map(([n,v,u])=>`<div><small>${n}</small><strong>${fmt(r?.solved?v:null)} <em>${u}</em></strong></div>`).join('')}</div></section>`;
 }).join('');}
export function editedStateLabel(editor){return editor.busy?'计算中':editor.client.error?'求解未完成':!editor.verified?'待单独复核':editor.current?.solved?(editor.usable?'满足初筛':'未通过初筛'):editor.current?'未生成数值':'待计算';}
