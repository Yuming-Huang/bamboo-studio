export const ghKeys=('type W H Foot LegH LegAngle CrossAngle TrussDepth Panels PointedVersion PointE PointSlope PointFootGap PointPanels PointLowerD PointLowerT PointWebD PointWebT SiteL SiteW SiteH SiteEnd L S End PurlinCount ArchD ArchT PurlinD PurlinT E Nu G Density Fc Ft Fb RoofG RoofQ W0 MuZ Beta Cp Cs SnowQ WindModel WindDirection WindEnds WindReverse WindAngle WindRoofWindward WindRoofLeeward WindRoofParallel WindEndWindward WindEndLeeward WindEndParallel Support Joints Buckle Mesh Limit Pmax').split(' ');
export function ghExchange(before,after,core){
 if(before.type!==after.type)throw Error('修改前后必须属于同一种构型。');
 const block=(name,raw)=>{const p=core.normalize(raw),g=core.build(p);if(p.Axis!==0||p.HeightMode!==0)throw Error('配套 GH 仅支持当前六步的直线等高阵列。');const data={...p,PurlinCount:g.railCount};const lines=ghKeys.map(k=>{const v=typeof data[k]==='boolean'?Number(data[k]):data[k];if(!Number.isFinite(v))throw Error('GH 参数不完整：'+k);return k+'='+v;});return '['+name+']\n'+lines.join('\n')+'\nStations='+g.frames.map(f=>f.origin.y).join(',');};
 return 'BAMBOO_GH_EDIT_V1\n'+block('Before',before)+'\n'+block('After',after)+'\n';
}
