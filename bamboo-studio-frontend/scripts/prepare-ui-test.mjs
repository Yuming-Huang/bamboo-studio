import fs from 'node:fs';import path from 'node:path';import {fileURLToPath} from 'node:url';
const root=fileURLToPath(new URL('../',import.meta.url));
const output=path.resolve(root,'artifacts/ui-test');fs.mkdirSync(output,{recursive:true});
const recorded=JSON.parse(fs.readFileSync(path.join(root,'tests/fixtures/recorded-karamba-preset.json'),'utf8')).result;
const tail=fs.readFileSync(path.join(root,'tests/desktop/replay-tail.js.txt'),'utf8');
fs.writeFileSync(path.join(output,'ui-fixture.js'),'// Test-only recorded response; no live solve.\n(()=>{const recorded='+JSON.stringify(recorded)+tail);
console.log(output);
