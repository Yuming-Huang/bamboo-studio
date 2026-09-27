import {siteGuide,siteEdges} from './module-fit.mjs';

// Presentation only: analysis and export continue to build their own real geometry.
export function draftPreview(c,stage,core){
 const box=siteEdges(c),viewPoints=box.flatMap(m=>m.points);
 if(stage<=2){const g=siteGuide(c,stage,core);g.viewPoints=viewPoints;return g;}
 const g=core.build(c);
 g.patches=[];g.supports=[];g.feet=[];g.dashed=true;g.siteContext=true;g.viewPoints=viewPoints;
 g.members=[...box,...g.members.filter(m=>stage!==3||m.role!=='purlin').map(m=>({...m,previewRole:m.role==='arch'?(m.index===0?'source':'copy'):'purlin'}))];
 g.guides=[];g.guideLabels=[];
 if(stage!==3)return g;
 const ys=g.frames.map(f=>f.origin.y),first=ys[0],last=ys.at(-1);
 if(!Number.isFinite(first)||ys.length<2)return g;
 const x=c.SiteW/2+550,z=30,p=y=>({x,y,z}),f=n=>(n/1000).toFixed(2)+' m';
 g.guides.push({points:[p(first),p(last)],kind:'axis'});
 g.guides.push({points:[{x:x-160,y:last-350,z},p(last),{x:x+160,y:last-350,z}],kind:'axis'});
 for(const y of ys)g.guides.push({points:[{x:x-130,y,z},{x:x+130,y,z}],kind:'tick'});
 if(first>0){
  g.guides.push({points:[p(0),p(first)],kind:'reserve'});
  g.guideLabels.push({point:p(first/2),text:'预留 '+f(first),kind:'reserve'});
 }
 g.guideLabels.push({point:p((first+ys[1])/2),text:'榀距 '+f(ys[1]-first),kind:'spacing'});
 g.guideLabels.push({point:p(last+350),text:'沿 +Y 复制',kind:'direction'});
 return g;
}

export function draftStyle(m,dark){
 const muted=dark?0xa3b3c5:0x74869a,blue=dark?0x83b2ff:0x2864dc;
 const color=m.role==='site'||m.role==='section'?muted:m.role==='arch'&&m.previewRole!=='copy'?blue:dark?0xa8bab6:0x687e79;
 return {color,transparent:true,opacity:m.role==='site'?.38:m.role==='section'?.55:m.previewRole==='copy'?.75:1,
  dashSize:m.role==='arch'?.24:.14,gapSize:m.role==='arch'?.09:.12};
}
