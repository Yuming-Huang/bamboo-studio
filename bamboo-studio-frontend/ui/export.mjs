import {parameterKey} from './client.mjs';

// A search result is based on C's input but contains B's actual parameters.
// Keep that provenance separate: never label C's recipe/fingerprint as B's.
export function canExportB({current,result,resultKey,busy=false}){
 return !busy&&!!result?.parameters&&!!result?.B?.solved&&!!result.B.parameters&&
  resultKey===parameterKey(current)&&parameterKey(result.parameters)===parameterKey(current);
}

export function parameterExport({caseId,current,result,resultKey,busy=false,variant='desktop-v1',createdAt=new Date().toISOString()}){
 if(!['C','B'].includes(caseId))throw Error('请选择 C 当前设计或 B 搜索推荐。');
 const fresh=!busy&&!!result?.parameters&&resultKey===parameterKey(current)&&parameterKey(result.parameters)===parameterKey(current);
 if(caseId==='B'&&!canExportB({current,result,resultKey,busy}))throw Error('B 尚未求解或已过期，请先重新计算 Run + Optimize。');
 const selected=fresh?result[caseId]:null;
 const parameters=structuredClone(caseId==='B'?selected.parameters:current);
 const analysis=selected?{
  schema:'bamboo-scheme-analysis/v1',executed:true,solved:!!selected.solved,selectedCase:caseId,
  engine:result.engine,engineVersion:result.engineVersion,createdAt:result.createdAt,
  parameters:structuredClone(parameters),result:structuredClone(selected),
  source:{schema:result.schema,searchFingerprint:result.fingerprint,inputParameters:structuredClone(result.parameters)},
  recommendation:caseId==='B'?result.recommendation:undefined,
  windScope:result.windScope,roofScope:result.roofScope,materialScope:result.materialScope,scope:result.scope
 }:{executed:false,note:'没有与这份参数一致的已完成计算结果；仅导出参数。'};
 return {
  schema:'bamboo-arch-web/v1',kind:'parameters',selectedCase:caseId,
  schemeName:caseId==='B'?'B 搜索推荐':'C 当前设计',createdAt,
  interfaceVersion:'20260920-desktop-v0.2.6',interfaceVariant:variant,
  parameters,analysis,scope:'本地研究初筛，不是施工图或安全批准。'
 };
}

export const parameterFileName=caseId=>caseId==='B'?'竹拱_B_搜索推荐_参数.json':'竹拱_C_当前设计_参数.json';
