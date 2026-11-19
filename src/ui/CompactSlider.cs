using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
internal class GoldSlider : Control {
    int selected=1;bool dragging;
    internal event EventHandler ValueChanged;
    internal GoldSlider(){SetStyle(ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Selectable,true);
        BackColor=Color.White;TabStop=true;AccessibleName="金币倍率滑块";AccessibleRole=AccessibleRole.Slider;Cursor=Cursors.Hand;}
    internal int Value {get{return selected;}set{int next=Math.Max(1,Math.Min(10,value));if(next==selected)return;selected=next;Invalidate();
        AccessibilityNotifyClients(AccessibleEvents.ValueChange,-1);if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
    internal float Inset {get{return Math.Max(16f,Height*.4f);}}
    internal float XForValue(int number){return Inset+(Width-2*Inset)*(number-1)/9f;}
    internal int ValueAt(float x){return Math.Max(1,Math.Min(10,1+(int)Math.Round((x-Inset)*9/Math.Max(1f,Width-2*Inset),MidpointRounding.AwayFromZero)));}
    protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;
        float y=Height/2f,unit=Height/44f;
        using(var p=new Pen(Theme.Line,4*unit)){p.StartCap=p.EndCap=LineCap.Round;g.DrawLine(p,XForValue(1),y,XForValue(10),y);}
        if(Value>1)using(var p=new Pen(Theme.Blue,4*unit)){p.StartCap=p.EndCap=LineCap.Round;g.DrawLine(p,XForValue(1),y,XForValue(Value),y);}
        float x=XForValue(Value),r=8*unit;
        if(Focused&&ShowFocusCues)using(var b=new SolidBrush(Color.FromArgb(228,234,254)))g.FillEllipse(b,x-r-5*unit,y-r-5*unit,2*r+10*unit,2*r+10*unit);
        using(var b=new SolidBrush(Theme.Blue))g.FillEllipse(b,x-r,y-r,2*r,2*r);
    }
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;Focus();dragging=true;Capture=true;Value=ValueAt(e.X);}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(dragging&&Capture)Value=ValueAt(e.X);}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button!=MouseButtons.Left)return;if(dragging)Value=ValueAt(e.X);dragging=false;Capture=false;}
    protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)dragging=false;}
    protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);dragging=false;Capture=false;Invalidate();}
    protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
    protected override bool IsInputKey(Keys key){Keys k=key&Keys.KeyCode;return k==Keys.Left||k==Keys.Right||k==Keys.Up||k==Keys.Down||k==Keys.Home||k==Keys.End||base.IsInputKey(key);}
    protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Down)Value--;
        else if(e.KeyCode==Keys.Right||e.KeyCode==Keys.Up)Value++;else if(e.KeyCode==Keys.Home)Value=1;else if(e.KeyCode==Keys.End)Value=10;else return;e.Handled=true;e.SuppressKeyPress=true;}
    protected override AccessibleObject CreateAccessibilityInstance(){return new SliderAccess(this);}
    sealed class SliderAccess : ControlAccessibleObject {
        readonly GoldSlider owner;internal SliderAccess(GoldSlider s):base(s){owner=s;}
        public override string Value {get{return owner.Value.ToString();}set{int n;if(int.TryParse(value,out n)&&n>=1&&n<=10)owner.Value=n;}}
        public override AccessibleRole Role {get{return AccessibleRole.Slider;}}
    }
}

