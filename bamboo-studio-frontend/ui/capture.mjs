// Capture only the renderer, then add optional labels in separate margins.
// DOM controls and floating result cards never enter the image.
export function captureControls({view,context,onGrid,toast}){
 const $=q=>document.querySelector(q);let active=false,busy=false,previousGrid=true;
 function refresh(){
  const c=context(),annotated=$('#captureAnnotations').checked,stress=annotated&&c.stress;
  document.body.classList.toggle('capture-annotated',annotated);
  document.body.classList.toggle('capture-stress',!!stress);document.body.classList.toggle('capture-schematic',!!c.presentationNote);$('#capturePresentation').textContent=c.presentationNote||'';$('#capturePresentation').hidden=!c.presentationNote;
  const caption=$('#captureCaption');caption.replaceChildren();
  for(const label of c.labels){const span=document.createElement('span');span.textContent=label;caption.append(span);}
  $('#captureLegend').hidden=!stress;
  if(c.windLabel){const span=document.createElement('span');span.className='capture-wind-label';span.textContent=c.windLabel;caption.append(span);}
  $('#captureMax').textContent=(c.maxStress||0).toFixed(2)+' MPa';
  $('#captureGrid').checked=!!view.gridVisible;$('#captureGrid').disabled=document.body.dataset.engine==='fallback';
  $('#savePng').disabled=busy||!c.valid;$('#savePng').textContent=busy?'正在保存…':'保存 PNG';
  $('#captureAnnotations').disabled=busy;$('#captureGrid').disabled||=busy;
  $('#captureFit').disabled=busy||!c.valid;
 }
 function setActive(value){
  if(value===active)return;active=value;
  if(active){previousGrid=view.gridVisible;onGrid(false);$('#toast').hidden=true;}
  else onGrid(previousGrid);
  view.setGuidesVisible?.(!active&&!$('#viewport').classList.contains('view-info-hidden'));view.cleanLayout=active;document.body.classList.toggle('capture-mode',active);$('#captureToolbar').hidden=!active;
  $('#captureMode').setAttribute('aria-pressed',String(active));
  refresh();view.resize?.();
  requestAnimationFrame(()=>{view.resize?.();view.draw();(active?$('#canvasHost canvas'):$('#captureMode'))?.focus({preventScroll:true});});
 }
 $('#captureMode').onclick=()=>setActive(true);$('#exitCapture').onclick=()=>setActive(false);
 $('#toggleInfo').onclick=()=>{const hidden=$('#viewport').classList.toggle('view-info-hidden');$('#toggleInfo').textContent=hidden?'显示信息':'隐藏信息';$('#toggleInfo').setAttribute('aria-pressed',String(hidden));view.setGuidesVisible?.(!hidden&&!active);};
 $('#captureAnnotations').onchange=()=>{refresh();view.resize?.();view.draw();};
 $('#captureGrid').onchange=e=>{onGrid(e.target.checked);refresh();};
 $('#captureFit').onclick=()=>view.fit();
 document.addEventListener('keydown',e=>{if(e.key==='Escape'&&active){e.preventDefault();setActive(false);}});
 $('#savePng').onclick=async()=>{
  if(busy||!context().valid)return;
  // Freeze geometry, context and background before asynchronous image decoding.
  const data=context(),annotated=$('#captureAnnotations').checked;
  const color=getComputedStyle($('#viewport')).backgroundColor,sourceWidth=$('#canvasHost').clientWidth,dark=document.documentElement.dataset.theme==='dark';
  busy=true;refresh();
  try{
   const raw=view.capture(2),image=new Image();image.src=raw;await image.decode();
   const scale=image.naturalWidth/sourceWidth;
   const png=composeCapture(image,{...data,annotated,color,scale,dark});
   const stamp=new Date().toISOString().replace(/[-:TZ.]/g,'').slice(0,17);
   const name=`竹拱_${data.cases.join('')}_${annotated?'带标识':'纯净'}_${stamp}.png`;
   const response=await fetch('/api/save-image',{method:'POST',body:JSON.stringify({name,dataUrl:png})}),result=await response.json();
   if(!response.ok)throw Error(result.error||'图片保存失败');
   const message=result.cancelled?'已取消保存图片。':result.saved?'PNG 已保存：'+result.path:'图片未保存，请重试。';
   $('#captureStatus').textContent=message;toast(message);
  }catch(e){const message='图片保存失败：'+e.message;$('#captureStatus').textContent=message;toast(message);}
  finally{busy=false;refresh();}
 };
 refresh();return {refresh};
}

export function composeCapture(image,{labels,annotated,stress,maxStress,color,scale=2,dark=false,windLabel="",presentationNote=""}){
 const width=image.naturalWidth,height=image.naturalHeight;
 const heading=annotated?Math.round((windLabel?78:48)*scale):0,footer=annotated&&stress?Math.round(84*scale):0;
 const canvas=document.createElement('canvas');canvas.width=width;canvas.height=height+heading+footer+(presentationNote?Math.round(28*scale):0);
 const ctx=canvas.getContext('2d');ctx.fillStyle=color;ctx.fillRect(0,0,width,canvas.height);ctx.drawImage(image,0,heading);
 const ink=dark?'#e9edf1':'#222a35',muted=dark?'#b3bdc8':'#606c7c';
 ctx.textBaseline='middle';ctx.fillStyle=ink;ctx.font=`600 ${16*scale}px "Segoe UI", "Microsoft YaHei", sans-serif`;
 if(annotated)labels.forEach((label,i)=>ctx.fillText(label,i*width/labels.length+24*scale,24*scale,width/labels.length-48*scale));
 if(annotated&&windLabel){ctx.fillStyle=muted;ctx.font=`${12*scale}px "Segoe UI", "Microsoft YaHei", sans-serif`;ctx.fillText(windLabel,24*scale,56*scale,width-48*scale);}
 if(footer){
  const y=heading+height,w=Math.min(width-48*scale,360*scale),left=(width-w)/2;
  ctx.textAlign='center';ctx.font=`${12*scale}px "Segoe UI", "Microsoft YaHei", sans-serif`;ctx.fillText('正应力绝对值包络 · 同一色标 · 非安全等级',width/2,y+16*scale);
  const ramp=ctx.createLinearGradient(left,0,left+w,0);['#2456df','#21a2be','#8cb448','#efc544','#e34838'].forEach((v,i)=>ramp.addColorStop(i/4,v));
  ctx.fillStyle=ramp;ctx.fillRect(left,y+30*scale,w,8*scale);ctx.fillStyle=muted;
  ctx.textAlign='left';ctx.fillText('0',left,y+54*scale);ctx.textAlign='right';ctx.fillText(maxStress.toFixed(2)+' MPa',left+w,y+54*scale);
 }
 if(presentationNote){ctx.fillStyle=muted;ctx.textAlign='right';ctx.font=`${12*scale}px "Segoe UI", "Microsoft YaHei", sans-serif`;ctx.fillText(presentationNote,width-20*scale,canvas.height-14*scale);}
 return canvas.toDataURL('image/png');
}
