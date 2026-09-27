// A reproducible display preset verified through the installed Karamba3D bridge.
export const demoKey='bamboo-five-types-demo-20260927';
export const draftKey='bamboo-karamba-v080';
export const demoBackupKey=demoKey+'-previous-draft';
export function fiveTypePreset(){return {...structuredClone(globalThis.ArchCore.defaults),
 SiteL:17000,SiteW:8000,SiteH:6500,SideReserve:1200,SiteEnd:2500,L:12000,S:2000,
 W:5600,H:4040,Foot:1,LegH:1800,LegAngle:12,Axis:0,HeightMode:0,
 CrossAngle:20,CrossAngles:[20,15,25],ArchD:150,Diameters:[100,150,200],SearchD2:150,SearchD3:200,
 HeightSearch:false,TrussDepth:450,TrussDepths:[450],Panels:8,PanelCounts:[8],
 PointE:1200,PointEs:[1200],PointSlope:.9,PointSlopes:[.9],PointFootGap:10,PointFootGaps:[10],PointPanels:6,PointPanelCounts:[6],
 PMode:1,PurlinCount:9,Mesh:2,Buckle:false,Run:true,Optimize:true,CrossCompare:true,type:0};}
export function loadDemo(storage){
 const old=storage.getItem(draftKey)||storage.getItem('bamboo-karamba-v070')||storage.getItem('bamboo-six-stage-v032');
 if(old)storage.setItem(demoBackupKey,old);
 storage.setItem(draftKey,JSON.stringify({parameters:fiveTypePreset(),moduleFit:{mode:'auto',legacy:false},selection:null,lastManual:null,preview:null,editorDraft:null,snapshot:null}));
 storage.setItem(demoKey,'1');
}
export function installDemoOnce(storage){if(storage.getItem(demoKey))return false;loadDemo(storage);return true;}
export function restoreDemoBackup(storage){const old=storage.getItem(demoBackupKey);if(!old)return false;storage.setItem(draftKey,old);storage.setItem(demoKey,'1');return true;}
