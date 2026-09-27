import {spawnSync} from 'node:child_process';
import fs from 'node:fs';
import {fileURLToPath} from 'node:url';
const output=fileURLToPath(new URL('../artifacts/test-results/',import.meta.url));
fs.mkdirSync(output,{recursive:true});
for(const name of ['demo-preset','roof-choice','gh080','screening','result-status','pending-preview','export']){
 const file=fileURLToPath(new URL(`../tests/unit/test-${name}.mjs`,import.meta.url));
 const result=spawnSync(process.execPath,[file],{cwd:output,stdio:'inherit'});
 if(result.error)throw result.error;
 if(result.status!==0)process.exit(result.status||1);
}
