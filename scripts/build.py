"""Build the standalone Windows x64 application. No game files are required."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess

ROOT=Path(__file__).resolve().parents[1]
def run(args): subprocess.run([str(a) for a in args],check=True)
def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--gcc',default=shutil.which('gcc'),help='MinGW-w64 x64 gcc path')
    parser.add_argument('--test',action='store_true',help='Run isolated UI/control tests; does not attach to a game')
    args=parser.parse_args()
    if os.name!='nt':parser.error('Windows x64 is required')
    if not args.gcc:parser.error('Add a MinGW-w64 x64 gcc to PATH or pass --gcc')
    csc=Path(os.environ['WINDIR'])/'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
    if not csc.exists():parser.error('.NET Framework 4.x x64 compiler not found')
    machine=subprocess.check_output([args.gcc,'-dumpmachine'],text=True).strip()
    if 'x86_64' not in machine:parser.error('gcc must target x86_64 Windows')
    work=ROOT/'build';dist=ROOT/'dist';work.mkdir(exist_ok=True);dist.mkdir(exist_ok=True)
    data=(ROOT/'src/bridge/gold_hook.lua').read_bytes()
    (work/'live_payload.h').write_text('static const unsigned char payload[] = {'+','.join(map(str,data))+',0};\n',encoding='ascii')
    bridge=work/'KRFLocalGoldBridge.dll'
    run([args.gcc,'-shared','-O2','-fno-strict-aliasing','-static-libgcc','-Wall','-Wextra','-I',work,'-o',bridge,ROOT/'src/bridge/LiveBridge.c','-luser32'])
    compatibility=json.loads((ROOT/'compatibility.json').read_text())
    bridge_hash=hashlib.sha256(bridge.read_bytes()).hexdigest()
    info={'GameHash':compatibility['game_sha256'],'LuaHash':compatibility['lua51_sha256'],'BridgeHash':bridge_hash}
    (work/'BuildInfo.cs').write_text('internal static class BuildInfo {\n'+''.join(' internal const string '+k+'="'+v+'";\n' for k,v in info.items())+'}\n')
    (work/'Version.cs').write_text('[assembly:System.Reflection.AssemblyVersion("0.1.0.0")]\n[assembly:System.Reflection.AssemblyFileVersion("0.1.0.0")]\n[assembly:System.Reflection.AssemblyProduct("KingdomRushFrontiersGoldTrainer")]\n')
    common=[csc,'/nologo','/platform:x64','/optimize+','/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll','/reference:Accessibility.dll',
            '/resource:'+str(bridge)+',KRFLocalGoldBridge','/resource:'+str(ROOT/'assets/app.ico')+',AppIcon','/win32icon:'+str(ROOT/'assets/app.ico'),'/win32manifest:'+str(ROOT/'assets/app.manifest')]
    sources=sorted((ROOT/'src/ui').glob('*.cs'))+[work/'BuildInfo.cs',work/'Version.cs']
    exe=dist/'KingdomRushFrontiersGoldTrainer.exe'
    run(common+['/target:winexe','/out:'+str(exe)]+sources)
    if args.test:
        tests=work/'UiTests.exe'
        run(common+['/target:exe','/main:CompactTests','/out:'+str(tests)]+sources+[ROOT/'tests/UiTests.cs'])
        run([tests])
    digest=hashlib.sha256(exe.read_bytes()).hexdigest()
    (dist/'SHA256SUMS.txt').write_text(digest+'  '+exe.name+'\n',encoding='ascii')
    print('Built:',exe)
if __name__=='__main__':main()
