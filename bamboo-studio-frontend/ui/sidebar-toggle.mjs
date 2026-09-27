// A view preference only: keep parameter DOM, scroll position, and results intact.
export function sidebarControls({view}){
 const root=document.documentElement,panel=document.querySelector('#controlPanel');
 const button=document.querySelector('#sidebarToggle'),label=button.querySelector('span');
 function setCollapsed(collapsed,persist=true){
  if(collapsed&&panel.contains(document.activeElement))button.focus({preventScroll:true});
  root.dataset.sidebar=collapsed?'collapsed':'expanded';
  panel.inert=collapsed;panel.setAttribute('aria-hidden',String(collapsed));
  button.setAttribute('aria-expanded',String(!collapsed));
  const action=collapsed?'展开参数侧栏':'向左收起参数侧栏';
  button.setAttribute('aria-label',action);button.title=action+"（Alt+S）";label.textContent=action;
  if(persist)try{localStorage.setItem('bamboo-ui-sidebar-collapsed-v1',String(collapsed));}catch{}
  requestAnimationFrame(()=>{view.resize?.();view.draw();});
 }
 button.addEventListener('click',()=>setCollapsed(root.dataset.sidebar!=='collapsed'));
 document.querySelector('.skip').addEventListener('click',e=>{
  e.preventDefault();setCollapsed(false);document.querySelector('#controls').focus({preventScroll:true});
 });
 setCollapsed(root.dataset.sidebar==='collapsed',false);
}
