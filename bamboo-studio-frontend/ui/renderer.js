import {memberDiameterMm,displayedDiameterMm} from './presentation.mjs';
import {roofPalette} from './roof-style.mjs';
import {draftStyle} from './draft-preview.mjs';
import {addWind,windGlyphs} from './wind-renderer.js';
import * as T from 'three';
import {OrbitControls} from 'three/addons/controls/OrbitControls.js';
import {viewDirection,viewHalfHeight} from './view-settings.mjs';
const xyz=p=>new T.Vector3(p.x/1000,p.z/1000,p.y/1000);
export function stressColor(value,max){const colors=[0x2456df,0x21a2be,0x8cb448,0xefc544,0xe34838],t=T.MathUtils.clamp(value/Math.max(max,.001),0,1)*4,i=Math.min(3,Math.floor(t));return new T.Color(colors[i]).lerp(new T.Color(colors[i+1]),t-i);}
export class ModelView{
 constructor(host,dark=false,paper=false){
  this.host=host;this.dark=dark;this.paper=paper;this.displayMode='schematic';this.solid=true;this.roofVisible=true;this.gridVisible=true;this.needFit=true;
  this.renderer=new T.WebGLRenderer({antialias:true,alpha:true,preserveDrawingBuffer:true});this.renderer.setPixelRatio(Math.min(devicePixelRatio,2));this.renderer.shadowMap.enabled=true;this.renderer.shadowMap.type=T.PCFSoftShadowMap;this.renderer.setClearColor(0,0);this.renderer.outputColorSpace=T.SRGBColorSpace;this.renderer.toneMapping=T.ACESFilmicToneMapping;this.renderer.toneMappingExposure=1.22;
  host.append(this.renderer.domElement);this.labelLayer=document.createElement('div');this.labelLayer.className='guide-labels';host.append(this.labelLayer);this.guidesVisible=true;let canvas=this.renderer.domElement;canvas.tabIndex=0;canvas.setAttribute('aria-label','三维竹拱；方向键旋转，加减键缩放，0键居中');
  this.scene=new T.Scene();this.camera=new T.OrthographicCamera(-10,10,10,-10,.01,1000);this.camera.position.set(13,10,-18);this.controls=new OrbitControls(this.camera,canvas);this.controls.enableDamping=false;this.controls.maxPolarAngle=Math.PI*.49;this.controls.minZoom=.35;this.controls.maxZoom=5;this.controls.addEventListener('change',()=>this.draw());
  this.scene.add(new T.HemisphereLight(dark?0xecf3ff:0xffffff,dark?0x434947:0xb9b6a8,2.6));
  const light=new T.DirectionalLight(0xfffcf3,2.4);light.position.set(-8,20,-10);light.castShadow=true;light.shadow.mapSize.set(2048,2048);Object.assign(light.shadow.camera,{left:-35,right:35,top:35,bottom:-35,near:1,far:90});light.shadow.bias=-.0005;light.shadow.normalBias=.025;this.scene.add(light);this.scene.add(new T.AmbientLight(0xffffff,.3));
  this.content=new T.Group();this.scene.add(this.content);this.grid=new T.GridHelper(100,100,dark?0x35404b:0xd7dde2,dark?0x2a323b:0xe1e5e7);this.grid.material.transparent=true;this.grid.material.opacity=paper?.3:.24;this.grid.position.y=-.07;this.scene.add(this.grid);
  const ground=new T.Mesh(new T.PlaneGeometry(160,160),new T.ShadowMaterial({opacity:dark?.09:.055}));ground.rotation.x=-Math.PI/2;ground.position.y=-.065;ground.receiveShadow=true;this.scene.add(ground);
  this.frameGroup=new T.Group();this.roofGroup=new T.Group();this.lineGroup=new T.Group();this.content.add(this.frameGroup,this.roofGroup,this.lineGroup);
  this.bamboo=new T.MeshStandardMaterial({color:dark?0xc8bba1:0xb6ab8f,roughness:1,metalness:0});this.rail=new T.MeshStandardMaterial({color:dark?0xb8b1a0:0xa9a38f,roughness:.9});this.roofMat=new T.MeshStandardMaterial({color:roofPalette(dark).surface,side:T.DoubleSide,transparent:true,opacity:.82,depthWrite:false,roughness:.86,metalness:0,forceSinglePass:true});this.lineMat=new T.LineBasicMaterial({color:dark?0xc5dfab:0x627a70});this.edgeMat=new T.LineBasicMaterial({color:roofPalette(dark).edge,transparent:true,opacity:.88,depthWrite:false});
  this.resizeObserver=new ResizeObserver(()=>this.resize());this.resizeObserver.observe(host);
  canvas.addEventListener('keydown',e=>{if(['+','=','-','0','ArrowLeft','ArrowRight','ArrowUp','ArrowDown'].includes(e.key)){e.preventDefault();if(e.key==='0')this.fit();else if(['+','=','-'].includes(e.key))this.zoom(e.key==='-'?.9:1.1);else{const offset=this.camera.position.clone().sub(this.controls.target),s=new T.Spherical().setFromVector3(offset);if(e.key==='ArrowLeft')s.theta-=.12;if(e.key==='ArrowRight')s.theta+=.12;if(e.key==='ArrowUp')s.phi=Math.max(.1,s.phi-.1);if(e.key==='ArrowDown')s.phi=Math.min(1.5,s.phi+.1);this.camera.position.copy(this.controls.target).add(new T.Vector3().setFromSpherical(s));this.controls.update();this.draw();}}});this.resize();
 }
 setTheme(dark){
  this.dark=dark;
  this.bamboo.color.setHex(dark?0xc8bba1:0xb6ab8f);this.rail.color.setHex(dark?0xb8b1a0:0xa9a38f);
  this.roofMat.color.setHex(roofPalette(dark).surface);this.lineMat.color.setHex(dark?0xc5dfab:0x627a70);this.edgeMat.color.setHex(roofPalette(dark).edge);
  this.scene.traverse(o=>{
   if(o.isHemisphereLight){o.color.setHex(dark?0xecf3ff:0xffffff);o.groundColor.setHex(dark?0x434947:0xb9b6a8);}
   if(o.material?.isShadowMaterial)o.material.opacity=dark?.09:.055;
   if(o.userData.draftMember)o.material.color.setHex(draftStyle(o.userData.draftMember,dark).color);
   if(o.userData.guideKind)o.material.color.setHex(o.userData.guideKind==='reserve'?(dark?0xa3b3c5:0x74869a):(dark?0x83b2ff:0x2864dc));
   if(o.userData.foundation)o.material.color.setHex(dark?0x65736b:0xaab3ad);
  });
  // GridHelper has four vertices per division. Recolor its existing buffer in place.
  const colors=this.grid.geometry.getAttribute('color'),center=(colors.count/4-1)/2;
  const major=new T.Color(dark?0x35404b:0xd7dde2),minor=new T.Color(dark?0x2a323b:0xe1e5e7);
  for(let i=0;i<colors.count;i++){const color=Math.floor(i/4)===center?major:minor;colors.setXYZ(i,color.r,color.g,color.b);}
  colors.needsUpdate=true;this.draw();
 }
 clear(){this.content.traverse(o=>{o.geometry?.dispose();if(o.userData.disposeMat)o.material.dispose();});this.content.clear();}
 setModel(g,single=false,other=null){
  this.clear();this.labelLayer.replaceChildren();this.g=g;this.other=other;this.single=single;this.comparison=!!(g&&other);this.groups=[];
  this.renderer.domElement.dataset.comparison=String(this.comparison);
  this.renderer.domElement.dataset.wind=String(!!g?.wind);
  this.renderer.domElement.dataset.models=g?(other?'C B':'single'):'';
  if(!g){this.bounds=null;this.draw();return;}
  this.addModel(g,single);if(other)this.addModel(other,false);
  const extent=[g,...(other?[other]:[])].flatMap(model=>model.viewPoints||[...model.members.filter(m=>!single||(m.index>=0&&m.index<(Math.floor(model.c.type/2)===1?2:1))).flatMap(m=>m.points),...model.patches.flatMap(p=>p.faces.flatMap(f=>f.points))]);this.bounds=new T.Box3().setFromPoints(extent.map(xyz));if(!g.viewPoints)this.bounds.expandByScalar(Math.max(g.c.ArchD,g.c.PurlinD,other?.c.ArchD||0,other?.c.PurlinD||0)/2000);this.setDisplay(this.solid);this.setRoof(this.roofVisible);
  if(this.needFit){this.fit();this.needFit=false;}else this.resize();this.draw();
 }
 addModel(g,single){
  const root=new T.Group(),frame=new T.Group(),roof=new T.Group(),line=new T.Group();root.add(frame,roof,line);this.content.add(root);this.groups.push({root,frame,roof,line,hasWind:!!g.wind,dashed:!!g.dashed});
  const limit=Math.floor(g.c.type/2)===1?2:1,ms=g.members.filter(m=>!single||(m.index>=0&&m.index<limit));
  for(const m of ms){const pts=m.points.map(xyz),path=new T.CurvePath();for(let i=1;i<pts.length;i++)if(pts[i].distanceTo(pts[i-1])>.000001)path.add(new T.LineCurve3(pts[i-1],pts[i]));if(!path.curves.length)continue;const actualDiameter=memberDiameterMm(m,g.c),displayDiameter=displayedDiameterMm(m,g.c,this.displayMode),diameter=displayDiameter/1000;
   const colored=Number.isFinite(m.stress),color=colored?stressColor(m.stress,g.stressMax):null;
   const material=colored?new T.MeshBasicMaterial({color}):m.role==='purlin'?this.rail:this.bamboo;
   const straight=pts.length===2,geometry=straight?new T.CylinderGeometry(diameter/2,diameter/2,pts[0].distanceTo(pts[1]),12,1,false):new T.TubeGeometry(path,Math.max(1,pts.length-1),diameter/2,12,false);const mesh=new T.Mesh(geometry,material);if(straight){mesh.position.copy(pts[0]).add(pts[1]).multiplyScalar(.5);mesh.quaternion.setFromUnitVectors(new T.Vector3(0,1,0),pts[1].clone().sub(pts[0]).normalize());}mesh.userData.disposeMat=colored;mesh.userData.actualDiameterMm=actualDiameter;mesh.userData.displayDiameterMm=displayDiameter;mesh.castShadow=true;mesh.receiveShadow=true;frame.add(mesh);
   const stroke=new T.Line(new T.BufferGeometry().setFromPoints(pts),g.dashed?new T.LineDashedMaterial(g.siteContext?draftStyle(m,this.dark):{color:this.dark?0xa8bab6:0x687e79,dashSize:.24,gapSize:.09}):colored?new T.LineBasicMaterial({color}):this.lineMat);stroke.userData.disposeMat=colored||g.dashed;if(g.dashed){stroke.computeLineDistances();if(g.siteContext)stroke.userData.draftMember=m;}line.add(stroke);
  }
  const foundations=g.foundations?.length?g.foundations:(single?g.feet.slice(0,limit*2):g.feet).map(point=>({point,width:260,height:100,depth:260}));for(const f of foundations.filter(x=>!single||x.index===undefined||x.index<limit)){const b=new T.Mesh(new T.BoxGeometry(f.width/1000,f.height/1000,f.depth/1000),new T.MeshStandardMaterial({color:this.dark?0x65736b:0xaab3ad,roughness:1}));b.position.copy(xyz(f.point));b.position.y=-f.height/2000;b.rotation.y=-(f.angle||0);b.userData.disposeMat=true;b.userData.foundation=true;b.receiveShadow=true;frame.add(b);}
  if(!single){for(const patch of g.patches){const vertices=patch.faces.flatMap(f=>f.points.flatMap(p=>[p.x/1000,p.z/1000,p.y/1000]));const geometry=new T.BufferGeometry();geometry.setAttribute('position',new T.Float32BufferAttribute(vertices,3));geometry.computeVertexNormals();const mesh=new T.Mesh(geometry,this.roofMat);mesh.renderOrder=1;roof.add(mesh);for(const border of patch.borders){const outline=new T.Line(new T.BufferGeometry().setFromPoints(border.map(xyz)),this.edgeMat);outline.renderOrder=2;roof.add(outline);}}}
  const guides=new T.Group();root.add(guides);this.groups.at(-1).guides=guides;guides.visible=this.guidesVisible;
  for(const guide of g.guides||[]){const mat=new T.LineBasicMaterial({color:guide.kind==='reserve'?(this.dark?0xa3b3c5:0x74869a):(this.dark?0x83b2ff:0x2864dc),transparent:true,opacity:.8});const stroke=new T.Line(new T.BufferGeometry().setFromPoints(guide.points.map(xyz)),mat);stroke.userData.disposeMat=true;stroke.userData.guideKind=guide.kind;guides.add(stroke);}
  for(const label of g.guideLabels||[]){const el=document.createElement('span');el.className='guide-label '+label.kind;el.textContent=label.text;el.worldPoint=xyz(label.point);this.labelLayer.append(el);}
  if(!single)addWind(root,g);
 }
 setGuidesVisible(value){this.guidesVisible=value;for(const group of this.groups||[])group.guides.visible=value;this.labelLayer.hidden=!value;this.draw();}
 setPresentation(mode){if(!['schematic','actual','line'].includes(mode))return;this.displayMode=mode;this.solid=mode!=='line';this.setModel(this.g,this.single,this.other);}
 setDisplay(solid){this.solid=solid;for(const g of this.groups||[]){g.frame.visible=solid&&!g.dashed;g.line.visible=!solid||g.dashed;}this.draw();}
 setRoof(v){this.roofVisible=v;for(const g of this.groups||[])g.roof.visible=v&&!g.hasWind;this.draw();}setGrid(v){this.gridVisible=v;this.grid.visible=v;this.draw();}
 fit(view='iso'){if(!this.bounds||this.bounds.isEmpty())return;const center=this.bounds.getCenter(new T.Vector3()),size=this.bounds.getSize(new T.Vector3());this.controls.target.copy(center);const radius=Math.max(size.length(),4);this.camera.up.set(0,1,0);const direction=new T.Vector3(...viewDirection(view,this.single,this.g?.c.type));this.camera.position.copy(center).add(direction.normalize().multiplyScalar(radius*2.5));this.camera.zoom=1;this.camera.lookAt(center);this.controls.update();this.base=viewHalfHeight(size,this.single);this.resize();}
 resize(){const w=this.host.clientWidth,h=this.host.clientHeight;if(!w||!h)return;this.renderer.setSize(w,h);const aspect=(this.comparison?Math.floor(w/2):w)/h;let half=this.base||8;if(this.bounds){const size=this.bounds.getSize(new T.Vector3());half=Math.max(half,(Math.max(size.x,size.z)*.65)/aspect);}const compact=this.comparison&&!this.cleanLayout?Math.max(0,620-h):0;half*=(1+compact/300)*(this.comparisonPadding||1);const lift=half*2*(compact*.28)/h;this.camera.left=-half*aspect;this.camera.right=half*aspect;this.camera.top=half-lift;this.camera.bottom=-half-lift;this.camera.updateProjectionMatrix();this.draw();}
 zoom(n){this.camera.zoom=T.MathUtils.clamp(this.camera.zoom*n,.35,5);this.camera.updateProjectionMatrix();this.draw();}
 draw(){
  const r=this.renderer,w=this.host.clientWidth,h=this.host.clientHeight;if(!w||!h)return;
  this.camera.updateMatrixWorld();const hostBox=this.host.getBoundingClientRect(),placed=[];
  const obstacles=[...this.host.parentElement.querySelectorAll('.display-bar,.view-actions,.model-metrics')].filter(el=>el.offsetWidth).map(el=>{const b=el.getBoundingClientRect();return {left:b.left-hostBox.left,right:b.right-hostBox.left,top:b.top-hostBox.top,bottom:b.bottom-hostBox.top};});
  for(const el of this.labelLayer.children){const p=el.worldPoint.clone().project(this.camera);let x=(p.x+1)*w/2+12,y=(1-p.y)*h/2-el.offsetHeight/2;const ew=el.offsetWidth,eh=el.offsetHeight;x=Math.max(4,Math.min(w-ew-4,x));for(const r of [...obstacles,...placed])if(x<r.right+4&&x+ew>r.left-4&&y<r.bottom+4&&y+eh>r.top-4)y=r.top-eh-7;y=Math.max(4,Math.min(h-eh-4,y));el.style.transform='none';el.style.left=x+'px';el.style.top=y+'px';el.hidden=p.z < -1||p.z>1;placed.push({left:x,right:x+ew,top:y,bottom:y+eh});}

  // One renderer and one orthographic camera: both panes always share exact scale,
  // orientation and world coordinates, including mouse/keyboard pan and zoom.
  if(this.comparison&&this.groups?.length===2){const half=Math.floor(w/2);r.setScissorTest(true);
   for(let i=0;i<2;i++){this.groups[0].root.visible=i===0;this.groups[1].root.visible=i===1;const x=i?w-half:0;r.setViewport(x,0,half,h);r.setScissor(x,0,half,h);r.render(this.scene,this.camera);}
   r.setScissorTest(false);r.setViewport(0,0,w,h);for(const g of this.groups)g.root.visible=true;
  }else{r.setScissorTest(false);r.setViewport(0,0,w,h);r.render(this.scene,this.camera);}
 }
 capture(scale=2){const r=this.renderer,ratio=r.getPixelRatio();try{r.setPixelRatio(Math.min(scale,3840/this.host.clientWidth,2160/this.host.clientHeight));this.draw();return r.domElement.toDataURL('image/png');}finally{r.setPixelRatio(ratio);this.draw();}}
}
export class FallbackView{
 setPresentation(){this.displayMode='line';this.solid=false;this.draw();}
 setGuidesVisible(value){this.guidesVisible=value;this.draw();}
 setTheme(dark){this.dark=dark;this.draw();}
 constructor(host,dark=false){this.host=host;this.dark=dark;this.displayMode='line';this.canvas=document.createElement('canvas');host.append(this.canvas);this.canvas.tabIndex=0;this.canvas.setAttribute('aria-label','兼容三维线稿预览');this.ctx=this.canvas.getContext('2d');this.yaw=-.65;this.zoomLevel=1;this.roofVisible=true;this.gridVisible=true;this.solid=true;let drag;this.canvas.onpointerdown=e=>{drag=e.clientX;this.canvas.setPointerCapture(e.pointerId);};this.canvas.onpointermove=e=>{if(drag===undefined)return;this.yaw+=(e.clientX-drag)*.006;drag=e.clientX;this.draw();};this.canvas.onpointerup=this.canvas.onpointercancel=()=>drag=undefined;this.canvas.onwheel=e=>{e.preventDefault();this.zoom(e.deltaY>0?.95:1.05);};this.canvas.onkeydown=e=>{if(e.key==='0')this.fit();else if(e.key==='ArrowLeft'){this.yaw-=.12;this.draw();}else if(e.key==='ArrowRight'){this.yaw+=.12;this.draw();}else if(e.key==='+')this.zoom(1.1);else if(e.key==='-')this.zoom(.9);};new ResizeObserver(()=>this.draw()).observe(host);}
 setModel(g,s,other=null){this.g=g;this.other=other;this.single=s;this.canvas.dataset.comparison=String(!!(g&&other));this.canvas.dataset.wind=String(!!g?.wind);this.canvas.dataset.models=g?(other?'C B':'single'):'';if(this.needFit){this.fit();this.needFit=false;}else this.draw();}setDisplay(s){this.solid=s;this.draw();}setRoof(v){this.roofVisible=v;this.draw();}setGrid(v){this.gridVisible=v;this.draw();}fit(v){this.yaw=v==='front'?0:this.single?([2,3].includes(this.g?.c.type)?-.45:-.18):-.65;this.top=v==='top';this.zoomLevel=1;this.draw();}zoom(n){this.zoomLevel=Math.min(4,Math.max(.35,this.zoomLevel*n));this.draw();}capture(){return this.canvas.toDataURL('image/png');}
 draw(){
  const w=this.host.clientWidth,h=this.host.clientHeight;if(!w||!h)return;this.canvas.width=w*2;this.canvas.height=h*2;const ctx=this.ctx;ctx.setTransform(2,0,0,2,0,0);ctx.clearRect(0,0,w,h);if(!this.g)return;
  const models=[this.g,...(this.other?[this.other]:[])],groups=models.map(g=>g.members.filter(m=>!this.single||m.index>=0&&m.index<(Math.floor(g.c.type/2)===1?2:1)));
  const cy=Math.cos(this.yaw),sy=Math.sin(this.yaw),q=p=>({x:p.x*cy-p.y*sy,y:this.top?p.y*cy+p.x*sy:(p.x*sy+p.y*cy)*.4-p.z*.9});
  const points=(this.g.viewPoints&&!this.other?this.g.viewPoints:groups.flatMap(ms=>ms.flatMap(m=>m.points))).map(q);if(!points.length)return;
  let minx=Infinity,maxx=-Infinity,miny=Infinity,maxy=-Infinity;for(const p of points){minx=Math.min(minx,p.x);maxx=Math.max(maxx,p.x);miny=Math.min(miny,p.y);maxy=Math.max(maxy,p.y);}
  const width=w/models.length,s=Math.min((width-70)/(maxx-minx||1),(h-185)/(maxy-miny||1))*this.zoomLevel;
  groups.forEach((ms,j)=>{ctx.save();ctx.beginPath();ctx.rect(j*width,0,width,h);ctx.clip();ctx.lineWidth=1.7;ctx.setLineDash(models[j].dashed?[6,5]:[]);for(const m of ms){ctx.globalAlpha=models[j].siteContext?draftStyle(m,this.dark).opacity:1;ctx.strokeStyle=models[j].siteContext?'#'+draftStyle(m,this.dark).color.toString(16).padStart(6,'0'):Number.isFinite(m.stress)?'#'+stressColor(m.stress,models[j].stressMax).getHexString():this.dark?'#cbb78a':'#9b8257';ctx.beginPath();m.points.map(q).forEach((p,i)=>{const x=(j+.5)*width+(p.x-(minx+maxx)/2)*s,y=h/2+(p.y-(miny+maxy)/2)*s;ctx[i?'lineTo':'moveTo'](x,y);});ctx.stroke();}
   if(this.guidesVisible!==false){const screen=p=>{const a=q(p);return {x:(j+.5)*width+(a.x-(minx+maxx)/2)*s,y:h/2+(a.y-(miny+maxy)/2)*s};};ctx.setLineDash([]);ctx.globalAlpha=.8;ctx.strokeStyle=this.dark?'#83b2ff':'#2864dc';for(const guide of models[j].guides||[]){ctx.beginPath();guide.points.map(screen).forEach((p,i)=>ctx[i?'lineTo':'moveTo'](p.x,p.y));ctx.stroke();}ctx.font='11px "Microsoft YaHei",sans-serif';ctx.fillStyle=ctx.strokeStyle;for(const label of models[j].guideLabels||[]){const p=screen(label.point);ctx.fillText(label.text,p.x+12,p.y);}}
   if(models[j].wind){const screen=p=>{const a=q(p);return {x:(j+.5)*width+(a.x-(minx+maxx)/2)*s,y:h/2+(a.y-(miny+maxy)/2)*s};};for(const a of windGlyphs(models[j])){const from=screen(a.a),to=screen(a.b),angle=Math.atan2(to.y-from.y,to.x-from.x);ctx.strokeStyle='#'+a.color.toString(16).padStart(6,'0');ctx.beginPath();ctx.moveTo(from.x,from.y);ctx.lineTo(to.x,to.y);ctx.lineTo(to.x-7*Math.cos(angle-.5),to.y-7*Math.sin(angle-.5));ctx.moveTo(to.x,to.y);ctx.lineTo(to.x-7*Math.cos(angle+.5),to.y-7*Math.sin(angle+.5));ctx.stroke();}}
   ctx.restore();});
 }
}

