using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace WindowsTimer {
 public class Schedule {
  public DateTime? Due; public string Action; public bool Warned;
  public void Start(DateTime now, DateTime due, string action) {
   if (due <= now) throw new ArgumentException("현재보다 뒤의 시간을 선택해 주세요.");
   Due = due; Action = action; Warned = false;
  }
  public void Cancel() { Due = null; Warned = false; }
  public void Extend() { if (Due.HasValue) { Due = Due.Value.AddMinutes(10); Warned = false; } }
  public int Tick(DateTime now, bool resumed) {
   if (!Due.HasValue) return 0;
   if (resumed && Due.Value <= now.AddSeconds(60)) { Due = now.AddSeconds(60); Warned = false; }
   if (!Warned && (Due.Value-now).TotalSeconds <= 60) { Warned = true; if(Due.Value <= now) Due = now.AddSeconds(60); return 1; }
   if (Warned && Due.Value <= now) return 2;
   return 0;
  }
 }

 static class Palette {
  public static readonly Color Canvas=Color.FromArgb(246,242,235), Paper=Color.FromArgb(255,253,249), Cream=Color.FromArgb(236,226,212);
  public static readonly Color Brown=Color.FromArgb(54,44,37), Deep=Color.FromArgb(43,35,30), Ink=Color.FromArgb(55,47,42);
  public static readonly Color Muted=Color.FromArgb(137,126,115), Clay=Color.FromArgb(178,91,58), ClayDark=Color.FromArgb(147,68,42);
  public static readonly Color Line=Color.FromArgb(229,220,208), Green=Color.FromArgb(94,132,101), White=Color.White;
 }

 static class UIFont {
  static PrivateFontCollection collection; static IntPtr fontMemory=IntPtr.Zero; static FontFamily family;
  public static FontFamily Family { get { if (family==null) Load(); return family; } }
  static void Load() {
   try {
    Assembly a=Assembly.GetExecutingAssembly(); using(Stream s=a.GetManifestResourceStream("WindowsTimer.fonts.NotoSansKR-VF.ttf")) {
     if(s==null) throw new FileNotFoundException("Noto Sans KR resource missing"); byte[] b=new byte[s.Length]; s.Read(b,0,b.Length);
     fontMemory=Marshal.AllocHGlobal(b.Length); Marshal.Copy(b,0,fontMemory,b.Length); collection=new PrivateFontCollection(); collection.AddMemoryFont(fontMemory,b.Length);
     if(collection.Families.Length>0) family=collection.Families[0];
    }
   } catch { family=new FontFamily("Malgun Gothic"); }
  }
  public static Font Make(float size, FontStyle style) { return new Font(Family,size,style,GraphicsUnit.Point); }
  public static void Cleanup() { if(collection!=null) collection.Dispose(); if(fontMemory!=IntPtr.Zero){Marshal.FreeHGlobal(fontMemory);fontMemory=IntPtr.Zero;} }
 }

 enum ButtonTone { Soft, Selected, Primary, Quiet, Step }
 sealed class SoftButton : Button {
  public ButtonTone Tone=ButtonTone.Soft; bool selected; bool over,down;
  public bool Selected { get { return selected; } set { if(selected==value)return; selected=value; Invalidate(); } }
  public SoftButton(string text, ButtonTone tone){Text=text;Tone=tone;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;UseVisualStyleBackColor=false;BackColor=Color.Transparent;Cursor=Cursors.Hand;TabStop=true;Font=UIFont.Make(11,FontStyle.Regular);AccessibleName=text;UseCompatibleTextRendering=true;}
  protected override void OnMouseEnter(EventArgs e){over=true;Invalidate();base.OnMouseEnter(e);} protected override void OnMouseLeave(EventArgs e){over=false;down=false;Invalidate();base.OnMouseLeave(e);}
  protected override void OnMouseDown(MouseEventArgs e){if(e.Button==MouseButtons.Left)down=true;Invalidate();base.OnMouseDown(e);} protected override void OnMouseUp(MouseEventArgs e){down=false;Invalidate();base.OnMouseUp(e);}
  protected override void OnPaint(PaintEventArgs e){Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;g.Clear(Parent==null?Palette.Canvas:Parent.BackColor);Rectangle r=ClientRectangle;r.Inflate(-1,-2);int radius=Math.Min(15,r.Height/2);
   Color top,bottom,fg,border=Palette.Line; bool primary=Tone==ButtonTone.Primary;
   if(Tone==ButtonTone.Selected || (Tone==ButtonTone.Soft&&Selected)){top=Color.FromArgb(190,111,75);bottom=Palette.ClayDark;fg=Color.White;border=top;}
   else if(primary){top=over?Color.FromArgb(196,112,74):Palette.Clay;bottom=over?Palette.Clay:Palette.ClayDark;fg=Color.White;border=bottom;}
   else if(Tone==ButtonTone.Step){top=Color.FromArgb(255,254,251);bottom=Color.FromArgb(242,236,227);fg=Palette.Brown;border=Palette.Line;}
   else if(Tone==ButtonTone.Quiet){top=over?Palette.Cream:Color.Transparent;bottom=top;fg=Palette.Brown;border=Color.Transparent;}
   else {top=over?Color.FromArgb(244,235,223):Color.FromArgb(248,244,237);bottom=Color.FromArgb(241,234,224);fg=Palette.Brown;border=Palette.Line;}
   if(!Enabled){fg=Color.FromArgb(156,148,139);top=Color.FromArgb(243,239,232);bottom=top;border=Color.FromArgb(235,229,221);}
   if(primary&&!down){using(SolidBrush shadow=new SolidBrush(Color.FromArgb(38,82,50,37))){using(GraphicsPath sp=Shape.RoundRect(new Rectangle(r.X,r.Y+4,r.Width,r.Height),radius))g.FillPath(shadow,sp);}}
   using(GraphicsPath p=Shape.RoundRect(r,radius)) {using(LinearGradientBrush b=new LinearGradientBrush(r,top,bottom,90f))g.FillPath(b,p);using(Pen pen=new Pen(border,1))g.DrawPath(pen,p);}
   Rectangle textRect=r;if(down)textRect.Offset(0,1);using(Font f=UIFont.Make(11,Selected||primary?FontStyle.Bold:FontStyle.Regular))using(SolidBrush brush=new SolidBrush(fg))using(StringFormat sf=new StringFormat()){sf.Alignment=StringAlignment.Center;sf.LineAlignment=StringAlignment.Center;sf.Trimming=StringTrimming.EllipsisCharacter;g.DrawString(Text,f,brush,textRect,sf);}
   if(Focused&&ShowFocusCues){Rectangle focus=r;focus.Inflate(-5,-5);using(Pen p=new Pen(Color.FromArgb(130,Palette.Brown))){p.DashPattern=new float[]{2,2};using(GraphicsPath path=Shape.RoundRect(focus,Math.Max(4,radius-4)))g.DrawPath(p,path);}}
  }
 }

 static class Shape {
  public static GraphicsPath RoundRect(Rectangle r,int radius){GraphicsPath p=new GraphicsPath();int d=Math.Max(2,radius*2);p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 }

 sealed class CardPanel : Panel {
  public Color PanelTop=Palette.Paper, PanelBottom=Palette.Paper; public int Radius=24;
  public CardPanel(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);BackColor=Palette.Paper;}
  protected override void OnPaintBackground(PaintEventArgs e){Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Palette.Canvas);Rectangle r=ClientRectangle;r.Inflate(-5,-6);for(int i=5;i>=1;i--){Rectangle sh=r;sh.Offset(0,i);sh.Inflate(i,i);using(SolidBrush b=new SolidBrush(Color.FromArgb(3+i*2,75,54,41)))using(GraphicsPath p=Shape.RoundRect(sh,Radius+i))g.FillPath(b,p);}using(GraphicsPath p=Shape.RoundRect(r,Radius))using(LinearGradientBrush b=new LinearGradientBrush(r,PanelTop,PanelBottom,90f))g.FillPath(b,p);using(GraphicsPath p=Shape.RoundRect(r,Radius))using(Pen pen=new Pen(Color.FromArgb(218,208,194),1))g.DrawPath(pen,p);}
 }

 sealed class HeroPanel : Panel {
  Image hero;
  public HeroPanel(Image image){hero=image;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);BackColor=Palette.Cream;}
  protected override void OnPaintBackground(PaintEventArgs e){Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Palette.Canvas);Rectangle r=ClientRectangle;r.Inflate(-5,-6);for(int i=5;i>=1;i--){Rectangle sh=r;sh.Offset(0,i);sh.Inflate(i,i);using(SolidBrush b=new SolidBrush(Color.FromArgb(3+i*2,75,54,41)))using(GraphicsPath p=Shape.RoundRect(sh,28+i))g.FillPath(b,p);}using(GraphicsPath p=Shape.RoundRect(r,28))using(LinearGradientBrush b=new LinearGradientBrush(r,Color.FromArgb(242,234,222),Color.FromArgb(224,210,192),90f))g.FillPath(b,p);
   using(SolidBrush glow=new SolidBrush(Color.FromArgb(28,255,255,255)))g.FillEllipse(glow,new Rectangle(78,30,390,390));using(Pen ring=new Pen(Color.FromArgb(55,255,255,255),1))g.DrawEllipse(ring,new Rectangle(91,38,366,366));
   if(hero!=null){int side=Math.Min(Width-74,Height-104);Rectangle dst=new Rectangle((Width-side)/2,25,side,side);g.DrawImage(hero,dst);}
   using(SolidBrush pill=new SolidBrush(Color.FromArgb(220,255,252,247)))using(GraphicsPath p=Shape.RoundRect(new Rectangle(23,22,120,27),13))g.FillPath(pill,p);
   using(Font f=UIFont.Make(8,FontStyle.Bold))using(SolidBrush b=new SolidBrush(Palette.ClayDark))using(StringFormat sf=new StringFormat()){sf.Alignment=StringAlignment.Center;sf.LineAlignment=StringAlignment.Center;g.DrawString("CALM  /  01",f,b,new Rectangle(23,22,120,27),sf);}
   using(SolidBrush fade=new SolidBrush(Color.FromArgb(238,242,234,223)))g.FillRectangle(fade,18,Height-68,Width-36,49);
   using(Font f=UIFont.Make(13,FontStyle.Bold))using(SolidBrush b=new SolidBrush(Palette.Brown))g.DrawString("차분한 하루의 마무리",f,b,new RectangleF(31,Height-64,Width-62,22));
   using(Font f=UIFont.Make(9,FontStyle.Regular))using(SolidBrush b=new SolidBrush(Palette.Muted))g.DrawString("원하는 순간을 정하면 마지막 60초를 알려드려요",f,b,new RectangleF(31,Height-40,Width-62,18));
  }
 }

 sealed class StatusBadge : Control {
  public string StatusText="예약 대기 중"; public bool Active;
  public StatusBadge(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);AccessibleRole=AccessibleRole.StatusBar;}
  protected override void OnPaint(PaintEventArgs e){Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;Rectangle r=ClientRectangle;r.Inflate(-1,-1);using(GraphicsPath p=Shape.RoundRect(r,18))using(SolidBrush b=new SolidBrush(Active?Color.FromArgb(250,235,225):Color.FromArgb(238,242,234)))g.FillPath(b,p);using(SolidBrush dot=new SolidBrush(Active?Palette.Clay:Palette.Green))g.FillEllipse(dot,16,Height/2-4,8,8);using(Font f=UIFont.Make(9,FontStyle.Bold))using(SolidBrush b=new SolidBrush(Active?Palette.ClayDark:Palette.Green))using(StringFormat sf=new StringFormat()){sf.Alignment=StringAlignment.Near;sf.LineAlignment=StringAlignment.Center;g.DrawString(StatusText,f,b,new RectangleF(31,0,Width-38,Height),sf);}}
 }

 public class MainForm : Form {
  readonly Schedule job=new Schedule(); readonly bool demo;
  readonly Color ink=Palette.Ink, muted=Palette.Muted;
  string selectedAction="종료"; bool relative=true, exiting; int minutes=30;
  Label headline,subhead,number,valueHint,dueLabel,safetyLabel,footer; StatusBadge badge;
  SoftButton shutdownButton,restartButton,relativeButton,dateButton,minusButton,plusButton,p10,p30,p60,p120,mainButton,cancelButton,extendButton,infoButton;
  DateTimePicker datePicker; HeroPanel heroPanel; Image heroImage; NotifyIcon tray; System.Windows.Forms.Timer timer; DateTime lastTick=DateTime.Now;
  public MainForm(bool demoMode) {
   demo=demoMode; Text="윈도우 타이머"+(demo?" · 안전 데모":""); ClientSize=new Size(1220,790);MinimumSize=new Size(1236,830);MaximizeBox=false;FormBorderStyle=FormBorderStyle.FixedSingle;StartPosition=FormStartPosition.CenterScreen;AutoScaleMode=AutoScaleMode.Dpi;BackColor=Palette.Canvas;ForeColor=Palette.Ink;Font=UIFont.Make(10,FontStyle.Regular);DoubleBuffered=true;
   Icon appIcon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);Icon=appIcon??SystemIcons.Application;
   LoadHero(); BuildShell(); BuildSettings();
   tray=new NotifyIcon{Icon=Icon,Text="윈도우 타이머 · 예약 대기 중",Visible=true};ContextMenuStrip menu=new ContextMenuStrip();menu.Items.Add("창 열기",null,delegate{Reveal();});menu.Items.Add("예약 취소",null,delegate{job.Cancel();RefreshView("예약을 취소했어요");});menu.Items.Add("앱 종료",null,delegate{ExitApp();});tray.ContextMenuStrip=menu;tray.DoubleClick+=delegate{Reveal();};tray.BalloonTipClicked+=delegate{Reveal();};
   FormClosing+=delegate(object s,FormClosingEventArgs e){if(!exiting&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();tray.ShowBalloonTip(2500,"윈도우 타이머",job.Due.HasValue?"예약을 유지하고 있어요. 트레이 메뉴에서 취소할 수 있어요.":"트레이 아이콘에서 다시 열거나 종료할 수 있어요.",ToolTipIcon.Info);}};
   FormClosed+=delegate{if(timer!=null)timer.Stop();if(tray!=null){tray.Visible=false;tray.Dispose();}if(heroImage!=null)heroImage.Dispose();};
   timer=new System.Windows.Forms.Timer{Interval=250};timer.Tick+=delegate{TickJob();};timer.Start();RefreshView(null);
  }
  void LoadHero(){try{using(Stream s=Assembly.GetExecutingAssembly().GetManifestResourceStream("WindowsTimer.assets.hero-timer.png")){if(s!=null)using(Image i=Image.FromStream(s))heroImage=new Bitmap(i);}}catch{heroImage=null;}}
  Label AddLabel(Control parent,string text,int x,int y,int w,int h,float size,Color color,FontStyle style){Label l=new Label{Text=text,Location=new Point(x,y),Size=new Size(w,h),BackColor=Color.Transparent,ForeColor=color,Font=UIFont.Make(size,style),UseCompatibleTextRendering=true,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};parent.Controls.Add(l);return l;}
  SoftButton AddButton(Control parent,string text,int x,int y,int w,int h,ButtonTone tone,EventHandler click){SoftButton b=new SoftButton(text,tone){Location=new Point(x,y),Size=new Size(w,h)};if(click!=null)b.Click+=click;parent.Controls.Add(b);return b;}
  void BuildShell(){
   Panel rail=new Panel{Location=new Point(0,0),Size=new Size(88,ClientSize.Height),BackColor=Palette.Deep,Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left};rail.Paint+=delegate(object s,PaintEventArgs e){Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using(LinearGradientBrush bg=new LinearGradientBrush(rail.ClientRectangle,Palette.Brown,Palette.Deep,90))g.FillRectangle(bg,rail.ClientRectangle);using(SolidBrush mark=new SolidBrush(Palette.Clay))using(GraphicsPath p=Shape.RoundRect(new Rectangle(20,23,48,48),16))g.FillPath(mark,p);using(Pen ring=new Pen(Color.FromArgb(250,248,241),2.3f))g.DrawEllipse(ring,33,36,22,22);using(Pen hand=new Pen(Color.FromArgb(250,248,241),2.3f)){g.DrawLine(hand,44,47,44,40);g.DrawLine(hand,44,47,50,50);}using(SolidBrush indicator=new SolidBrush(Color.FromArgb(117,241,205)))g.FillEllipse(indicator,58,60,6,6);};Controls.Add(rail);
   Panel nav=new Panel{Location=new Point(13,124),Size=new Size(62,62),BackColor=Color.Transparent};nav.Paint+=delegate(object s,PaintEventArgs e){Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using(GraphicsPath p=Shape.RoundRect(new Rectangle(0,0,62,62),16))using(SolidBrush b=new SolidBrush(Color.FromArgb(64,Palette.Clay)))g.FillPath(b,p);using(Pen p=new Pen(Color.FromArgb(255,218,194),2f)){g.DrawEllipse(p,20,12,22,22);g.DrawLine(p,31,17,31,23);g.DrawLine(p,31,23,36,26);}using(Font f=UIFont.Make(8,FontStyle.Bold))using(SolidBrush b=new SolidBrush(Color.FromArgb(250,232,218)))using(StringFormat sf=new StringFormat()){sf.Alignment=StringAlignment.Center;sf.LineAlignment=StringAlignment.Far;g.DrawString("예약",f,b,new RectangleF(0,0,62,59),sf);}};rail.Controls.Add(nav);
   infoButton=AddButton(rail,"정보",18,ClientSize.Height-79,52,44,ButtonTone.Quiet,delegate{ShowAbout();});infoButton.Anchor=AnchorStyles.Bottom|AnchorStyles.Left;infoButton.ForeColor=Color.FromArgb(240,225,211);infoButton.Font=UIFont.Make(9,FontStyle.Regular);
   AddLabel(rail,"v1.0.1",15,ClientSize.Height-31,58,18,7,Color.FromArgb(157,144,132),FontStyle.Regular).Anchor=AnchorStyles.Bottom|AnchorStyles.Left;
   AddLabel(this,"윈도우 타이머  /  예약 관리",120,24,560,18,9,Palette.ClayDark,FontStyle.Bold);
   headline=AddLabel(this,"하루의 끝을, 차분하게.",120,45,630,43,26,Palette.Brown,FontStyle.Bold);
   subhead=AddLabel(this,"원하는 시간을 예약하고 편안하게 마무리하세요.",122,91,660,25,11,Palette.Muted,FontStyle.Regular);
   badge=new StatusBadge{Location=new Point(950,48),Size=new Size(230,38),StatusText="예약 대기 중"};Controls.Add(badge);
   heroPanel=new HeroPanel(heroImage){Location=new Point(116,144),Size=new Size(556,548)};Controls.Add(heroPanel);
   footer=AddLabel(this,"앱이 실행되는 동안 예약이 유지돼요. 절전에서 깨어나면 실행 전에 60초 더 기다려요.",122,712,710,26,9,Palette.Muted,FontStyle.Regular);
  }
  void BuildSettings(){
   CardPanel card=new CardPanel{Location=new Point(695,144),Size=new Size(485,548),Radius=23};Controls.Add(card);
   AddLabel(card,"01  /  작업 선택",28,18,420,18,8,Palette.ClayDark,FontStyle.Bold);
   AddLabel(card,"무엇을 할까요?",27,37,420,34,17,Palette.Brown,FontStyle.Bold);
   shutdownButton=AddButton(card,"종료",28,83,198,50,ButtonTone.Soft,delegate{selectedAction="종료";RefreshView(null);});
   restartButton=AddButton(card,"재부팅",239,83,218,50,ButtonTone.Soft,delegate{selectedAction="재부팅";RefreshView(null);});
   Panel rule=new Panel{Location=new Point(28,150),Size=new Size(429,1),BackColor=Palette.Line};card.Controls.Add(rule);
   AddLabel(card,"02  /  시간 설정",28,166,420,18,8,Palette.ClayDark,FontStyle.Bold);
   AddLabel(card,"언제 마칠까요?",27,186,420,34,17,Palette.Brown,FontStyle.Bold);
   relativeButton=AddButton(card,"분 후",28,229,198,39,ButtonTone.Soft,delegate{relative=true;RefreshView(null);});
   dateButton=AddButton(card,"날짜 · 시간",239,229,218,39,ButtonTone.Soft,delegate{relative=false;RefreshView(null);});
   minusButton=AddButton(card,"−",44,290,48,48,ButtonTone.Step,delegate{if(minutes>1)minutes--;RefreshView(null);});
   number=AddLabel(card,"30",102,275,280,65,48,Palette.Brown,FontStyle.Bold);number.TextAlign=ContentAlignment.MiddleCenter;
   plusButton=AddButton(card,"+",389,290,48,48,ButtonTone.Step,delegate{if(minutes<10080)minutes++;RefreshView(null);});
   valueHint=AddLabel(card,"분 뒤 실행",80,339,335,25,10,Palette.Muted,FontStyle.Regular);valueHint.TextAlign=ContentAlignment.MiddleCenter;
   datePicker=new DateTimePicker{Location=new Point(29,291),Size=new Size(427,42),Format=DateTimePickerFormat.Custom,CustomFormat="yyyy년 MM월 dd일   HH:mm",Value=DateTime.Now.AddHours(1),MinDate=DateTime.Now.AddMinutes(-1),MaxDate=DateTime.Now.AddDays(7),Font=UIFont.Make(11,FontStyle.Regular),Visible=false};card.Controls.Add(datePicker);
   p10=AddButton(card,"10분",27,379,96,39,ButtonTone.Soft,delegate{minutes=10;relative=true;RefreshView(null);});
   p30=AddButton(card,"30분",130,379,96,39,ButtonTone.Soft,delegate{minutes=30;relative=true;RefreshView(null);});
   p60=AddButton(card,"1시간",233,379,96,39,ButtonTone.Soft,delegate{minutes=60;relative=true;RefreshView(null);});
   p120=AddButton(card,"2시간",336,379,96,39,ButtonTone.Soft,delegate{minutes=120;relative=true;RefreshView(null);});
   mainButton=AddButton(card,"예약 시작",28,426,429,58,ButtonTone.Primary,delegate{StartJob();});
   cancelButton=AddButton(card,"예약 취소",28,426,205,58,ButtonTone.Soft,delegate{job.Cancel();RefreshView("예약을 취소했어요");});
   extendButton=AddButton(card,"10분 연장",245,426,212,58,ButtonTone.Primary,delegate{job.Extend();RefreshView("예약을 10분 연장했어요");});
   dueLabel=AddLabel(card,"실행 전에 60초 안내를 표시해요.",29,489,428,23,9,Palette.Muted,FontStyle.Regular);
   safetyLabel=AddLabel(card,"저장하지 않은 파일이 있으면 Windows가 종료를 막을 수 있어요.",29,514,428,28,8,Palette.Muted,FontStyle.Regular);
  }
  void RefreshView(string message){bool active=job.Due.HasValue;shutdownButton.Selected=selectedAction=="종료";restartButton.Selected=selectedAction=="재부팅";relativeButton.Selected=relative;dateButton.Selected=!relative;
   shutdownButton.Enabled=restartButton.Enabled=relativeButton.Enabled=dateButton.Enabled=minusButton.Enabled=plusButton.Enabled=p10.Enabled=p30.Enabled=p60.Enabled=p120.Enabled=!active;
   minusButton.Visible=plusButton.Visible=number.Visible=valueHint.Visible=p10.Visible=p30.Visible=p60.Visible=p120.Visible=relative&&!active;
   datePicker.Visible=!relative&&!active;
   mainButton.Visible=!active;cancelButton.Visible=extendButton.Visible=active;
   if(active){badge.Active=true;badge.StatusText="예약됨 · Windows "+job.Action;number.Text=FormatTime(job.Due.Value-DateTime.Now);valueHint.Text="남은 시간";datePicker.Visible=false;p10.Visible=p30.Visible=p60.Visible=p120.Visible=false;minusButton.Visible=plusButton.Visible=false;dueLabel.Text=job.Due.Value.ToString("yyyy년 MM월 dd일  HH:mm")+" 실행 예정";}
   else {badge.Active=false;badge.StatusText=message??"예약 대기 중";number.Text=minutes.ToString("00");valueHint.Text=relative?"분 뒤 실행":"선택한 날짜와 시간에 실행";dueLabel.Text=message??(relative?"분 단위로 빠르게 예약할 수 있어요.":"오늘부터 7일 안의 시간을 지정할 수 있어요.");}
   badge.Invalidate(); if(tray!=null)tray.Text="윈도우 타이머 · "+(active?job.Due.Value.ToString("HH:mm")+" "+job.Action:"예약 대기 중");
   p10.Selected=relative&&minutes==10;p30.Selected=relative&&minutes==30;p60.Selected=relative&&minutes==60;p120.Selected=relative&&minutes==120;
   Invalidate();
  }
  static string FormatTime(TimeSpan t){if(t<TimeSpan.Zero)t=TimeSpan.Zero;int h=(int)Math.Floor(t.TotalHours);return h.ToString("00")+":"+t.Minutes.ToString("00")+":"+t.Seconds.ToString("00");}
  void StartJob(){try{DateTime now=DateTime.Now;DateTime due=relative?now.AddMinutes(minutes):datePicker.Value;job.Start(now,due,selectedAction);lastTick=now;RefreshView(null);}catch(ArgumentException e){MessageBox.Show(this,e.Message,"시간을 확인해 주세요",MessageBoxButtons.OK,MessageBoxIcon.Information);}}
  void Reveal(){Show();WindowState=FormWindowState.Normal;Activate();}
  void ExitApp(){if(job.Due.HasValue&&MessageBox.Show(this,"앱을 끝내면 현재 예약도 취소돼요. 종료할까요?","예약 취소 및 앱 종료",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;job.Cancel();exiting=true;Close();}
  void ShowAbout(){using(Form f=new Form()){f.Text="윈도우 타이머 정보";f.ClientSize=new Size(410,270);f.FormBorderStyle=FormBorderStyle.FixedDialog;f.MaximizeBox=false;f.MinimizeBox=false;f.StartPosition=FormStartPosition.CenterParent;f.BackColor=Palette.Canvas;f.Font=UIFont.Make(10,FontStyle.Regular);f.Icon=Icon;AddLabel(f,"윈도우 타이머",26,22,350,40,22,Palette.Brown,FontStyle.Bold);AddLabel(f,"원하는 시간에 Windows 종료 또는 재부팅을 예약해요.\n예약 전 알림과 취소, 10분 연장을 지원합니다.\n\n버전 1.0.1  ·  개발자 Lebi (@cybereun)",28,78,350,94,10,Palette.Muted,FontStyle.Regular);SoftButton b=new SoftButton("닫기",ButtonTone.Primary){Location=new Point(27,205),Size=new Size(356,42)};b.Click+=delegate{f.Close();};f.Controls.Add(b);f.ShowDialog(this);}}
  void TickJob(){DateTime now=DateTime.Now;bool resumed=(now-lastTick).TotalSeconds>5;lastTick=now;if(!job.Due.HasValue)return;int result=job.Tick(now,resumed);if(result==0){number.Text=FormatTime(job.Due.Value-now);return;}if(result==1){RefreshView(null);dueLabel.Text="곧 Windows "+job.Action+"돼요 · 지금 취소할 수 있어요";Reveal();tray.ShowBalloonTip(5000,"Windows "+job.Action+" 예정","실행 60초 전이에요. 원하지 않으면 예약을 취소해 주세요.",ToolTipIcon.Warning);System.Media.SystemSounds.Exclamation.Play();}
   if(result==2){string requested=job.Action;job.Cancel();RefreshView(null);if(demo){RefreshView("데모 완료 · 실제 종료 명령은 실행하지 않았어요");return;}try{string args=requested=="종료"?"/s /t 0":"/r /t 0";ProcessStartInfo info=new ProcessStartInfo("shutdown.exe",args){UseShellExecute=false,CreateNoWindow=true};using(Process p=Process.Start(info)){p.WaitForExit(5000);if(!p.HasExited||p.ExitCode!=0)throw new Exception("Windows가 요청을 처리하지 못했어요.");}RefreshView("Windows에 "+requested+"를 요청했어요");}catch(Exception ex){Reveal();MessageBox.Show(this,ex.Message,"실행하지 못했어요",MessageBoxButtons.OK,MessageBoxIcon.Error);}}
  }
 }

 static class Program {
  [STAThread] static int Main(string[] args){if(Array.IndexOf(args,"--self-test")>=0)return Tests.Run();bool created;using(Mutex mutex=new Mutex(true,"Local\\WindowsTimer.Desktop",out created)){if(!created){MessageBox.Show("윈도우 타이머가 이미 실행 중이에요. 작업 표시줄의 아이콘을 확인해 주세요.","윈도우 타이머");return 0;}Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new MainForm(Array.IndexOf(args,"--demo")>=0));}UIFont.Cleanup();return 0;}
 }
 static class Tests {
  static void Check(bool value,string message){if(!value)throw new Exception(message);}
  public static int Run(){try{DateTime now=new DateTime(2026,10,21,12,0,0);Schedule s=new Schedule();bool rejected=false;try{s.Start(now,now,"종료");}catch(ArgumentException){rejected=true;}Check(rejected,"past rejected");s.Start(now,now.AddMinutes(2),"종료");Check(s.Tick(now,false)==0,"not early");Check(s.Tick(now.AddMinutes(1),false)==1,"warning");Check(s.Tick(now.AddSeconds(119),false)==0,"not due");Check(s.Tick(now.AddMinutes(2),false)==2,"due");s.Cancel();Check(s.Tick(now.AddHours(1),false)==0,"cancel");s.Start(now,now.AddMinutes(2),"재부팅");s.Tick(now.AddMinutes(1),false);s.Extend();Check(!s.Warned&&s.Due==now.AddMinutes(12),"extend resets warning");Check(s.Tick(now.AddHours(1),true)==1&&s.Due==now.AddHours(1).AddSeconds(60),"resume grace");s.Cancel();s.Start(now,now.AddSeconds(10),"종료");Check(s.Tick(now,false)==1,"short warning");Check(s.Tick(now.AddSeconds(10),false)==2,"short due");File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results.txt"),"PASS: 9 scheduling checks. No shutdown commands executed.\r\n");return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results.txt"),"FAIL: "+ex);return 1;}}
 }
}
