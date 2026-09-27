// Structural diagrams, not perspective duplicates. Every line is a member.
const sample=(n,fn)=>Array.from({length:n+1},(_,i)=>fn(i/n));
function profile(pointed,half,rise,leg){
 const radius=pointed?(half*half+rise*rise)/(2*half):(half*half+rise*rise)/(2*rise);
 const cx=pointed?-half+radius:0,cz=pointed?leg:leg+(rise*rise-half*half)/(2*rise);
 const start=pointed?Math.PI:Math.atan2(leg-cz,-half),sweep=pointed?Math.atan2(rise,half-radius)-Math.PI:Math.PI/2-start;
 return u=>{if(u===0)return{x:-half,z:leg};if(u===1)return{x:half,z:leg};if(u===.5)return{x:0,z:leg+rise};const angle=start+sweep*(u>.5?2*(1-u):2*u);return{x:(u>.5?-1:1)*(cx+radius*Math.cos(angle)),z:cz+radius*Math.sin(angle)};};
}
export function diagram(type,foot=1,parameters=null){
 const pointed=type%2===1,truss=type>=4,cross=type===2||type===3,leg=foot?12:0,half=foot===2?36:40,rise=half*(pointed?1.45:.8);
 const upper=profile(pointed,half,rise,leg),lower=profile(pointed,half,rise-14,leg),members=[],nodes=[];
 const project=(p,y=0)=>({x:p.x+y*.48,y:-p.z+y*.17});
 const whole=fn=>[...(foot?[{x:-40,z:0}]:[]),...sample(64,fn),...(foot?[{x:40,z:0}]:[])];
 const add=(points,role)=>members.push({points,role});
 if(type===5&&globalThis.ArchCore){
  const c={...globalThis.ArchCore.defaults,...parameters,type:5,PointedVersion:parameters?.PointedVersion??3};
  for(const m of globalThis.ArchCore.pointedMembers(c,c.H))add(m.points.map(p=>project(p)),m.part);
  nodes.push(project({x:0,z:c.H}));
 }else if(cross){
  const gap=25,slope=Math.tan(25*Math.PI/180);
  for(let i=0;i<2;i++)add(whole(upper).map(p=>project(p,i*gap+p.z*slope*(i?-1:1))),'cross-rib');
  const z=gap/(2*slope);let a=0,b=.5;
  for(let i=0;i<50;i++){const u=(a+b)/2;if(upper(u).z<z)a=u;else b=u;}
  for(const u of [(a+b)/2,1-(a+b)/2]){const p=upper(u);nodes.push(project(p,gap/2));}
 }else{
  add(whole(upper).map(p=>project(p)),'upper');
  if(truss){
   add(sample(64,lower).map(p=>project(p)),'lower');
   for(let i=1;i<8;i++)add([upper(i/8),lower(i/8)].map(p=>project(p)),'web');
   // Shared chord endpoints form the end triangles; no duplicate end diagonal.
   for(let i=1;i<7;i++)add([i%2?upper(i/8):lower(i/8),i%2?lower((i+1)/8):upper((i+1)/8)].map(p=>project(p)),'web');
  }
 }
 const all=members.flatMap(m=>m.points),minx=Math.min(...all.map(p=>p.x)),maxx=Math.max(...all.map(p=>p.x)),miny=Math.min(...all.map(p=>p.y)),maxy=Math.max(...all.map(p=>p.y));
 const scale=Math.min(136/(maxx-minx),72/(maxy-miny));
 const fit=p=>({x:80+(p.x-(minx+maxx)/2)*scale,y:48+(p.y-(miny+maxy)/2)*scale});
 return{members:members.map(m=>({...m,points:m.points.map(fit)})),nodes:nodes.map(fit)};
}
export function protoSvg(type,foot=1,parameters=null){
 let d;try{d=diagram(type,foot,parameters);}catch{d=diagram(type,foot,null);}const path=points=>points.map((p,i)=>`${i?'L':'M'}${p.x.toFixed(2)} ${p.y.toFixed(2)}`).join(' ');
 return `<svg class="prototype-diagram" viewBox="0 0 160 96" aria-hidden="true" fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round">${d.members.map(m=>`<path data-member="${m.role}" d="${path(m.points)}" stroke-width="${m.role==='web'?1.15:1.8}"/>`).join('')}${d.nodes.map(p=>`<circle cx="${p.x.toFixed(2)}" cy="${p.y.toFixed(2)}" r="2.1" fill="currentColor" stroke="none"/>`).join('')}</svg>`;
}
