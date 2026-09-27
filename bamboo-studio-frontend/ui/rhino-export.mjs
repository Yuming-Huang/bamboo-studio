import {parameterExport} from './export.mjs';

// Export analytic design members, never the displaced or shifted comparison view.
export function rhinoExport(context,core){
 const selected=parameterExport(context);
 return rhinoGeometry(selected,core);
}
export function rhinoGeometry(selected,core){
 const {selectedCase,parameters}=selected;
 if(!['C','B'].includes(selectedCase))throw Error('请选择 C 或 B。');
 const g=core.build(parameters);
 if(g.roofError)throw Error('屋面无法生成，请先修正：'+g.roofError);
 const pt=p=>[p.x,p.y,p.z];
 const members=g.members.map((m,i)=>{
  const p=m.points;
  // The geometry core samples each analytic arch at 65 points, with the crown at 32.
  const segments=m.segments?m.segments.map(s=>({...s,points:s.points.map(pt)})):m.role==='arch'&&p.length===65
   ?[0,32].map(k=>({kind:'arc',points:[pt(p[k]),pt(p[k+16]),pt(p[k+32])]}))
   :p.slice(1).map((b,j)=>({kind:'line',points:[pt(p[j]),pt(b)]}));
  return {id:`${m.role}-${i+1}`,role:m.role,frame:m.index,diameter:m.diameterMm??(m.role==='purlin'?parameters.PurlinD:parameters.ArchD),wall:m.wallMm??(m.role==='purlin'?parameters.PurlinT:parameters.ArchT),segments};
 });
 return {schema:'bamboo-rhino-export/v1',selectedCase,createdAt:selected.createdAt,parameters,
  analysis:{roofDisplayOnly:true,roofScope:"第六步屋面只作外观；分析按第五步预设荷载面。",pass:selected.analysis?.pass??selected.analysis?.result?.pass??null,screeningStatus:selected.analysis?.screeningStatus||null,verified:!!selected.analysis?.verified,selectedSide:selected.analysis?.selectedSide||null,warnings:selected.analysis?.warnings||[],executed:!!selected.analysis?.executed,solved:!!selected.analysis?.solved,engine:selected.analysis?.engine||null,createdAt:selected.analysis?.createdAt||null},
  members,roofs:g.patches.map((patch,i)=>({name:`${patch.name}-${i+1}`,triangles:patch.faces.map(f=>f.points.map(pt))})),
  foundations:g.foundations.map(f=>({...f,point:pt(f.point)})),
  supports:g.supports.map(s=>s.points.map(pt)),
  summary:{frames:g.frames.length,rails:g.railCount,areaM2:g.area,planAreaM2:g.planArea},
  scope:'几何模型，单位毫米。中心线与竹竿外形可编辑；屋面为网格。外形不包含竹壁、竹节或连接节点。分脚底座仅为几何示意，各实际落点独立；底座刚度和锚固未建模。修改几何不联动原软件或 Grasshopper 参数；屋面次支承线仅为待设计示意。'};
}
export const rhinoFileName=caseId=>`竹拱_${caseId}_${caseId==='B'?'搜索推荐':'当前设计'}.3dm`;
