import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const REPO_ROOT = path.resolve(__dirname, '../..');
const DOCS_DIR = path.join(REPO_ROOT, 'docs');
const SIM_DIR = path.join(REPO_ROOT, 'sim');

const EXACT_FILES = [
  'index.html',
  'styles.css',
  'SPEC.md',
  'serve.mjs',
  'start-sim.bat',
];

const SYNC_DIRS = ['src', 'tests'];

const FORBIDDEN_IN_DOCS = [
  'CNAME',
  'RAKDA_3D_Sim.exe',
  'launcher',
];

const errors = [];

function compareFiles(relPath) {
  const docsFile = path.join(DOCS_DIR, relPath);
  const simFile = path.join(SIM_DIR, relPath);
  if (!fs.existsSync(docsFile)) {
    errors.push(`Missing in docs/: ${relPath}`);
    return;
  }
  if (!fs.existsSync(simFile)) {
    errors.push(`Missing in sim/: ${relPath}`);
    return;
  }
  const docsBuf = fs.readFileSync(docsFile);
  const simBuf = fs.readFileSync(simFile);
  if (!docsBuf.equals(simBuf)) {
    errors.push(`Byte mismatch between docs/${relPath} (${docsBuf.length} B) and sim/${relPath} (${simBuf.length} B)`);
  }
}

for (const rel of EXACT_FILES) {
  compareFiles(rel);
}

for (const dirName of SYNC_DIRS) {
  const docsSub = path.join(DOCS_DIR, dirName);
  const simSub = path.join(SIM_DIR, dirName);
  if (!fs.existsSync(docsSub) || !fs.existsSync(simSub)) {
    errors.push(`Missing sync directory ${dirName} in docs/ or sim/`);
    continue;
  }
  const docsEntries = fs.readdirSync(docsSub).sort();
  const simEntries = fs.readdirSync(simSub).sort();
  const allEntries = new Set([...docsEntries, ...simEntries]);
  for (const name of allEntries) {
    compareFiles(path.join(dirName, name));
  }
}

for (const forbidden of FORBIDDEN_IN_DOCS) {
  const p = path.join(DOCS_DIR, forbidden);
  if (fs.existsSync(p)) {
    errors.push(`Forbidden path exists in docs/: ${forbidden}`);
  }
}

if (!fs.existsSync(path.join(DOCS_DIR, '.nojekyll'))) {
  errors.push('Missing required GitHub Pages marker: docs/.nojekyll');
}

if (errors.length > 0) {
  console.error('check-sync FAILED:');
  for (const err of errors) {
    console.error(`  - ${err}`);
  }
  process.exit(1);
}

console.log('check-sync PASSED: docs/ and sim/ web assets are 100% byte-identical and docs/ is clean.');
