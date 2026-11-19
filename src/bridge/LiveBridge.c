#define _WIN32_WINNT 0x0601
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <tlhelp32.h>
#include <stdint.h>
#include <string.h>
#include <stddef.h>
#include "live_payload.h"

typedef struct { volatile LONG factor; volatile LONG status; volatile LONG64 heartbeat; char error[240]; } Shared;
typedef struct lua_State lua_State;
static Shared *shared;
static unsigned char *entry;
static const unsigned char expected[12]={0x48,0x8b,0x41,0x18,0x48,0x2b,0x41,0x10,0x48,0xc1,0xf8,0x03};
static int (*original_top)(lua_State*);
static void (*settop)(lua_State*,int);
static int (*loadbuffer)(lua_State*,const char*,size_t,const char*);
static int (*pcall_fn)(lua_State*,int,int,int);
static intptr_t (*tointeger)(lua_State*,int);
static const char* (*tostring_fn)(lua_State*,int,size_t*);
static void (*pushinteger)(lua_State*,intptr_t);
static void (*pushclosure)(lua_State*,int(*)(lua_State*),int);
static void (*setfield)(lua_State*,int,const char*);
static int (*checkstack)(lua_State*,int);
static volatile LONG busy;
static ULONGLONG next_poll;
static DWORD main_thread;

static int native_factor(lua_State *L) {
    LONG f=shared ? shared->factor : 1;
    ULONGLONG now=GetTickCount64(), beat=shared ? (ULONGLONG)InterlockedCompareExchange64(&shared->heartbeat,0,0) : 0;
    if(f<1 || f>10 || now<beat || now-beat>2500) f=1;
    pushinteger(L,f); return 1;
}

// Suspend only for the short instruction replacement. Refuse if an instruction
// pointer is in the overwritten prologue; never resume at a partial instruction.
static BOOL patch_bytes(const unsigned char *bytes) {
    HANDLE snapshot=CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD,0);
    if(snapshot==INVALID_HANDLE_VALUE) return FALSE;
    HANDLE held[512]; unsigned count=0; BOOL ok=TRUE;
    THREADENTRY32 t={0}; t.dwSize=sizeof(t);
    if(Thread32First(snapshot,&t)) do {
        if(t.th32OwnerProcessID!=GetCurrentProcessId() || t.th32ThreadID==GetCurrentThreadId()) continue;
        if(count==512) {ok=FALSE; break;}
        HANDLE h=OpenThread(THREAD_SUSPEND_RESUME|THREAD_GET_CONTEXT|THREAD_QUERY_INFORMATION,FALSE,t.th32ThreadID);
        if(!h) {ok=FALSE;break;}
        if(SuspendThread(h)==(DWORD)-1) {CloseHandle(h);ok=FALSE;break;}
        held[count++]=h;
        CONTEXT c={0}; c.ContextFlags=CONTEXT_CONTROL;
        if(!GetThreadContext(h,&c) || (c.Rip>=(DWORD64)entry && c.Rip<(DWORD64)entry+12)) {ok=FALSE;break;}
    } while(Thread32Next(snapshot,&t));
    if(ok) {
        DWORD old;
        if(VirtualProtect(entry,12,PAGE_EXECUTE_READWRITE,&old)) {
            memcpy(entry,bytes,12); FlushInstructionCache(GetCurrentProcess(),entry,12);
            DWORD ignored; VirtualProtect(entry,12,old,&ignored);
        } else ok=FALSE;
    }
    for(unsigned i=0;i<count;i++){ResumeThread(held[i]);CloseHandle(held[i]);}
    CloseHandle(snapshot); return ok;
}

static int intercepted_top(lua_State *L) {
    if(shared && shared->status==1 && GetCurrentThreadId()==main_thread && GetTickCount64()>=next_poll && InterlockedCompareExchange(&busy,1,0)==0) {
        next_poll=GetTickCount64()+250;
        int saved=original_top(L);
        if(checkstack(L,64)) {
            pushclosure(L,native_factor,0); setfield(L,-10002,"__KR_GOLD_NATIVE_GET");
            int err=loadbuffer(L,(const char*)payload,sizeof(payload)-1,"@KR_Gold_Live646");
            if(!err) err=pcall_fn(L,0,1,0);
            if(err) {
                const char *msg=tostring_fn(L,-1,NULL);
                if(msg) lstrcpynA(shared->error,msg,239);
                shared->status=3;
            } else if(tointeger(L,-1)==1) shared->status=2;
            settop(L,saved);
        }
        // After successful attachment no native API interception remains.
        if(shared->status!=1) patch_bytes(expected);
        InterlockedExchange(&busy,0);
    }
    return original_top(L);
}

static DWORD find_main_thread(void) {
    DWORD best=0; ULONGLONG oldest=~(ULONGLONG)0;
    HANDLE snap=CreateToolhelp32Snapshot(TH32CS_SNAPTHREAD,0);
    THREADENTRY32 t={0}; t.dwSize=sizeof(t);
    if(snap!=INVALID_HANDLE_VALUE && Thread32First(snap,&t)) do {
        if(t.th32OwnerProcessID!=GetCurrentProcessId() || t.th32ThreadID==GetCurrentThreadId()) continue;
        HANDLE h=OpenThread(THREAD_QUERY_INFORMATION,FALSE,t.th32ThreadID);
        FILETIME creation,exit,kernel,user;
        if(h && GetThreadTimes(h,&creation,&exit,&kernel,&user)) {
            ULONGLONG n=((ULONGLONG)creation.dwHighDateTime<<32)|creation.dwLowDateTime;
            if(n<oldest){oldest=n;best=t.th32ThreadID;}
        }
        if(h)CloseHandle(h);
    } while(Thread32Next(snap,&t));
    if(snap!=INVALID_HANDLE_VALUE)CloseHandle(snap);
    return best;
}

static DWORD WINAPI initialize(void *unused) {
    (void)unused;
    wchar_t name[96]; wsprintfW(name,L"Local\\KRFLocalGoldLive_%lu",GetCurrentProcessId());
    HANDLE mapping=OpenFileMappingW(FILE_MAP_ALL_ACCESS,FALSE,name);
    if(!mapping)return 0;
    shared=(Shared*)MapViewOfFile(mapping,FILE_MAP_ALL_ACCESS,0,0,sizeof(Shared));
    CloseHandle(mapping); if(!shared)return 0;
    HMODULE lua=GetModuleHandleW(L"lua51.dll");
    if(!lua){strcpy(shared->error,"lua51.dll not loaded");shared->status=3;return 0;}
#define API(var,name) *(FARPROC*)&var=GetProcAddress(lua,name); if(!var){strcpy(shared->error,name " unavailable");shared->status=3;return 0;}
    API(entry,"lua_gettop"); API(settop,"lua_settop"); API(loadbuffer,"luaL_loadbuffer");
    API(pcall_fn,"lua_pcall"); API(tointeger,"lua_tointeger"); API(tostring_fn,"lua_tolstring");
    API(pushinteger,"lua_pushinteger"); API(pushclosure,"lua_pushcclosure"); API(setfield,"lua_setfield"); API(checkstack,"lua_checkstack");
    if(memcmp(entry,expected,12)!=0){strcpy(shared->error,"Unsupported lua_gettop prologue; no patch applied");shared->status=3;return 0;}
    unsigned char *trampoline=VirtualAlloc(NULL,32,MEM_RESERVE|MEM_COMMIT,PAGE_READWRITE);
    if(!trampoline){shared->status=3;return 0;}
    memcpy(trampoline,expected,12); trampoline[12]=0xc3;
    DWORD old; VirtualProtect(trampoline,32,PAGE_EXECUTE_READ,&old);
    original_top=(void*)trampoline;
    main_thread=find_main_thread();
    if(!main_thread){shared->status=3;return 0;}
    unsigned char jump[12]={0x48,0xb8};
    uint64_t dest=(uint64_t)(uintptr_t)intercepted_top;
    memcpy(jump+2,&dest,8);jump[10]=0xff;jump[11]=0xe0;
    for(int i=0;i<40;i++) {if(patch_bytes(jump)) return 0; Sleep(10);}
    strcpy(shared->error,"Could not safely install callback; no patch applied");shared->status=3;
    return 0;
}

BOOL WINAPI DllMain(HINSTANCE instance,DWORD reason,LPVOID reserved) {
    (void)reserved;
    if(reason==DLL_PROCESS_ATTACH){DisableThreadLibraryCalls(instance);HANDLE t=CreateThread(NULL,0,initialize,NULL,0,NULL);if(t)CloseHandle(t);}
    return TRUE;
}
