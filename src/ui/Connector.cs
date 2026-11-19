using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

internal static class Connector
{
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr OpenProcess(uint a,bool b,int c);
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr VirtualAllocEx(IntPtr p,IntPtr a,UIntPtr n,uint t,uint prot);
    [DllImport("kernel32.dll",SetLastError=true)] static extern bool WriteProcessMemory(IntPtr p,IntPtr a,byte[] b,UIntPtr n,out UIntPtr written);
    [DllImport("kernel32.dll",SetLastError=true)] static extern IntPtr CreateRemoteThread(IntPtr p,IntPtr sa,UIntPtr stack,IntPtr start,IntPtr arg,uint flags,out uint tid);
    [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(IntPtr h,uint ms);
    [DllImport("kernel32.dll")] static extern bool GetExitCodeThread(IntPtr h,out uint code);
    [DllImport("kernel32.dll")] static extern bool VirtualFreeEx(IntPtr p,IntPtr a,UIntPtr s,uint t);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr GetModuleHandle(string n);
    [DllImport("kernel32.dll",CharSet=CharSet.Ansi)] static extern IntPtr GetProcAddress(IntPtr m,string n);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern bool GetModuleHandleEx(uint flags,IntPtr address,out IntPtr module);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern uint GetModuleFileName(IntPtr module,StringBuilder path,int length);
    [DllImport("kernel32.dll")] internal static extern ulong GetTickCount64();

    public static void Inject(Process process,string dll)
    {
        IntPtr p=OpenProcess(0x043A,false,process.Id);
        if(p==IntPtr.Zero) throw new IOException("无法连接游戏进程。请让游戏与修改器以相同的普通用户权限运行。");
        IntPtr remote=IntPtr.Zero,thread=IntPtr.Zero;
        bool completed=false;
        try {
            var local=GetProcAddress(GetModuleHandle("kernel32.dll"),"LoadLibraryW");
            IntPtr owner;
            if(!GetModuleHandleEx(6,local,out owner)) throw new IOException("无法定位系统加载入口。");
            var path=new StringBuilder(1024); GetModuleFileName(owner,path,path.Capacity);
            string ownerName=Path.GetFileName(path.ToString());
            ProcessModule targetOwner=null;
            foreach(ProcessModule m in process.Modules) if(string.Equals(m.ModuleName,ownerName,StringComparison.OrdinalIgnoreCase))targetOwner=m;
            if(targetOwner==null)throw new IOException("游戏系统模块尚未就绪。");
            IntPtr targetLoader=new IntPtr(targetOwner.BaseAddress.ToInt64()+local.ToInt64()-owner.ToInt64());
            byte[] name=Encoding.Unicode.GetBytes(Path.GetFullPath(dll)+"\0");
            remote=VirtualAllocEx(p,IntPtr.Zero,(UIntPtr)name.Length,0x3000,4);
            UIntPtr count;
            if(remote==IntPtr.Zero || !WriteProcessMemory(p,remote,name,(UIntPtr)name.Length,out count) || count.ToUInt64()!=(ulong)name.Length)
                throw new IOException("无法准备连接模块，系统错误："+Marshal.GetLastWin32Error());
            uint tid;
            thread=CreateRemoteThread(p,IntPtr.Zero,UIntPtr.Zero,targetLoader,remote,0,out tid);
            if(thread==IntPtr.Zero)throw new IOException("无法加载连接模块，系统错误："+Marshal.GetLastWin32Error());
            if(WaitForSingleObject(thread,15000)!=0)throw new IOException("连接模块加载超时。请正常退出游戏后重试。");
            completed=true;
            uint exit; if(!GetExitCodeThread(thread,out exit)||exit==0)throw new IOException("游戏未能加载连接模块。");
        } finally {
            if(thread!=IntPtr.Zero)CloseHandle(thread);
            if(remote!=IntPtr.Zero&&(completed||thread==IntPtr.Zero))VirtualFreeEx(p,remote,UIntPtr.Zero,0x8000);
            CloseHandle(p);
        }
    }
}

