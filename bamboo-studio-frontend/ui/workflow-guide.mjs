// The next action follows the user's decision state, not the currently inspected model.
export function workflowGuide({stage,error,busy,fresh,selected,recommended,improvement,editor,editorCurrent,editorUsable,changes=0,hasBaseline=true,verified=true,roofCandidate=null,selectedSide='current'}) {
 if(error)return {title:'先修正参数',note:error,action:'invalid',label:'修正后继续',disabled:true};
 if(busy)return {title:'正在计算',note:'计算完成后可查看结果；需要时可取消。',action:'wait',label:'正在计算…',disabled:true};
 if(stage<5){const notes=['得到场地边界，并自动适配初始单拱。','得到共同单拱，下一步沿纵向复制。','站位已确定，下一步补齐檩条。','拱架与檩条草案就绪，下一步比较候选构型。'];return {title:'本步成果',note:notes[stage-1],action:'next',label:['确认场地，设置单榀模数 →','确认模数，进入线性阵列 →','确认阵列，进入檩条布置 →','进入结构选型与多参数搜索 →'][stage-1]};}
 if(stage===6)return selected?{title:'准备导出',note:'导出当前所选方案。屋面形式只改变外观。',action:'export',label:'导出 Rhino 模型 .3dm'}:{title:'尚无可导出的方案',note:'返回第五步选择方案，或完成编辑后的计算。',action:'selection',label:'返回第五步 →'};
 if(editor){
  if(roofCandidate)return {title:'已选'+(selectedSide==='before'?'修改前方案':'当前方案'),note:roofCandidate.pass?'可以直接生成屋面；也可继续修改并计算。':'未通过初筛也可生成屋面，原计算结论会保留。',action:'next',label:'按所选方案生成屋面 →'};
  if(!verified)return {title:'所选方案待复核',note:'按本候选实际参数单独计算，作为编辑基准。可选择已有计算结果生成屋面，或先复核。',action:'solve-edit',label:changes?'复核基准与当前修改':'复核所选方案'};
  if(!hasBaseline)return {title:'恢复了上次编辑',note:'先分别重算修改前基准和当前方案，再显示云图。',action:'solve-edit',label:'重算基准与当前方案'};
  if(!editorCurrent)return {title:`${changes} 项修改待计算`,note:'几何已更新。点击下方计算后，才显示修改后的应力。',action:'solve-edit',label:'计算当前修改'};
  if(!editorCurrent.solved)return {title:'本次未生成数值',note:'查看原因后调整参数；修改前的结果仍保留。',action:'solve-edit',label:'重新计算当前方案'};
  if(!editorUsable)return {title:'已计算，未通过初筛',note:'可对照云图继续调整。未通过初筛也可选择生成屋面。',action:'solve-edit',label:'重新计算当前方案'};
  return {title:changes?'修改已计算':'已建立修改前基准',note:changes?'前后云图共用色标。可继续调整，或采用当前修改。':'修改下方参数后，可计算并对照应力变化。',action:changes?'next':'edit-ready',label:changes?'采用当前修改，生成屋面 →':'计算当前方案'};
 }
 if(!fresh)return {title:'先计算，再选型',note:'确认荷载与材料后计算；榀距保持不变。',action:'solve',label:'计算候选构型'};
 if(selected)return {title:'已选方案',note:'先复核所选构型；也可修改参数并比较应力。查看其他候选不会改变选择。',action:'edit',label:'编辑所选方案 →'};
 if(recommended)return {title:'计算完成，等待你选择',note:'系统推荐尚未自动采用。也可在结果中选择其他可行构型。',action:'adopt',label:'采用系统推荐'};
 if(improvement)return {title:'已有相对优选，可继续改进',note:'按有效结果比较。初筛状态仍单独显示，选中后可编辑并对照云图。',action:'improve',label:'以相对优选开始调整 →'};
 return {title:'本次没有系统推荐',note:'查看各型原因；仍可选择本型可行方案，或调整条件重算。',action:'settings',label:'调整计算条件 →'};
}
