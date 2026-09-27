// Initial geometry only. The fifth stage performs structural optimization.
export function fitModule(c){
 for(const k of ['SiteW','SiteH','Foot','LegH','LegAngle','SideReserve'])if(!Number.isFinite(c[k]))throw Error('请填写完整的场地与拱脚尺寸。');
 if(c.SiteW<2000||c.SiteW>30000||c.SiteH<1500||c.SiteH>16000)throw Error('场地宽度须为 2—30 m，限高须为 1.5—16 m。');
 if(c.SideReserve<0||2*c.SideReserve>=c.SiteW)throw Error('每侧预留须非负，且两侧预留之和小于场地宽度。');
 const reserve=2*c.SideReserve,W=Math.min(c.SiteW-reserve,12000),leg=c.Foot===0?0:c.LegH,half=W/2-(c.Foot===2?leg*Math.tan(c.LegAngle*Math.PI/180):0);
 if(leg<0||leg>6000||![0,1,2].includes(c.Foot)||c.LegAngle<0||c.LegAngle>40)throw Error('请检查第二步的拱脚形式、高度与角度。');
 if(W<2000)throw Error('扣除两侧预留后，跨度不足 2 m；请加宽场地或减小侧向预留。');
 if(half<=1)throw Error('下部内倾已占满跨度，请降低起拱高度或减小内倾角。');
 if(c.SiteH<=leg+1)throw Error('场地限高不高于起拱高度，请在第二步降低起拱高度，或提高场地限高。');
 const targetH=leg+.8*half,H=Math.floor(Math.min(c.SiteH,targetH)*1e6)/1e6;
 if(H<500)throw Error('当前拱脚条件无法生成有效初始模数，请调整起拱高度或内倾角。');
 return {W,H,targetH,leg,half,reserve,widthCapped:c.SiteW-reserve>12000,heightLimited:c.SiteH<targetH};
}
export function restoredFit(saved){
 if(saved?.moduleFit?.mode==='auto'||saved?.moduleFit?.mode==='manual')return {...saved.moduleFit};
 return saved?.parameters?{mode:'manual',legacy:true}:{mode:'auto',legacy:false};
}
export function migrateSiteParameters(c){
 const SiteL=c.SiteL>0?c.SiteL:c.L+2*c.SiteEnd;
 return {...c,SiteL,L:SiteL-2*c.SiteEnd};
}
export function siteEdges(c){
 if(![c.SiteW,c.SiteH,c.SiteL].every(Number.isFinite)||c.SiteW<=0||c.SiteH<=0||c.SiteL<=0)throw Error('请填写有效的场地尺寸。');
 const x=c.SiteW/2,y=c.SiteL,z=c.SiteH,a=0;
 const p=[[-x,a,0],[x,a,0],[x,y,0],[-x,y,0],[-x,a,z],[x,a,z],[x,y,z],[-x,y,z]].map(([x,y,z])=>({x,y,z}));
 return [[0,1],[1,2],[2,3],[3,0],[4,5],[5,6],[6,7],[7,4],[0,4],[1,5],[2,6],[3,7]].map(([a,b])=>({points:[p[a],p[b]],role:'site',index:0}));
}
export function siteGuide(c,stage,core){
 const g={c,members:siteEdges(c),frames:[],feet:[],patches:[],supports:[],stations:[],dashed:true,siteContext:true,guideError:''};
 if(stage===2){
  const x=c.SiteW/2,z=c.SiteH,p=[{x:-x,y:0,z:0},{x:x,y:0,z:0},{x:x,y:0,z},{x:-x,y:0,z}];
  for(let i=0;i<4;i++)g.members.push({points:[p[i],p[(i+1)%4]],role:'section',index:0});
  try{
   if(c.W<2000||c.W>12000||c.H<500||c.H>12000)throw Error('跨度须为 2—12 m，总高须为 0.5—12 m。');
   if(c.W>c.SiteW-2*(c.SideReserve??0)+.001||c.H>c.SiteH)throw Error('自定义尺寸超出扣除侧向预留后的场地，请调整跨度或高度。');
   const p=core.profile(c,c.H),points=Array.from({length:65},(_,i)=>{const q=p.at(i/64);return {x:q.x,y:0,z:q.z};});
   if(c.Foot){points.unshift({x:-c.W/2,y:0,z:0});points.push({x:c.W/2,y:0,z:0});}
   g.members.push({points,role:'arch',index:0});
  }catch(e){g.guideError=e.message;}
 }
 return g;
}
export function sectionDiagram(c,core){
 try{
  const p=core.profile(c,c.H),s=Math.min(204/c.SiteW,124/c.SiteH),x=v=>122+v*s,y=v=>151-v*s;
  const points=[...(c.Foot?[{x:-c.W/2,z:0}]:[]),...Array.from({length:65},(_,i)=>p.at(i/64)),...(c.Foot?[{x:c.W/2,z:0}]:[])];
  const d=points.map((p,i)=>`${i?'L':'M'}${x(p.x).toFixed(2)},${y(p.z).toFixed(2)}`).join(' ');
  return `<svg class="module-section" viewBox="${x(-c.SiteW/2)-7} ${y(c.SiteH)-7} ${c.SiteW*s+14} ${c.SiteH*s+14}" role="img" aria-label="灰色场地边界内的蓝色单榀模数，按实际尺寸绘制"><rect class="section-bound" x="${x(-c.SiteW/2)}" y="${y(c.SiteH)}" width="${c.SiteW*s}" height="${c.SiteH*s}"/><path class="section-arch" d="${d}"/></svg>`;
 }catch{return '';}
}

export function validateModule(c){
 if(!Number.isFinite(c.SideReserve)||c.SideReserve<0||2*c.SideReserve>=c.SiteW)throw Error('请检查每侧预留与场地宽度。');
 if(c.W<2000||c.W>12000||c.H<500||c.H>12000)throw Error('跨度须为 2—12 m，总高须为 0.5—12 m。');
 if(c.W>c.SiteW-2*c.SideReserve+.001||c.H>c.SiteH)throw Error('当前单拱超过扣除两侧预留后的空间；请调整尺寸或恢复自动适配。');
}
