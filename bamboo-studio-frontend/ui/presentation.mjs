// Display-only convention. Never included in solver inputs or Rhino geometry.
export const schematicScale=.45;
export const presentationNote='轻盈示意 · 杆径缩小显示';
export function memberDiameterMm(member,parameters){
 return member.diameterMm??(member.role==='purlin'?parameters.PurlinD:parameters.ArchD);
}
export function displayedDiameterMm(member,parameters,mode){
 return memberDiameterMm(member,parameters)*(mode==='schematic'?schematicScale:1);
}
