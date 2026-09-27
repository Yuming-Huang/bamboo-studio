import {displayKeys} from './client.mjs';

// Geometry-only editing context. It intentionally holds no solver metrics,
// member stresses, pass flag, or authority to select/export a solved scheme.
export class PendingPreview {
 constructor(snapshot=null){this.restore(snapshot);}
 restore(snapshot){
  this.record=null;this.edited=new Set();
  if(snapshot&&[0,1,2,4,5].includes(snapshot.type)&&snapshot.parameters&&snapshot.input){
   const numeric=values=>Object.fromEntries(Object.entries(values).filter(([,v])=>typeof v==='boolean'||typeof v==='number'&&Number.isFinite(v)));
   this.record={type:snapshot.type,parameters:numeric(snapshot.parameters),input:numeric(snapshot.input)};
   this.edited=new Set(Array.isArray(snapshot.edited)?snapshot.edited.filter(k=>typeof k==='string'):[]);
  }
 }
 remember(result,input){
  if(!result?.parameters||![0,1,2,4,5].includes(result.type))return;
  this.restore({type:result.type,parameters:result.parameters,input,edited:[]});
 }
 noteChange(key,before,after){
  if(!this.record)return;
  this.edited.add(key);
  for(const k of Object.keys(after))if(!Object.is(before[k],after[k]))this.edited.add(k);
 }
 snapshot(){return this.record?{...structuredClone(this.record),edited:[...this.edited]}:null;}
 parameters(current){
  if(!this.record)return null;
  const p={...this.record.parameters};
  // Also account for automatic site fitting and changes made outside controls.
  for(const [k,v] of Object.entries(current))if(this.edited.has(k)||!Object.is(v,this.record.input[k]))p[k]=v;
  for(const k of displayKeys)p[k]=current[k];
  if(['P','PMode','PurlinCount'].some(k=>this.edited.has(k)||!Object.is(current[k],this.record.input[k])))
   for(const k of ['P','PMode','PurlinCount'])p[k]=current[k];
  p.type=this.record.type;
  return p;
 }
 build(current,core,roof=false){
  const p=this.parameters(current);if(!p)return null;
  const g=core.build(p);g.pendingAnalysis=true;
  if(!roof){g.patches=[];g.supports=[];}
  return g;
 }
}
