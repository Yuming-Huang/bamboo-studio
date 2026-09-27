import * as T from 'three';
const xyz=p=>new T.Vector3(p.x/1000,p.z/1000,p.y/1000);
const add=(a,b)=>({x:a.x+b.x,y:a.y+b.y,z:a.z+b.z});
const mul=(p,s)=>({x:p.x*s,y:p.y*s,z:p.z*s});
export const windColor=p=>Math.abs(p)<1e-10?0x9ca7af:p>0?0xdd5138:0x287fc7;
export function windGlyphs(g){
 if(!g.wind)return [];
 const groups=new Map();for(const face of g.wind.faces){const key=face.surface+'/'+face.zone;if(!groups.has(key))groups.set(key,[]);groups.get(key).push(face);}
 const size=Math.max(400,Math.min(g.c.W*.1,900)),arrows=[];
 for(const faces of groups.values())for(let i=0;i<faces.length;i+=Math.max(1,Math.ceil(faces.length/10))){
  const f=faces[i],force=f.forceKN,len=Math.hypot(force.x,force.y,force.z);if(len<1e-10)continue;
  const center=mul(f.points.reduce(add,{x:0,y:0,z:0}),1/3),delta=mul(force,size/len);
  arrows.push({a:f.pressure>0?add(center,mul(delta,-1)):center,b:f.pressure>0?center:add(center,delta),color:windColor(f.pressure)});
 }
 const points=g.wind.faces.flatMap(f=>f.points),center=mul(points.reduce(add,{x:0,y:0,z:0}),1/points.length);
 const a={x:Math.min(...points.map(p=>p.x))-size*1.7,y:center.y,z:Math.max(...points.map(p=>p.z))+size*.6};
 arrows.push({a,b:add(a,mul(g.wind.direction,size*2)),color:0x178653,incoming:true});return arrows;
}
export function addWind(root,g){
 const group=new T.Group();root.add(group);
 if(!g.wind)return group;
 for(const f of g.wind.faces){
  const geometry=new T.BufferGeometry().setFromPoints(f.points.map(xyz));geometry.computeVertexNormals();
  const material=new T.MeshBasicMaterial({color:windColor(f.pressure),side:T.DoubleSide,transparent:true,opacity:.23,depthWrite:false,polygonOffset:true,polygonOffsetFactor:1});
  const mesh=new T.Mesh(geometry,material);mesh.userData.disposeMat=true;mesh.renderOrder=2;group.add(mesh);
 }
 for(const a of windGlyphs(g)){
  const start=xyz(a.a),end=xyz(a.b),delta=end.clone().sub(start),length=delta.length();
  const arrow=new T.ArrowHelper(delta.normalize(),start,length,a.color,length*.24,length*.12);
  // ArrowHelper shares its geometry. Clone it so normal model disposal is safe.
  arrow.traverse(o=>{if(o.geometry)o.geometry=o.geometry.clone();if(o.material)o.userData.disposeMat=true;});group.add(arrow);
 }return group;
}
