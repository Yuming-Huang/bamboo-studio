export function validCandidate(x){
 if(x?.solved!==true||x.warnings?.length||x.error)return false;
 if(![x.displacementMm,x.stressMPa,x.massKg,x.limitMm].every(Number.isFinite)||x.displacementMm<0||x.stressMPa<0||x.massKg<=0||x.limitMm<=0)return false;
 if(x.parameters?.Buckle&&(!Number.isFinite(x.bucklingFactor)||x.bucklingFactor<=0))return false;
 if(x.parameters?.Fc>0&&x.parameters?.Ft>0&&x.parameters?.Fb>0&&!Number.isFinite(x.stressRatio))return false;
 return true;
}
export function relativeCandidate(bundle){
 if(!bundle||bundle.B||bundle.search?.complete===false)return null;
 const candidates=(bundle.types||[]).map(row=>row.result||row.reference).filter(validCandidate);
 const score=x=>Math.max(x.displacementMm/x.limitMm,Number.isFinite(x.stressRatio)?x.stressRatio:0,x.parameters?.Buckle?1/x.bucklingFactor:0);
 candidates.sort((a,b)=>Number(!!b.pass)-Number(!!a.pass)||(a.pass?a.massKg-b.massKg:score(a)-score(b))||(a.pass?score(a)-score(b):a.massKg-b.massKg)||a.type-b.type);
 const best=candidates[0];return best?{...best,screeningScore:score(best)}:null;
}

