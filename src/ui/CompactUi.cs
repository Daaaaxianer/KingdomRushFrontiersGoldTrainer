using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

internal sealed class LiveTrainer : Form {
    readonly GoldSlider slider=new GoldSlider();
    readonly Label number=new Label(),stateTitle=new Label(),stateDot=new Label();
    readonly SoftButton minus=new SoftButton(),plus=new SoftButton(),action=new SoftButton(),reset=new SoftButton();
    readonly ToolTip hints=new ToolTip{UseAnimation=false,UseFading=false,AutoPopDelay=15000};
    readonly System.Windows.Forms.Timer pulse=new System.Windows.Forms.Timer();
    readonly Func<CancellationToken,ConnectionSession> connectionFactory;
    ConnectionSession session;CancellationTokenSource cancellation;bool connecting,closeAfterConnect,preview;
    string stateDetail="";
    [STAThread] static void Main(){bool first;using(var m=new Mutex(true,"Local\\KRGoldLiveUI646",out first)){
        if(!first){MessageBox.Show("请先退出已打开的修改器，再启动此版本。","金币倍率");return;}
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new LiveTrainer());}}
    internal LiveTrainer():this(false){}
    internal LiveTrainer(bool previewOnly):this(previewOnly,ConnectionSession.Open){}
    internal LiveTrainer(bool previewOnly,Func<CancellationToken,ConnectionSession> factory){
        SuspendLayout();connectionFactory=factory;preview=previewOnly;Text="金币倍率 · 王国保卫战：前线";
        AutoScaleDimensions=new SizeF(96,96);AutoScaleMode=AutoScaleMode.Dpi;Font=new Font("Microsoft YaHei UI",10);
        ClientSize=new Size(440,340);ForeColor=Theme.Ink;BackColor=Theme.Paper;FormBorderStyle=FormBorderStyle.FixedSingle;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;
        using(var stream=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("AppIcon")){if(stream!=null)Icon=new Icon(stream);}
        LabelOn("金币倍率",28,24,220,32,16,Theme.Ink,true);
        stateDot.SetBounds(298,30,18,20);stateDot.Text="●";stateDot.Font=new Font("Segoe UI",8);Controls.Add(stateDot);
        stateTitle.SetBounds(318,27,100,25);stateTitle.Font=new Font(Font.FontFamily,10);stateTitle.Cursor=Cursors.Hand;stateTitle.Click+=delegate{
            if(!string.IsNullOrEmpty(stateDetail))MessageBox.Show(this,stateDetail,"连接状态",MessageBoxButtons.OK,MessageBoxIcon.Information);};Controls.Add(stateTitle);
        number.SetBounds(90,82,260,78);number.Font=new Font("Segoe UI",43,FontStyle.Regular);number.TextAlign=ContentAlignment.MiddleCenter;number.AccessibleName="所选金币倍率";Controls.Add(number);
        Setup(minus,"−",28,100,48,48,20,false);minus.Corner=24;minus.AccessibleName="降低一倍";minus.Click+=delegate{slider.Value--;};
        Setup(plus,"+",364,100,48,48,20,false);plus.Corner=24;plus.AccessibleName="提高一倍";plus.Click+=delegate{slider.Value++;};
        slider.SetBounds(28,176,384,44);slider.BackColor=Theme.Paper;slider.ValueChanged+=delegate{UpdateRate();};Controls.Add(slider);
        LabelOn("1",44,220,50,24,10,Theme.Muted,false);
        var max=LabelOn("10",346,220,50,24,10,Theme.Muted,false);max.TextAlign=ContentAlignment.TopRight;
        Setup(action,"连接游戏",28,268,240,44,11,true);action.Click+=delegate{if(session!=null)Detach("已断开连接","后续收入已恢复 1 倍。");else ConnectAsync();};
        Setup(reset,"恢复 1 倍",280,268,132,44,11,false);reset.Click+=delegate{slider.Value=1;UpdateRate();};
        minus.TabIndex=0;slider.TabIndex=1;plus.TabIndex=2;action.TabIndex=3;reset.TabIndex=4;
        hints.SetToolTip(minus,"降低一倍");hints.SetToolTip(plus,"提高一倍");hints.SetToolTip(slider,"点击或拖动选择 1–10 倍；也可用方向键调整。");
        hints.SetToolTip(reset,"恢复正常金币收入，保持游戏连接。");
        pulse.Interval=250;pulse.Tick+=delegate{RefreshSession();};pulse.Start();FormClosing+=OnClosing;Deactivate+=delegate{slider.Capture=false;};
        UpdateRate();SetState("未连接",preview?"界面预览":"从 Steam 启动游戏并进入关卡，再点击连接游戏。",Theme.Muted);action.Enabled=!preview;
        ResumeLayout(true);
    }
    Label LabelOn(string text,int x,int y,int w,int h,float size,Color color,bool bold){var l=new Label{Text=text,Font=new Font(Font.FontFamily,size,bold?FontStyle.Bold:FontStyle.Regular),ForeColor=color};l.SetBounds(x,y,w,h);Controls.Add(l);return l;}
    void Setup(SoftButton b,string text,int x,int y,int w,int h,float size,bool primary){b.Text=text;b.SetBounds(x,y,w,h);b.Font=new Font(Font.FontFamily,size,FontStyle.Regular);b.Primary=primary;Controls.Add(b);}
    void UpdateRate(){number.Text=slider.Value+"×";minus.Enabled=slider.Value>1;plus.Enabled=slider.Value<10;
        if(session!=null)try{session.View.Write(0,slider.Value);RefreshSession();}catch(Exception ex){Detach("连接已断开",ex.Message);}}
    void SetState(string title,string detail,Color dot){
        if(title.StartsWith("已连接"))title="已连接";
        else if(title=="尚未连接游戏")title="未连接";else if(title=="已断开连接"||title=="连接已断开")title="已断开";
        else if(title=="等待游戏就绪")title="等待就绪";else if(title=="未能连接游戏"||title=="连接未完成")title="连接失败";
        else if(title=="正在查找游戏")title="连接中";else if(title=="正在结束连接")title="正在关闭";
        stateTitle.Text=title;stateDot.ForeColor=dot;stateDetail=detail;hints.SetToolTip(stateTitle,detail);
    }
    async void ConnectAsync(){if(connecting||preview)return;connecting=true;action.Enabled=false;action.Text="连接中…";
        SetState("连接中","正在查找游戏并准备连接。",Theme.Blue);cancellation=new CancellationTokenSource();
        try{var token=cancellation.Token;var ready=await Task.Run(()=>connectionFactory(token));if(closeAfterConnect){ready.Dispose();return;}
            session=ready;session.View.Write(0,slider.Value);action.Text="断开连接";RefreshSession();
        }catch(OperationCanceledException){SetState("已取消","收入保持 1 倍。",Theme.Muted);}
        catch(Exception ex){if(session!=null){session.Dispose();session=null;}SetState("连接失败",ex.Message,Color.FromArgb(174,87,71));action.Text="重新连接";
            if(!closeAfterConnect)MessageBox.Show(this,ex.Message,"未能连接游戏",MessageBoxButtons.OK,MessageBoxIcon.Information);}
        finally{connecting=false;cancellation.Dispose();cancellation=null;action.Enabled=!preview;if(session==null&&action.Text=="连接中…")action.Text="连接游戏";if(closeAfterConnect)Close();}
    }
    void RefreshSession(){if(session==null)return;try{
        if(session.Game.HasExited){Detach("游戏已退出","下次启动游戏后重新连接即可。");return;}
        session.View.Write(8,Connector.GetTickCount64());int state=session.View.ReadInt32(4);
        if(state==2)SetState("已连接","Steam 原版已连接，当前为 "+slider.Value+" 倍。",Color.FromArgb(56,150,117));
        else if(state==1)SetState("等待就绪","请切回游戏并进入关卡。",Theme.Blue);
        else if(state==3){byte[] bytes=new byte[240];session.View.ReadArray(16,bytes,0,240);Detach("连接失败",System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0'));}
    }catch(Exception ex){Detach("已断开",ex.Message);}}
    void Detach(string title,string detail){if(session!=null){session.Dispose();session=null;}slider.Value=1;UpdateRate();action.Text="连接游戏";action.Enabled=!preview&&!connecting;SetState(title,detail,Theme.Muted);}
    void OnClosing(object sender,FormClosingEventArgs e){if(connecting){e.Cancel=true;closeAfterConnect=true;cancellation.Cancel();SetState("正在关闭","完成当前步骤后自动关闭。",Theme.Muted);return;}
        pulse.Stop();if(session!=null){session.Dispose();session=null;}}
    protected override void Dispose(bool disposing){if(disposing){pulse.Dispose();hints.Dispose();if(session!=null){session.Dispose();session=null;}}base.Dispose(disposing);}
}
