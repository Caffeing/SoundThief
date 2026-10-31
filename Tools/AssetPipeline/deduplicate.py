import pathlib,subprocess,re,hashlib,json,shutil,collections
root=pathlib.Path(__file__).resolve().parents[2]
def paths(base):
 return [p.replace('\\','/') for p in subprocess.check_output(['rg','--files','--hidden',base],cwd=root).decode().splitlines()]
def guids():
 result={}
 for line in subprocess.check_output(['rg','--no-heading','--glob','*.meta','^guid:','Assets'],cwd=root).decode().splitlines():
  p,g=line.rsplit(':guid: ',1);result[g.strip()]=p.replace('\\','/')[:-5]
 return result
def digest(p):
 h=hashlib.sha256()
 with (root/p).open('rb') as f:
  while chunk:=f.read(1048576):h.update(chunk)
 return h.hexdigest()
if '--restore' in __import__('sys').argv:
 existing=guids(); copied=skipped=0; source=root/'SourceAssets'/'Extracted'
 for p in paths('SourceAssets/Extracted'):
  if p.endswith('.meta'):continue
  file=root/p;meta=pathlib.Path(str(file)+'.meta')
  if not meta.exists():continue
  rel=file.relative_to(source).as_posix().split('/Assets/',1)
  if len(rel)<2:continue
  original='Assets/'+rel[1]
  if original.startswith('Assets/EmaceArt/'):dest='Assets/ThirdParty/EmaceArt/'+original[len('Assets/EmaceArt/'):]
  elif original.startswith('Assets/Dungeon Megapack/'):dest='Assets/ThirdParty/RGPoly/DungeonMegapack/'+original[len('Assets/Dungeon Megapack/'):]
  else:continue
  if file.suffix.lower() in ('.unity','.lighting','.cs','.asmdef'):continue
  m=re.search(rb'^guid: ([a-f0-9]{32})',meta.read_bytes(),re.M)
  if not m:continue
  guid=m[1].decode()
  if guid in existing:skipped+=1;continue
  target=root/dest
  if target.exists():
   dest='Assets/ThirdParty/OriginalVariants/'+rel[1];target=root/dest
  target.parent.mkdir(parents=True,exist_ok=True)
  shutil.copy2(file,target);shutil.copy2(meta,str(target)+'.meta');existing[guid]=dest;copied+=1
 print(json.dumps({'restored_unique':copied,'already_present':skipped}),flush=True)
else:
 existing=guids(); bypath={p:g for g,p in existing.items()}
 protected=set((root/'Logs'/'binary-asset-dependencies.txt').read_text(encoding='utf-8-sig').splitlines())
 groups=collections.defaultdict(list)
 for p in paths('Assets/ThirdParty'):
  if p.endswith('.meta') or p not in bypath or p in protected or '/Resources/' in p or '/StreamingAssets/' in p:continue
  if (root/p).stat().st_size<32:continue
  groups[(pathlib.Path(p).suffix.lower(),digest(p))].append(p)
 refs=subprocess.run(['rg','--only-matching','--no-filename','--glob','*.unity','--glob','*.prefab','--glob','*.mat','--glob','*.asset','guid: [a-f0-9]{32}','Assets'],cwd=root,stdout=subprocess.PIPE).stdout.decode()
 count=collections.Counter(re.findall(r'guid: ([a-f0-9]{32})',refs)); redirects={};moves={}
 for key,files in groups.items():
  if len(files)<2:continue
  settings=collections.defaultdict(list)
  for p in files:
   data=(root/(p+'.meta')).read_bytes()
   data=re.sub(rb'^guid: [a-f0-9]+\r?\n',b'',data,flags=re.M)
   data=re.sub(rb'^timeCreated: .*\r?\n',b'',data,flags=re.M).replace(b'\r\n',b'\n')
   settings[hashlib.sha256(data).digest()].append(p)
  for group in settings.values():
   if len(group)<2:continue
   group.sort(key=lambda p:(-count[bypath[p]],'ProjectVariants' in p,'OriginalVariants' in p,len(p),p))
   keep=group[0]
   for p in group[1:]:redirects[bypath[p]]=bypath[keep];moves[p]=keep
 rewrite_ext={'.unity','.prefab','.mat','.asset','.anim','.controller','.meta','.json','.cs','.md','.asmdef','.asmref','.uxml'}
 backup=root/'SourceAssets'/'Backups'/'BeforeDeduplication'
 changed=[]
 pattern=re.compile(rb'guid: ([a-f0-9]{32})')
 for base in ('Assets','ProjectSettings'):
  for p in paths(base):
   if pathlib.Path(p).suffix.lower() not in rewrite_ext or p in moves or (p.endswith('.meta') and p[:-5] in moves):continue
   data=(root/p).read_bytes()
   if b'\0' in data[:1024]:continue
   result=pattern.sub(lambda m:b'guid: '+redirects.get(m[1].decode(),m[1].decode()).encode(),data)
   if pathlib.Path(p).suffix.lower() in ('.cs','.md','.json'):
    for old,new in moves.items():result=result.replace(old.encode(),new.encode())
   if result!=data:
    dest=backup/p;dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(data);(root/p).write_bytes(result);changed.append(p)
 for p in moves:
  target=backup/p;target.parent.mkdir(parents=True,exist_ok=True)
  shutil.copy2(root/p,target);shutil.copy2(root/(p+'.meta'),str(target)+'.meta')
 (root/'Logs'/'asset-duplicates-to-delete.txt').write_text('\n'.join(moves),encoding='utf-8')
 (root/'Logs'/'asset-duplicate-remaps.json').write_text(json.dumps({'paths':moves,'guids':redirects,'rewritten':changed},indent=2),encoding='utf-8')
 print(json.dumps({'duplicates':len(moves),'rewritten_files':len(changed),'megabytes':sum((root/p).stat().st_size for p in moves)//1048576}),flush=True)

