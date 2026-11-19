using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

internal static class Theme {
    internal static readonly Color Paper=Color.FromArgb(249,250,253),Ink=Color.FromArgb(36,44,62),
        Blue=Color.FromArgb(83,106,202),Muted=Color.FromArgb(116,126,146),Line=Color.FromArgb(222,228,239),
        Soft=Color.FromArgb(235,239,248);
    internal static GraphicsPath Round(RectangleF r,float radius){float d=radius*2;var p=new GraphicsPath();
        p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
}

// A painted Control has no native BUTTON window, default-button frame or themed
// nonclient border. All corner pixels are explicitly cleared to the parent fill.
internal sealed class SoftButton : Control {
    bool hover,pressed,spaceDown;
    internal bool Primary;
    internal int Corner=12;
    internal SoftButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Selectable|ControlStyles.Opaque,true);
        SetStyle(ControlStyles.StandardClick|ControlStyles.StandardDoubleClick,false);TabStop=true;Cursor=Cursors.Hand;AccessibleRole=AccessibleRole.PushButton;
        Font=new Font("Microsoft YaHei UI",10,FontStyle.Bold);BackColor=Theme.Paper;}
    protected override void OnPaintBackground(PaintEventArgs e){}
    protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.Clear(Parent==null?Theme.Paper:Parent.BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;
        Color fill=Primary?Theme.Blue:Theme.Soft,fore=Primary?Color.White:Theme.Ink;
        if(!Enabled){fill=Theme.Soft;fore=Color.FromArgb(168,177,194);}
        else if(pressed)fill=Primary?Color.FromArgb(68,88,179):Color.FromArgb(213,222,241);
        else if(hover)fill=Primary?Color.FromArgb(99,121,215):Color.FromArgb(225,232,246);
        float scale=Height/44f;
        using(var p=Theme.Round(new RectangleF(1,1,Width-2,Height-2),Math.Min(Corner*scale,(Height-2)/2f)))using(var b=new SolidBrush(fill))g.FillPath(b,p);
        TextRenderer.DrawText(g,Text,Font,ClientRectangle,fore,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.NoPadding);
        if(Focused&&ShowFocusCues)using(var p=Theme.Round(new RectangleF(4,4,Width-8,Height-8),Math.Max(3,Corner*scale-3)))using(var pen=new Pen(Primary?Color.FromArgb(190,204,255):Theme.Blue,1.5f))g.DrawPath(pen,p);
    }
    internal void PerformClick(){if(Enabled){OnClick(EventArgs.Empty);AccessibilityNotifyClients(AccessibleEvents.StateChange,-1);}}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left||!Enabled)return;Focus();pressed=true;Capture=true;Invalidate();}
    protected override void OnMouseUp(MouseEventArgs e){bool click=Enabled&&pressed&&e.Button==MouseButtons.Left&&ClientRectangle.Contains(e.Location);
        pressed=false;Capture=false;Invalidate();base.OnMouseUp(e);if(click)PerformClick();}
    protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture){pressed=false;Invalidate();}}
    protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hover=true;Invalidate();}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hover=false;Invalidate();}
    protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
    protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);pressed=false;spaceDown=false;Capture=false;Invalidate();}
    protected override void OnEnabledChanged(EventArgs e){base.OnEnabledChanged(e);if(!Enabled){pressed=false;spaceDown=false;Capture=false;}Invalidate();}
    protected override bool IsInputKey(Keys k){return k==Keys.Space||k==Keys.Enter||base.IsInputKey(k);}
    protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(!Enabled)return;
        if(e.KeyCode==Keys.Space){spaceDown=true;pressed=true;Invalidate();e.Handled=e.SuppressKeyPress=true;}
        else if(e.KeyCode==Keys.Enter){PerformClick();e.Handled=e.SuppressKeyPress=true;}}
    protected override void OnKeyUp(KeyEventArgs e){base.OnKeyUp(e);if(e.KeyCode==Keys.Space&&spaceDown){spaceDown=false;pressed=false;Invalidate();PerformClick();e.Handled=e.SuppressKeyPress=true;}}
    protected override AccessibleObject CreateAccessibilityInstance(){return new ButtonAccess(this);}
    sealed class ButtonAccess : ControlAccessibleObject {
        readonly SoftButton owner;internal ButtonAccess(SoftButton b):base(b){owner=b;}
        public override AccessibleRole Role{get{return AccessibleRole.PushButton;}}
        public override string DefaultAction{get{return "按下";}}
        public override void DoDefaultAction(){owner.PerformClick();}
    }
}
