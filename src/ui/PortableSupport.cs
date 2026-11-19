using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

internal static class PortableAssets
{
    internal static string Hash(string path) {
        using(var f=File.OpenRead(path))using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(f)).Replace("-","").ToLowerInvariant();
    }
    internal static bool Compatible(string exe) {
        return Hash(exe)==BuildInfo.GameHash && Hash(Path.Combine(Path.GetDirectoryName(exe),"lua51.dll"))==BuildInfo.LuaHash;
    }
    internal static Process FindGame() {
        Process found=null;int named=0, inaccessible=0;
        foreach(var process in Process.GetProcessesByName("Kingdom Rush Frontiers")) {
            named++;
            bool match=false;
            try { match=Compatible(process.MainModule.FileName); }
            catch(System.ComponentModel.Win32Exception) {inaccessible++;}
            catch(InvalidOperationException) {}
            catch(IOException) {}
            catch(UnauthorizedAccessException) {inaccessible++;}
            if(!match){process.Dispose();continue;}
            if(found!=null){found.Dispose();process.Dispose();throw new IOException("发现多个兼容的游戏进程，请只保留一个再连接。");}
            found=process;
        }
        if(found!=null)return found;
        if(inaccessible>0)throw new IOException("无法读取游戏进程。请让游戏与修改器以相同的普通用户权限运行。");
        if(named>0)throw new IOException("找到了游戏，但版本或运行库不匹配。此工具适配 Frontiers 本机版本，请重新适配。");
        throw new IOException("没有找到正在运行的《王国保卫战：前线》。请从 Steam 启动游戏并进入关卡后再连接。");
    }
    internal static string EnsureBridge() {
        string directory=Path.Combine(Path.GetTempPath(),"KRFLocalGoldTrainer",BuildInfo.BridgeHash);
        Directory.CreateDirectory(directory);
        string target=Path.Combine(directory,"KRFLocalGoldBridge.dll");
        if(File.Exists(target)) {
            if(Hash(target)!=BuildInfo.BridgeHash)throw new IOException("临时连接模块校验失败，请退出游戏后清理该工具的临时缓存再试。");
            return target;
        }
        string pending=Path.Combine(directory,Guid.NewGuid().ToString("N")+".tmp");
        try {
            using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream("KRFLocalGoldBridge")) {
                if(input==null)throw new IOException("应用内缺少连接模块。");
                using(var output=new FileStream(pending,FileMode.CreateNew,FileAccess.Write,FileShare.None))input.CopyTo(output);
            }
            if(Hash(pending)!=BuildInfo.BridgeHash)throw new IOException("应用内连接模块校验失败。");
            File.Move(pending,target);
            return target;
        }finally{if(File.Exists(pending))File.Delete(pending);}
    }
    internal static string BackupRoot {
        get {return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"KRFLocalGoldTrainer","存档备份");}
    }
}
