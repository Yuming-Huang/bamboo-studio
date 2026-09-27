export function roofStatus(result,verified=false,side='current'){
 const solved=!!result?.solved,pass=solved&&typeof result.pass==='boolean'?result.pass:null;
 const warnings=result?.warnings||[];
 const status=!solved?'待计算':pass===false?'未通过初筛':warnings.length?'计算存在提示，需核查':pass===true?'通过已启用初筛项':'初筛状态未明确';
 return {roofDisplayOnly:true,selectedSide:side,selectedLabel:side==='before'?'修改前方案':'当前方案',pass,verified,screeningStatus:status,warnings:[...warnings],note:'屋面仅作形式展示；进入本步不改变结构初筛结论。'};
}
