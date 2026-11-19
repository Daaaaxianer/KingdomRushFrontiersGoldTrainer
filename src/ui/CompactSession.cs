using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
internal sealed class ConnectionSession : IDisposable {
    internal Process Game;internal MemoryMappedFile Map;internal MemoryMappedViewAccessor View;
    internal static ConnectionSession Open(CancellationToken cancel){
        var s=new ConnectionSession();
        try {
            s.Game=PortableAssets.FindGame();cancel.ThrowIfCancellationRequested();
            string dll=PortableAssets.EnsureBridge();
            BackupSave();cancel.ThrowIfCancellationRequested();
            s.Map=MemoryMappedFile.CreateOrOpen("Local\\KRFLocalGoldLive_"+s.Game.Id,256,MemoryMappedFileAccess.ReadWrite);
            s.View=s.Map.CreateViewAccessor(0,256,MemoryMappedFileAccess.ReadWrite);
            int state=s.View.ReadInt32(4);if(state==3)throw new IOException("本次游戏连接曾失败，请正常重启游戏再试。");
            // Stay at normal income until the UI has accepted the completed connection.
            s.View.Write(0,1);s.View.Write(8,Connector.GetTickCount64());cancel.ThrowIfCancellationRequested();
            if(state!=1&&state!=2){s.View.Write(4,1);Connector.Inject(s.Game,dll);}
            cancel.ThrowIfCancellationRequested();return s;
        }catch{s.Dispose();throw;}
    }
    static void BackupSave(){
        string source=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"kingdom_rush_frontiers");
        try{if((File.GetAttributes(source)&FileAttributes.Directory)==0)return;}catch(DirectoryNotFoundException){return;}catch(FileNotFoundException){return;}
        CopyTree(source,Path.Combine(PortableAssets.BackupRoot,DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")));
    }
    static void CopyTree(string source,string target){Directory.CreateDirectory(target);
        foreach(string f in Directory.GetFiles(source))if((File.GetAttributes(f)&FileAttributes.ReparsePoint)==0)File.Copy(f,Path.Combine(target,Path.GetFileName(f)),false);
        foreach(string d in Directory.GetDirectories(source))if((File.GetAttributes(d)&FileAttributes.ReparsePoint)==0)CopyTree(d,Path.Combine(target,Path.GetFileName(d)));
    }
    public void Dispose(){if(View!=null){try{View.Write(0,1);View.Write(8,(ulong)0);}catch{}View.Dispose();View=null;}
        if(Map!=null){Map.Dispose();Map=null;}if(Game!=null){Game.Dispose();Game=null;}}
}

