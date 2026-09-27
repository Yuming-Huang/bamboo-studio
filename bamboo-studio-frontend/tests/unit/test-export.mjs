import assert from 'node:assert/strict';
import fs from 'node:fs';
import {parameterKey} from '../../ui/client.mjs';
import {canExportB,parameterExport,parameterFileName} from '../../ui/export.mjs';

const result=JSON.parse(fs.readFileSync(new URL('../fixtures/synthetic-export.json',import.meta.url),'utf8'));
const current=structuredClone(result.parameters),before=JSON.stringify({current,result});
const context={current,result,resultKey:parameterKey(current)};
const checks=[];
function check(name,test){test();checks.push({name,pass:true});}
check('B exports its actual optimized parameters, not C',()=>{const out=parameterExport({...context,caseId:'B'});assert.deepEqual(out.parameters,result.B.parameters);assert.notDeepEqual(out.parameters,current);assert.equal(out.selectedCase,'B');});
check('B result is exact, including element stress and geometry',()=>{const out=parameterExport({...context,caseId:'B'});assert.deepEqual(out.analysis.result,result.B);assert.deepEqual(out.analysis.parameters,out.parameters);assert.equal(out.analysis.selectedCase,'B');});
check('Original search input and fingerprint are explicitly provenance',()=>{const out=parameterExport({...context,caseId:'B'});assert.deepEqual(out.analysis.source.inputParameters,current);assert.equal(out.analysis.source.searchFingerprint,result.fingerprint);assert.equal(out.analysis.fingerprint,undefined);});
check('C still exports current dimensions and its own result',()=>{const out=parameterExport({...context,caseId:'C'});assert.deepEqual(out.parameters,current);assert.deepEqual(out.analysis.result,result.C);});
check('Exporting does not mutate C, B or the result record',()=>{parameterExport({...context,caseId:'B'});assert.equal(JSON.stringify({current,result}),before);});
check('Exports are detached copies',()=>{const out=parameterExport({...context,caseId:'B'});out.parameters.H=1;out.analysis.result.parameters.H=2;out.analysis.source.inputParameters.H=3;assert.equal(JSON.stringify({current,result}),before);});
check('Stale B is blocked and stale C exports no calculated values',()=>{const ctx={...context,current:{...current,W:current.W+100}};assert.equal(canExportB(ctx),false);assert.throws(()=>parameterExport({...ctx,caseId:'B'}));assert.equal(parameterExport({...ctx,caseId:'C'}).analysis.executed,false);});
check('Missing B is blocked',()=>{const ctx={...context,result:{...result,B:null}};assert.equal(canExportB(ctx),false);assert.throws(()=>parameterExport({...ctx,caseId:'B'}));});
check('Unsolved B is blocked',()=>{const ctx={...context,result:{...result,B:{...result.B,solved:false}}};assert.equal(canExportB(ctx),false);assert.throws(()=>parameterExport({...ctx,caseId:'B'}));});
check('Busy analysis cannot export B or pretend C has a completed result',()=>{const ctx={...context,busy:true};assert.throws(()=>parameterExport({...ctx,caseId:'B'}));assert.equal(parameterExport({...ctx,caseId:'C'}).analysis.executed,false);});
check('C before any analysis is a parameter-only export',()=>{const out=parameterExport({caseId:'C',current,result:null,resultKey:''});assert.deepEqual(out.parameters,current);assert.equal(out.analysis.executed,false);});
check('Unknown case cannot silently export C',()=>assert.throws(()=>parameterExport({...context,caseId:'D'})));
check('Files have different, explicit C/B names',()=>{assert.match(parameterFileName('B'),/_B_/);assert.match(parameterFileName('C'),/_C_/);assert.notEqual(parameterFileName('B'),parameterFileName('C'));});
check('Serialized export remains self-consistent',()=>{const out=JSON.parse(JSON.stringify(parameterExport({...context,caseId:'B'})));assert.deepEqual(out.parameters,out.analysis.result.parameters);assert.deepEqual(out.parameters,out.analysis.parameters);});
fs.writeFileSync('export-tests.json',JSON.stringify({pass:true,count:checks.length,checks},null,2));
console.log(`Export tests: ${checks.length} passed`);
