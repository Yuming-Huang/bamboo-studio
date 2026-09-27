import fs from 'node:fs';import path from 'node:path';import {fileURLToPath} from 'node:url';import {build} from 'esbuild';
const root=path.dirname(fileURLToPath(import.meta.url)),ui=path.join(root,'ui');
const js=await build({entryPoints:[path.join(ui,'app.js')],bundle:true,minify:true,format:'iife',platform:'browser',target:'es2022',write:false});
const html=fs.readFileSync(path.join(ui,'template.html'),'utf8').replace('/*__CSS__*/',()=>['styles.css','sidebar.css','analysis.css','capture.css','sidebar-toggle.css','scheme-editor.css','guidance.css'].map(f=>fs.readFileSync(path.join(ui,f),'utf8')).join('\n')).replace('/*__CORE__*/',()=>fs.readFileSync(path.join(ui,'core.js'),'utf8')).replace('/*__BUNDLE__*/',()=>js.outputFiles[0].text.replace(/<\/script/gi,'<\\/script'));
fs.writeFileSync(path.join(ui,'index.html'),html);console.log('Built local interface:',Buffer.byteLength(html),'bytes');
