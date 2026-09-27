// Curated geometric examples, not structurally approved designs.
// Columns: total height, lower height, leg angle, longitudinal crossing angle, truss depth.
export const TABLE = [
 [[2900,0,0,25,600],[4000,1000,0,25,600],[3400,1000,10,25,600]],
 [[4200,0,0,25,600],[5200,1000,0,25,600],[5000,1000,10,25,600]],
 [[2500,0,0,30,600],[3700,1000,0,25,600],[3300,1000,10,25,600]],
 [[4500,0,0,20,600],[5500,1000,0,20,600],[5200,1000,10,20,600]],
 [[2900,0,0,25,600],[4000,1000,0,25,600],[3400,1000,10,25,600]],
 [[4800,0,0,25,600],[5800,1000,0,25,600],[5500,1000,10,25,600]]
];
export const choiceKey = c => `${c.type}:${c.Foot}`;
export function createExample(defaults,type=0,Foot=1){
 if(!Number.isInteger(type)||!Number.isInteger(Foot)||!TABLE[type]?.[Foot])throw Error('未知原型组合');
 const [H,LegH,LegAngle,CrossAngle,TrussDepth]=TABLE[type][Foot];
 return {...defaults,type,Foot,W:6000,H,LegH,LegAngle,CrossAngle,TrussDepth,Panels:8,
  L:12000,S:2000,End:true,Axis:0,HeightMode:0,RoofType:0};
}
export function isExample(c,defaults){
 const example=createExample(defaults,c.type,c.Foot);
 return Object.keys(defaults).every(key=>c[key]===example[key]);
}
export class ExampleBook {
 constructor(defaults){this.defaults=defaults;this.drafts=new Map();}
 remember(c){this.drafts.set(choiceKey(c),{...c});}
 switchFrom(c,type,Foot){
  this.remember(c);const id=choiceKey({type,Foot}),restored=this.drafts.has(id);
  const next=restored?{...this.drafts.get(id)}:createExample(this.defaults,type,Foot);
  this.remember(next);return {parameters:next,restored};
 }
 restore(records,validate){
  if(!Array.isArray(records))return;
  for(const item of records.slice(0,18))try{const c=validate(item);this.remember(c);}catch{}
 }
 serializable(validate){
  const valid=[];for(const c of this.drafts.values())try{valid.push(validate(c));}catch{}
  return valid;
 }
}
