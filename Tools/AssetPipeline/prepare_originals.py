import pathlib,hashlib,shutil,re,json,tarfile,zipfile,subprocess,gzip
root=pathlib.Path(__file__).resolve().parents[2]; downloads=pathlib.Path.home()/'Downloads'
archive=root/'SourceAssets'/'Archives'; archive.mkdir(parents=True,exist_ok=True)
manifest=[]; hashes={hashlib.sha256(p.read_bytes()).hexdigest():p for p in archive.iterdir() if p.is_file()}
for p in sorted(downloads.iterdir()):
 if not p.is_file() or p.suffix.lower() not in ('.zip','.unitypackage') or 'wiki' in p.name.lower():continue
 if not any(s in p.name.lower() for s in ('dungeon','harboria','village','kenney','eathe80','fbx_lod','fbx_nolod')):continue
 h=hashlib.sha256(p.read_bytes()).hexdigest()
 if h in hashes:continue
 name=re.sub(r' [(][0-9]+[)](?=[.][^.]+$)','',p.name)
 dest=archive/name
 if dest.exists() and hashlib.sha256(dest.read_bytes()).hexdigest()!=h:dest=archive/(p.stem+'-'+h[:8]+p.suffix)
 if not dest.exists():shutil.copy2(p,dest)
 hashes[h]=dest
 manifest.append({'file':dest.name,'bytes':p.stat().st_size,'sha256':h})
for p in archive.glob('*.zip'):
 if 'DUNGEON' not in p.name:continue
 with zipfile.ZipFile(p) as z:
  for item in z.infolist():
   if item.filename.endswith('.unitypackage'):
    dest=archive/pathlib.PurePosixPath(item.filename).name
    if not dest.exists():
     with z.open(item) as src,dest.open('wb') as dst:shutil.copyfileobj(src,dst)
for p in archive.glob('*.unitypackage'):
 pack='Harboria' if 'HARBORIA' in p.name else 'Slavica' if 'Village' in p.name else 'Dungeon'
 target=root/'SourceAssets'/'Extracted'/pack
 if (target/'.complete').exists():continue
 target.mkdir(parents=True,exist_ok=True)
 work=root/'SourceAssets'/'Work';work.mkdir(exist_ok=True)
 expanded=work/(pack+'.tar')
 with gzip.open(p,'rb') as src,expanded.open('wb') as dst:shutil.copyfileobj(src,dst,1048576)
 with tarfile.open(expanded,'r:') as tar:
  members={m.name:m for m in tar.getmembers()}
  for name,member in members.items():
   if not name.endswith('/pathname'):continue
   path=tar.extractfile(member).read().decode('utf-8-sig').strip()
   rel=pathlib.PurePosixPath(path)
   if '..' in rel.parts or not path.startswith('Assets/'):continue
   base=name[:-len('pathname')];dest=target.joinpath(*rel.parts)
   if base+'asset' in members:
    dest.parent.mkdir(parents=True,exist_ok=True)
    with tar.extractfile(members[base+'asset']) as src,dest.open('wb') as dst:shutil.copyfileobj(src,dst)
   else:dest.mkdir(parents=True,exist_ok=True)
   if base+'asset.meta' in members:
    pathlib.Path(str(dest)+'.meta').write_bytes(tar.extractfile(members[base+'asset.meta']).read())
 (target/'.complete').write_text(p.name,encoding='utf-8')
 expanded.unlink()
manifest=[{'file':p.name,'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in archive.iterdir() if p.is_file()]
out=root/'SourceAssets'/'catalog.json';out.write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print(json.dumps({'archives':len(manifest),'megabytes':sum(x['bytes'] for x in manifest)//1048576,'extracted': [p.name for p in (root/'SourceAssets'/'Extracted').iterdir()] }))

