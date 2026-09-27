// Slightly front-facing views keep single-rib proportions readable.
// Complete arrays retain the established axonometric view.
export function viewDirection(view,single,type){
 if(view==='front')return [0,.03,-1];
 if(view==='top')return [0,1,-.001];
 if(!single)return [1,.7,-1.08];
 return type===2||type===3?[.72,.42,-1.65]:[.32,.23,-1.8];
}
export function viewHalfHeight(size,single){
 return single?Math.max(size.y*.70,2.5):Math.max(size.y+Math.max(size.x,size.z)*.42,6)*.67;
}
