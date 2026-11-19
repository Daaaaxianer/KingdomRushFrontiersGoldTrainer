using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Threading;

internal sealed class CompactSliderProbe : GoldSlider {
    internal void Down(int x){OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,x,20,0));}
    internal void DragTo(int x){OnMouseMove(new MouseEventArgs(MouseButtons.Left,0,x,20,0));}
    internal void Up(int x){OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,x,20,0));}
    internal void Key(Keys k){OnKeyDown(new KeyEventArgs(k));}
}
internal static class CompactTests {
    static void Assert(bool good,string name){if(!good)throw new Exception(name);}
    static object Field(object o,string n){return o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
    static void Event(Control c,string method,object value){typeof(SoftButton).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,new[]{value});}
    static void Borderless(SoftButton b){using(var bmp=new Bitmap(b.Width,b.Height)){
        b.DrawToBitmap(bmp,b.ClientRectangle);
        foreach(var point in new[]{new Point(0,0),new Point(b.Width-1,0),new Point(0,b.Height-1),new Point(b.Width-1,b.Height-1)})
            Assert(bmp.GetPixel(point.X,point.Y).ToArgb()==Theme.Paper.ToArgb(),"Unpainted/black corner pixel");
    }}
    [STAThread] static void Main(){try{
        Application.EnableVisualStyles();
        foreach(float scale in new[]{1f,1.25f,1.5f,1.75f,2f})using(var s=new CompactSliderProbe()){
            s.Size=new Size((int)(384*scale),(int)(44*scale));
            for(int i=1;i<=10;i++){int x=(int)Math.Round(s.XForValue(i));s.Down(x);Assert(s.Value==i,"Exact tick");s.Up(x);Assert(!s.Capture,"Pointer capture");}
            s.Down(0);s.DragTo(s.Width+80);Assert(s.Value==10,"Drag upper bound");s.Up(s.Width+80);s.DragTo(0);Assert(s.Value==10,"Drag remains after release");
            s.Down(0);s.Capture=false;s.DragTo(s.Width);Assert(s.Value==1,"Lost capture");
            s.Key(Keys.End);Assert(s.Value==10,"End");s.Key(Keys.Home);s.Key(Keys.Right);Assert(s.Value==2,"Keyboard");
        }
        Console.WriteLine("PASS discrete slider, drag release, lost capture, keyboard at 100/125/150/175/200% sizes");
        using(var b=new SoftButton{Size=new Size(160,48)}){
            int clicks=0;b.Click+=delegate{clicks++;};Borderless(b);
            Event(b,"OnMouseEnter",EventArgs.Empty);Borderless(b);
            Event(b,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,20,20,0));Borderless(b);
            Event(b,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,20,20,0));Assert(clicks==1,"Click repeated or missing");Borderless(b);
            Event(b,"OnMouseDown",new MouseEventArgs(MouseButtons.Left,1,20,20,0));
            Event(b,"OnMouseUp",new MouseEventArgs(MouseButtons.Left,1,300,20,0));Assert(clicks==1,"Release outside clicked");
            Event(b,"OnKeyDown",new KeyEventArgs(Keys.Space));Event(b,"OnKeyUp",new KeyEventArgs(Keys.Space));Assert(clicks==2,"Space activation");
            Event(b,"OnKeyDown",new KeyEventArgs(Keys.Enter));Assert(clicks==3,"Enter activation");
            b.Enabled=false;b.PerformClick();Assert(clicks==3,"Disabled activation");Borderless(b);
            b.Enabled=true;b.Primary=true;Borderless(b);
        }
        Console.WriteLine("PASS painted button has clean corners in idle/hover/pressed/disabled; single activation and keyboard");
        foreach(float scale in new[]{1f,1.25f,1.5f,2f})using(var form=new LiveTrainer(true)){
            form.Scale(new SizeF(scale,scale));form.PerformLayout();
            var slider=(GoldSlider)Field(form,"slider");var minus=(SoftButton)Field(form,"minus");var plus=(SoftButton)Field(form,"plus");
            for(int i=2;i<=10;i++){plus.PerformClick();Assert(slider.Value==i,"Plus step");}
            plus.PerformClick();Assert(slider.Value==10&&!plus.Enabled,"Plus upper bound");
            for(int i=9;i>=1;i--){minus.PerformClick();Assert(slider.Value==i,"Minus step");}
            minus.PerformClick();Assert(slider.Value==1&&!minus.Enabled,"Minus lower bound");
            foreach(Control c in form.Controls)Assert(form.ClientRectangle.Contains(c.Bounds),"Control clipped at scale "+scale);
        }
        Console.WriteLine("PASS +/- exact steps, disabled bounds and compact layout at four sizes");
        using(var gate=new ManualResetEventSlim(false)){
            string name="Local\\KRGoldCompactTest_"+Guid.NewGuid().ToString("N");
            using(var observer=MemoryMappedFile.CreateOrOpen(name,256))using(var read=observer.CreateViewAccessor())
            using(var form=new LiveTrainer(false,token=>{gate.Wait(token);var map=MemoryMappedFile.OpenExisting(name);var view=map.CreateViewAccessor();view.Write(4,2);return new ConnectionSession{Game=Process.GetCurrentProcess(),Map=map,View=view};})){
                int ticks=0;using(var timer=new System.Windows.Forms.Timer{Interval=30}){
                    timer.Tick+=delegate{ticks++;};timer.Start();
                    ((SoftButton)Field(form,"action")).PerformClick();((GoldSlider)Field(form,"slider")).Value=7;
                    var watch=Stopwatch.StartNew();while(watch.ElapsedMilliseconds<500){Application.DoEvents();Thread.Sleep(5);}
                    Assert(ticks>=3,"UI blocked");gate.Set();watch.Restart();
                    while(watch.ElapsedMilliseconds<3000&&Field(form,"session")==null){Application.DoEvents();Thread.Sleep(5);}
                    Assert(read.ReadInt32(0)==7,"Pending rate not applied");
                    var slider=(GoldSlider)Field(form,"slider");for(int i=1;i<=10;i++){slider.Value=i;Assert(read.ReadInt32(0)==i,"Rate write");}
                    ((SoftButton)Field(form,"reset")).PerformClick();Assert(read.ReadInt32(0)==1,"Reset");
                    ((SoftButton)Field(form,"action")).PerformClick();Assert(read.ReadInt32(0)==1&&read.ReadUInt64(8)==0,"Disconnect reset");
                }
            }
        }
        Console.WriteLine("PASS background connection, all ten rate writes, reset and disconnect in simulated session");
    }catch(Exception e){Console.WriteLine("FAIL "+e.GetType().Name+": "+e.Message);Environment.ExitCode=1;}}
}
