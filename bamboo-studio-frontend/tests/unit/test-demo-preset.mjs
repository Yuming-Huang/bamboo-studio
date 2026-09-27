import assert from 'node:assert/strict';import fs from 'node:fs';import '../../ui/core.js';
import {fiveTypePreset,installDemoOnce,loadDemo,restoreDemoBackup,demoBackupKey,draftKey} from '../../ui/demo-preset.mjs';
import {searchPlan} from '../../ui/search-space.mjs';import {fitModule} from '../../ui/module-fit.mjs';
const p=fiveTypePreset();assert.equal(searchPlan(p).total,21);assert.equal(fitModule(p).W,p.W);assert.equal(fitModule(p).H,p.H);
for(const type of [0,1,2,4,5])for(const D of p.Diameters)for(const CrossAngle of type===2?p.CrossAngles:[20]){const g=ArchCore.build({...p,type,ArchD:D,CrossAngle});assert.ok(g.members.length>0);assert.equal(g.roofError,'');if(type===2)assert.ok(g.crossNodes.length>0);}
const state=new Map(),storage={getItem:k=>state.get(k)||null,setItem:(k,v)=>state.set(k,v)};
const prior=JSON.stringify({parameters:{W:10600,H:5716},snapshot:{saved:true}});storage.setItem(draftKey,prior);
assert.ok(installDemoOnce(storage));assert.equal(storage.getItem(demoBackupKey),prior);assert.equal(JSON.parse(storage.getItem(draftKey)).parameters.W,5600);
assert.ok(restoreDemoBackup(storage));assert.equal(storage.getItem(draftKey),prior);assert.equal(installDemoOnce(storage),false);assert.equal(storage.getItem(draftKey),prior);
loadDemo(storage);assert.equal(storage.getItem(demoBackupKey),prior);
const live=JSON.parse(fs.readFileSync(new URL('../fixtures/recorded-karamba-preset.json',import.meta.url),'utf8')).result;
assert.equal(live.engineId,'karamba3d');assert.equal(live.search.requested,21);assert.ok(live.search.complete);
for(const row of live.types){const r=row.result||row.reference;assert.ok(r?.solved);assert.ok(r.members.length);assert.equal(ArchCore.build(r.parameters).roofError,'');}
assert.equal(live.candidates.filter(x=>Number.isFinite(x.massKg)).length,21);
fs.writeFileSync('test-demo-preset-results.json',JSON.stringify({pass:true,geometryCandidates:21,recordedKarambaCandidates:21,recordedTypes:5,backupRestoreVerified:true,newStructuralSolve:false},null,2));console.log('Preset: 21 geometry/recorded Karamba results + backup restore passed (no new solve)');
