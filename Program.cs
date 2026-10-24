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
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace WindowsTimer {
 public class Schedule {
  public DateTime? Due; public string Action; public bool Warned; DateTime? blockedSince;
  public void Start(DateTime now, DateTime due, string action) {
   if (due <= now) throw new ArgumentException("현재보다 뒤의 시간을 선택해 주세요.");
   if (Array.IndexOf(new string[]{"종료","재부팅","절전","최대 절전"},action)<0) throw new ArgumentException("동작을 선택해 주세요.");
   Due = due; Action = action; Warned = false; blockedSince=null;
  }
  public void Cancel() { Due = null; Warned = false; blockedSince=null; }
  public void Extend() { if (Due.HasValue) { Due = Due.Value.AddMinutes(10); Warned = false; } }
  public void Defer(DateTime due) { Due = due; Warned = false; }
  public bool GuardTimedOut(DateTime now,int maxWaitMinutes){if(!blockedSince.HasValue)blockedSince=now;return (now-blockedSince.Value).TotalMinutes>=maxWaitMinutes;}
  public void ClearGuardWait(){blockedSince=null;}
  public int Tick(DateTime now, bool resumed) {
   if (!Due.HasValue) return 0;
   if (resumed && Due.Value <= now.AddSeconds(60)) { Due = now.AddSeconds(60); Warned = false; }
   if (!Warned && (Due.Value-now).TotalSeconds <= 60) { Warned = true; if(Due.Value <= now) Due = now.AddSeconds(60); return 1; }
   if (Warned && Due.Value <= now) return 2;
   return 0;
  }
  public static DateTime? NextOccurrence(DateTime after,string mode,string atTime,int[] days){TimeSpan at;if(!TimeSpan.TryParse(atTime,out at))at=new TimeSpan(23,0,0);for(int offset=0;offset<8;offset++){DateTime candidate=after.Date.AddDays(offset).Add(at);int weekday=((int)candidate.DayOfWeek+6)%7+1;bool selected=mode!="요일 선택"||(days!=null&&Array.IndexOf(days,weekday)>=0);if(selected&&candidate>after)return candidate;}return null;}
 }

 public sealed class TimerProfile {
  public string Name { get; set; }
  public string Action { get; set; }
  public int Minutes { get; set; }
  public bool Relative { get; set; }
  public DateTime AtTime { get; set; }
 }
 public sealed class HistoryEntry {
  public string When { get; set; }
  public string Text { get; set; }
 }
 public sealed class AppSettings {
  public int[] Presets { get; set; }
  public bool GuardBattery { get; set; }
  public int BatteryLimit { get; set; }
  public bool RequireAC { get; set; }
  public bool RequireIdle { get; set; }
  public int IdleMinutes { get; set; }
  public int GuardTimeoutMinutes { get; set; }
  public bool StartWithWindows { get; set; }
  public bool WakeEnabled { get; set; }
  public bool TrayCountdown { get; set; }
  public bool RepeatEnabled { get; set; }
  public string RepeatMode { get; set; }
  public string RepeatTime { get; set; }
  public string RepeatAction { get; set; }
  public int[] RepeatDays { get; set; }
  public string Language { get; set; }
  public List<TimerProfile> Profiles { get; set; }
  public List<HistoryEntry> History { get; set; }
  public bool RemoteEnabled { get; set; }
  public List<string> RemoteTokens { get; set; }
  public AppSettings() {
   Presets=new int[]{10,30,60,120}; BatteryLimit=20; IdleMinutes=10; GuardTimeoutMinutes=120;
   TrayCountdown=true; RepeatMode="매일"; RepeatTime="23:00"; RepeatAction="종료"; RepeatDays=new int[]{1,2,3,4,5};
   Language="한국어"; Profiles=new List<TimerProfile>(); History=new List<HistoryEntry>(); RemoteTokens=new List<string>();
  }
 }
 static class SettingsStore {
  public static string DirectoryPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WindowsTimer");
  static string FilePath { get { return Path.Combine(DirectoryPath,"settings.json"); } }
  public static AppSettings Load() { try { if(!File.Exists(FilePath))return new AppSettings(); JavaScriptSerializer j=new JavaScriptSerializer(); AppSettings s=j.Deserialize<AppSettings>(File.ReadAllText(FilePath,Encoding.UTF8)); if(s==null)s=new AppSettings(); Normalize(s); for(int i=s.RemoteTokens.Count-1;i>=0;i--)if(s.RemoteTokens[i].StartsWith("dpapi:",StringComparison.Ordinal)){try{s.RemoteTokens[i]=UnprotectToken(s.RemoteTokens[i].Substring(6));}catch{s.RemoteTokens.RemoveAt(i);}} return s; } catch { return new AppSettings(); } }
  static void Normalize(AppSettings s) { if(s.Presets==null||s.Presets.Length!=4)s.Presets=new int[]{10,30,60,120};for(int i=0;i<s.Presets.Length;i++)s.Presets[i]=Math.Max(1,Math.Min(10080,s.Presets[i]));if(s.Profiles==null)s.Profiles=new List<TimerProfile>();else s.Profiles.RemoveAll(profile=>profile==null);if(s.History==null)s.History=new List<HistoryEntry>();else s.History.RemoveAll(entry=>entry==null);if(s.RemoteTokens==null)s.RemoteTokens=new List<string>();else s.RemoteTokens.RemoveAll(token=>String.IsNullOrEmpty(token));if(s.RepeatDays==null)s.RepeatDays=new int[]{1,2,3,4,5};List<int>days=new List<int>();foreach(int day in s.RepeatDays)if(day>=1&&day<=7&&!days.Contains(day))days.Add(day);s.RepeatDays=days.ToArray();if(s.Language!="한국어"&&s.Language!="English"&&s.Language!="日本語"&&s.Language!="简体中文")s.Language="한국어";if(s.RepeatMode!="매일"&&s.RepeatMode!="요일 선택")s.RepeatMode="매일";TimeSpan repeatTime;if(!TimeSpan.TryParse(s.RepeatTime,out repeatTime))s.RepeatTime="23:00";if(s.RepeatAction!="종료"&&s.RepeatAction!="재부팅"&&s.RepeatAction!="절전"&&s.RepeatAction!="최대 절전")s.RepeatAction="종료";foreach(TimerProfile profile in s.Profiles){if(profile==null)continue;profile.Minutes=Math.Max(1,Math.Min(10080,profile.Minutes));if(profile.Action!="종료"&&profile.Action!="재부팅"&&profile.Action!="절전"&&profile.Action!="최대 절전")profile.Action="종료";} }
  public static void Save(AppSettings s) { Directory.CreateDirectory(DirectoryPath); JavaScriptSerializer j=new JavaScriptSerializer(); AppSettings persisted=j.Deserialize<AppSettings>(j.Serialize(s)); for(int i=0;i<persisted.RemoteTokens.Count;i++)persisted.RemoteTokens[i]="dpapi:"+ProtectToken(persisted.RemoteTokens[i]); string tmp=FilePath+".tmp"; File.WriteAllText(tmp,j.Serialize(persisted),new UTF8Encoding(false)); if(File.Exists(FilePath))File.Replace(tmp,FilePath,null);else File.Move(tmp,FilePath); }
  internal static string ProtectToken(string token){byte[] plain=Encoding.UTF8.GetBytes(token);return Convert.ToBase64String(ProtectedData.Protect(plain,null,DataProtectionScope.CurrentUser));}
  internal static string UnprotectToken(string token){byte[] plain=ProtectedData.Unprotect(Convert.FromBase64String(token),null,DataProtectionScope.CurrentUser);return Encoding.UTF8.GetString(plain);}
 }
 static class SystemState {
  [StructLayout(LayoutKind.Sequential)] struct POWER { public byte ACLineStatus, BatteryFlag, BatteryLifePercent, Reserved; public int BatteryLifeTime, BatteryFullLifeTime; }
  [StructLayout(LayoutKind.Sequential)] struct LASTINPUT { public uint cbSize, dwTime; }
  [DllImport("kernel32.dll")] static extern bool GetSystemPowerStatus(out POWER s);
  [DllImport("user32.dll")] static extern bool GetLastInputInfo(ref LASTINPUT i);
  public static bool TryPower(out bool onAC,out int percent,out bool knownAC) { POWER p; if(!GetSystemPowerStatus(out p)){onAC=false;percent=-1;knownAC=false;return false;} onAC=p.ACLineStatus==1;knownAC=p.ACLineStatus!=255;percent=p.BatteryLifePercent==255?-1:p.BatteryLifePercent; return true; }
  public static double IdleMinutes() { LASTINPUT i=new LASTINPUT();i.cbSize=(uint)Marshal.SizeOf(typeof(LASTINPUT));if(!GetLastInputInfo(ref i))return 0;uint elapsed=unchecked((uint)Environment.TickCount)-i.dwTime;return elapsed/60000.0; }
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
   Rectangle textRect=r;if(down)textRect.Offset(0,1);float fontSize=Font==null?11f:Font.Size;FontStyle textStyle=Selected||primary?FontStyle.Bold:FontStyle.Regular;while(fontSize>7.5f){using(Font measure=UIFont.Make(fontSize,textStyle)){if(g.MeasureString(Text,measure).Width<=textRect.Width-12)break;}fontSize=Math.Max(7.5f,fontSize-0.5f);}using(Font f=UIFont.Make(fontSize,textStyle))using(SolidBrush brush=new SolidBrush(fg))using(StringFormat sf=new StringFormat()){sf.Alignment=StringAlignment.Center;sf.LineAlignment=StringAlignment.Center;sf.Trimming=StringTrimming.EllipsisCharacter;sf.FormatFlags=StringFormatFlags.NoWrap;g.DrawString(Text,f,brush,textRect,sf);}
   if(Focused&&ShowFocusCues){Rectangle focus=r;focus.Inflate(-5,-5);using(Pen p=new Pen(Color.FromArgb(130,Palette.Brown))){p.DashPattern=new float[]{2,2};using(GraphicsPath path=Shape.RoundRect(focus,Math.Max(4,radius-4)))g.DrawPath(p,path);}}
  }
 }

 static class Shape {
  public static GraphicsPath RoundRect(Rectangle r,int radius){GraphicsPath p=new GraphicsPath();int d=Math.Max(2,radius*2);p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
 }

 sealed class CardPanel : Panel {
  public Color PanelTop=Palette.Paper, PanelBottom=Palette.Paper; public int Radius=24; public string Group;
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
   using(Font f=UIFont.Make(13,FontStyle.Bold))using(SolidBrush b=new SolidBrush(Palette.Brown))g.DrawString(Translations.Get("차분한 하루의 마무리",Translations.CurrentLanguage),f,b,new RectangleF(31,Height-64,Width-62,22));
   using(Font f=UIFont.Make(9,FontStyle.Regular))using(SolidBrush b=new SolidBrush(Palette.Muted))g.DrawString(Translations.Get("원하는 순간을 정하면 마지막 60초를 알려드려요",Translations.CurrentLanguage),f,b,new RectangleF(31,Height-40,Width-62,18));
  }
 }

 sealed class StatusBadge : Control {
  public string StatusText="예약 대기 중"; public bool Active;
  public StatusBadge(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);AccessibleRole=AccessibleRole.StatusBar;}
  protected override void OnPaint(PaintEventArgs e){Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;Rectangle r=ClientRectangle;r.Inflate(-1,-1);using(GraphicsPath p=Shape.RoundRect(r,18))using(SolidBrush b=new SolidBrush(Active?Color.FromArgb(250,235,225):Color.FromArgb(238,242,234)))g.FillPath(b,p);using(SolidBrush dot=new SolidBrush(Active?Palette.Clay:Palette.Green))g.FillEllipse(dot,16,Height/2-4,8,8);using(Font f=UIFont.Make(9,FontStyle.Bold))using(SolidBrush b=new SolidBrush(Active?Palette.ClayDark:Palette.Green))using(StringFormat sf=new StringFormat()){sf.Alignment=StringAlignment.Near;sf.LineAlignment=StringAlignment.Center;g.DrawString(StatusText,f,b,new RectangleF(31,0,Width-38,Height),sf);}}
 }

 public class MainForm : Form {
  enum UpdateDisplayState { Idle, Checking, Current, Downloading, Ready, Waiting, Installing, Authentication, NetworkError, ReleaseError, VerificationError, InstallError, Demo }
  readonly Schedule job=new Schedule(); readonly Schedule repeatJob=new Schedule(); readonly bool demo; readonly AppSettings settings;
  readonly Color ink=Palette.Ink, muted=Palette.Muted;
  string selectedAction="종료"; bool relative=true, exiting, showingAdvanced; int minutes=30;
  readonly List<Control> mainContent=new List<Control>(); DateTime? nextRepeatDue;
  Label headline,subhead,number,valueHint,dueLabel,safetyLabel,footer; StatusBadge badge;
  SoftButton shutdownButton,restartButton,relativeButton,dateButton,minusButton,plusButton,p10,p30,p60,p120,mainButton,cancelButton,extendButton,infoButton,advancedNav;
  DateTimePicker datePicker; HeroPanel heroPanel; Image heroImage; NotifyIcon tray; System.Windows.Forms.Timer timer,updateStartupTimer; DateTime lastTick=DateTime.Now,lastAdvancedRefresh=DateTime.MinValue;
  Panel advancedView; FlowLayoutPanel advancedFlow; Label advancedHint,historyList,remoteInfo,updateStatus; ComboBox repeatMode,repeatAction,languageBox,profileBox;
  SoftButton updateButton; UpdateDisplayState updateDisplayState=UpdateDisplayState.Idle; UpdateInfo pendingUpdate; string pendingUpdateFile; bool updateCheckRunning,updateApplyStarted; Version updateDisplayVersion;
  readonly List<SoftButton> advancedTabButtons=new List<SoftButton>(); string advancedTab="일정";
  DateTimePicker repeatTime; CheckBox repeatEnabled,guardBattery,requireAC,requireIdle,startWithWindows,wakeEnabled,trayCountdown,remoteEnabled;
  readonly List<CheckBox> repeatDayBoxes=new List<CheckBox>();
  NumericUpDown batteryLimit,idleMinutes,guardTimeout; TextBox presetInputs,profileName;
  readonly RemoteServer remoteServer=new RemoteServer();
  public MainForm(bool demoMode) {
   demo=demoMode;if(demo)SettingsStore.DirectoryPath=Path.Combine(Path.GetTempPath(),"WindowsTimerDemo");settings=SettingsStore.Load();minutes=settings.Presets[1]; Text="윈도우 타이머"+(demo?" · 안전 데모":""); ClientSize=new Size(1220,790);MinimumSize=new Size(1236,830);MaximizeBox=false;FormBorderStyle=FormBorderStyle.FixedSingle;StartPosition=FormStartPosition.CenterScreen;AutoScaleMode=AutoScaleMode.Dpi;BackColor=Palette.Canvas;ForeColor=Palette.Ink;Font=UIFont.Make(10,FontStyle.Regular);DoubleBuffered=true;
   Icon appIcon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);Icon=appIcon??SystemIcons.Application;
   LoadHero(); BuildShell(); BuildSettings(); BuildAdvanced(); ApplyLanguage();
   tray=new NotifyIcon{Icon=Icon,Text="윈도우 타이머 · 예약 대기 중",Visible=true};ContextMenuStrip menu=new ContextMenuStrip();menu.Items.Add(T("창 열기"),null,delegate{Reveal();});menu.Items.Add(T("예약 취소"),null,delegate{CancelCurrent();});menu.Items.Add(T("앱 종료"),null,delegate{ExitApp();});tray.ContextMenuStrip=menu;tray.DoubleClick+=delegate{Reveal();};tray.BalloonTipClicked+=delegate{Reveal();};
   FormClosing+=delegate(object s,FormClosingEventArgs e){if(!exiting&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();tray.ShowBalloonTip(2500,T("윈도우 타이머"),job.Due.HasValue?T("예약을 유지하고 있어요. 트레이 메뉴에서 취소할 수 있어요."):T("트레이 아이콘에서 다시 열거나 종료할 수 있어요."),ToolTipIcon.Info);}};
   FormClosed+=delegate{if(timer!=null)timer.Stop();if(updateStartupTimer!=null)updateStartupTimer.Stop();if(tray!=null){tray.Visible=false;tray.Dispose();}if(heroImage!=null)heroImage.Dispose();remoteServer.Stop();if(!demo)WakeTask.Remove();};
   timer=new System.Windows.Forms.Timer{Interval=250};timer.Tick+=delegate{TickJob();};timer.Start();
   if(!demo){UpdateManager.CleanupStaleDownloads();updateStartupTimer=new System.Windows.Forms.Timer{Interval=12000};updateStartupTimer.Tick+=delegate{updateStartupTimer.Stop();BeginUpdateCheck(false);updateStartupTimer.Interval=6*60*60*1000;updateStartupTimer.Start();};updateStartupTimer.Start();}
   if(settings.RemoteEnabled)StartRemoteServer(false);
   if(settings.RepeatEnabled)ScheduleNextRepeat(DateTime.Now);
   RefreshView(null); RefreshAdvancedStatus();
  }
  void LoadHero(){try{using(Stream s=Assembly.GetExecutingAssembly().GetManifestResourceStream("WindowsTimer.assets.hero-timer.png")){if(s!=null)using(Image i=Image.FromStream(s))heroImage=new Bitmap(i);}}catch{heroImage=null;}}
  Label AddLabel(Control parent,string text,int x,int y,int w,int h,float size,Color color,FontStyle style){Label l=new Label{Text=text,Tag=text,Location=new Point(x,y),Size=new Size(w,h),BackColor=Color.Transparent,ForeColor=color,Font=UIFont.Make(size,style),UseCompatibleTextRendering=true,TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};parent.Controls.Add(l);if(parent==this)mainContent.Add(l);return l;}
  SoftButton AddButton(Control parent,string text,int x,int y,int w,int h,ButtonTone tone,EventHandler click){SoftButton b=new SoftButton(text,tone){Location=new Point(x,y),Size=new Size(w,h),Tag=text};if(click!=null)b.Click+=click;parent.Controls.Add(b);return b;}
  Label AdvLabel(Control p,string text,int x,int y,int w,int h,float size,Color color,FontStyle style){Label l=AddLabel(p,text,x,y,w,h,size,color,style);return l;}
  CheckBox AdvCheck(Control p,string text,int x,int y,int w,int h,bool value){CheckBox c=new CheckBox{Text=T(text),Tag=text,Checked=value,Location=new Point(x,y),Size=new Size(w,h),BackColor=Color.Transparent,ForeColor=Palette.Ink,Font=UIFont.Make(9,FontStyle.Regular),FlatStyle=FlatStyle.Flat,AutoSize=false};p.Controls.Add(c);return c;}
  NumericUpDown AdvNumber(Control p,int x,int y,int w,int min,int max,int value){NumericUpDown n=new NumericUpDown{Location=new Point(x,y),Size=new Size(w,30),Minimum=min,Maximum=max,Value=Math.Max(min,Math.Min(max,value)),Font=UIFont.Make(9,FontStyle.Regular),TextAlign=HorizontalAlignment.Center,BackColor=Palette.Paper,ForeColor=Palette.Brown};p.Controls.Add(n);return n;}
  ComboBox AdvCombo(Control p,int x,int y,int w,params string[] items){ComboBox c=new ComboBox{Location=new Point(x,y),Size=new Size(w,30),DropDownStyle=ComboBoxStyle.DropDownList,Font=UIFont.Make(9,FontStyle.Regular),FlatStyle=FlatStyle.Flat,Tag=(object)items.Clone()};foreach(string item in items)c.Items.Add(item);if(c.Items.Count>0)c.SelectedIndex=0;p.Controls.Add(c);return c;}
  CardPanel AdvCard(string title,int height,string group){CardPanel p=new CardPanel{Size=new Size(507,height),Radius=18,Margin=new Padding(0,0,18,18),Padding=new Padding(16),BackColor=Palette.Paper,Group=group};AdvLabel(p,title,20,12,450,25,12,Palette.Brown,FontStyle.Bold);return p;}
  SoftButton AdvButton(Control p,string text,int x,int y,int w,EventHandler click,ButtonTone tone){SoftButton b=AddButton(p,text,x,y,w,34,tone,click);b.Font=UIFont.Make(9,FontStyle.Bold);return b;}
  void BuildAdvanced(){
   advancedView=new Panel{Location=new Point(88,0),Size=new Size(ClientSize.Width-88,ClientSize.Height),BackColor=Palette.Canvas,Visible=false,Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right};Controls.Add(advancedView);
   AdvLabel(advancedView,"고급 설정",28,22,500,38,23,Palette.Brown,FontStyle.Bold);
   advancedHint=AdvLabel(advancedView,"반복 예약과 안전 조건을 설정해요. 메인 예약 화면은 현재 설정을 그대로 유지합니다.",29,62,1015,28,9,Palette.Muted,FontStyle.Regular);
   string[] tabGroups={"일정","보호 · 시스템","프로필 · 기록","원격 · 언어"};
   for(int i=0;i<tabGroups.Length;i++){string group=tabGroups[i];SoftButton tab=AddButton(advancedView,group,22+i*268,101,252,42,ButtonTone.Soft,delegate{SelectAdvancedTab(group);});tab.Font=UIFont.Make(9,FontStyle.Bold);tab.AccessibleRole=AccessibleRole.PageTab;tab.AccessibleName=group;advancedTabButtons.Add(tab);}
   advancedFlow=new FlowLayoutPanel{Location=new Point(22,157),Size=new Size(advancedView.Width-40,advancedView.Height-171),AutoScroll=true,WrapContents=true,FlowDirection=FlowDirection.LeftToRight,BackColor=Palette.Canvas,Padding=new Padding(4),Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right};advancedView.Controls.Add(advancedFlow);

   CardPanel recurring=AdvCard("반복 일정",220,"일정");advancedFlow.Controls.Add(recurring);
   repeatEnabled=AdvCheck(recurring,"매일 또는 선택한 요일에 반복 실행",20,48,455,28,settings.RepeatEnabled);
   repeatMode=AdvCombo(recurring,20,83,140,"매일","요일 선택");repeatMode.SelectedItem=settings.RepeatMode=="요일 선택"?"요일 선택":"매일";
   TimeSpan savedRepeatTime; if(!TimeSpan.TryParse(settings.RepeatTime,out savedRepeatTime))savedRepeatTime=new TimeSpan(23,0,0);
   repeatTime=new DateTimePicker{Location=new Point(170,83),Size=new Size(116,30),Format=DateTimePickerFormat.Custom,CustomFormat="HH:mm",ShowUpDown=true,Value=DateTime.Today.Add(savedRepeatTime),Font=UIFont.Make(9,FontStyle.Regular)};recurring.Controls.Add(repeatTime);
   repeatAction=AdvCombo(recurring,296,83,178,"종료","재부팅","절전","최대 절전");repeatAction.SelectedItem=repeatAction.Items.Contains(settings.RepeatAction)?settings.RepeatAction:"종료";
   string[] dayNames={"월","화","수","목","금","토","일"};for(int di=0;di<7;di++){CheckBox day=AdvCheck(recurring,dayNames[di],20+di*63,119,57,24,Array.IndexOf(settings.RepeatDays,di+1)>=0);day.Tag=dayNames[di];day.Text=dayNames[di];day.TextAlign=ContentAlignment.MiddleCenter;repeatDayBoxes.Add(day);}
   AdvButton(recurring,"반복 일정 저장",20,153,455,delegate{SaveRepeat();},ButtonTone.Primary);
   AdvLabel(recurring,"다음 실행은 60초 알림과 취소 기능을 거쳐요.",20,190,455,19,8,Palette.Muted,FontStyle.Regular);

   CardPanel preset=AdvCard("빠른 시간 버튼",177,"일정");advancedFlow.Controls.Add(preset);
   AdvLabel(preset,"메인 화면의 네 버튼에 표시할 분 단위를 입력하세요. 예: 10, 30, 60, 120",20,46,455,34,8,Palette.Muted,FontStyle.Regular);
   presetInputs=new TextBox{Location=new Point(20,88),Size=new Size(455,34),Text=String.Join(", ",settings.Presets),Font=UIFont.Make(10,FontStyle.Regular),BorderStyle=BorderStyle.FixedSingle};preset.Controls.Add(presetInputs);
   AdvButton(preset,"빠른 시간 저장",20,132,455,delegate{SavePresets();},ButtonTone.Primary);

   CardPanel conditions=AdvCard("전원 · 사용 상태 보호",246,"보호 · 시스템");advancedFlow.Controls.Add(conditions);
   guardBattery=AdvCheck(conditions,"배터리 잔량이 기준보다 낮으면 대기",20,48,310,26,settings.GuardBattery);
   batteryLimit=AdvNumber(conditions,353,47,112,5,90,settings.BatteryLimit);AdvLabel(conditions,"기준 %",469,49,24,23,8,Palette.Muted,FontStyle.Regular);
   requireAC=AdvCheck(conditions,"AC 전원이 연결되어 있을 때만 실행",20,82,455,26,settings.RequireAC);
   requireIdle=AdvCheck(conditions,"입력 없이 지정 시간 동안 유휴 상태일 때 실행",20,116,455,26,settings.RequireIdle);
   AdvLabel(conditions,"유휴 분",28,151,80,25,8,Palette.Muted,FontStyle.Regular);idleMinutes=AdvNumber(conditions,104,149,84,1,240,settings.IdleMinutes);
   AdvLabel(conditions,"최대 대기 분",213,151,90,25,8,Palette.Muted,FontStyle.Regular);guardTimeout=AdvNumber(conditions,307,149,84,1,1440,settings.GuardTimeoutMinutes);
   AdvButton(conditions,"보호 조건 저장",20,190,455,delegate{SaveConditions();},ButtonTone.Primary);
   AdvLabel(conditions,"조건을 충족하지 못하면 예약을 보류하고 제한 시간 후 취소해요.",20,222,455,18,8,Palette.Muted,FontStyle.Regular);

   CardPanel system=AdvCard("Windows · 알림 설정",220,"보호 · 시스템");advancedFlow.Controls.Add(system);
   startWithWindows=AdvCheck(system,"Windows 로그인 시 자동 실행",20,48,455,26,settings.StartWithWindows);
   wakeEnabled=AdvCheck(system,"절전 중 예약 시각에 PC 깨우기",20,82,455,26,settings.WakeEnabled);
   trayCountdown=AdvCheck(system,"작업 표시줄 아이콘에 남은 시간 표시",20,116,455,26,settings.TrayCountdown);
   AdvLabel(system,"깨우기는 PC·BIOS의 절전 타이머 지원이 필요해요.",20,149,455,22,8,Palette.Muted,FontStyle.Regular);
   AdvButton(system,"Windows 설정 저장",20,177,455,delegate{SaveSystemOptions();},ButtonTone.Primary);

   CardPanel sleep=AdvCard("한 번 예약 · 다른 동작",180,"일정");advancedFlow.Controls.Add(sleep);
   AdvLabel(sleep,"종료·재부팅 외에도 절전과 최대 절전을 예약할 수 있어요.",20,47,455,29,8,Palette.Muted,FontStyle.Regular);
   ComboBox onceAction=AdvCombo(sleep,20,83,180,"종료","재부팅","절전","최대 절전");
   NumericUpDown onceMinutes=AdvNumber(sleep,215,83,100,1,10080,30);AdvLabel(sleep,"분 뒤",323,85,80,25,8,Palette.Muted,FontStyle.Regular);
   AdvButton(sleep,"이 동작 예약",20,124,455,delegate{selectedAction=Canonical((string)onceAction.SelectedItem);minutes=(int)onceMinutes.Value;relative=true;StartJob();ShowMain();},ButtonTone.Primary);
   AdvLabel(sleep,"한 번 예약도 메인 화면에서 취소하거나 10분 연장할 수 있어요.",20,160,455,18,8,Palette.Muted,FontStyle.Regular);

   CardPanel profiles=AdvCard("프로필",205,"프로필 · 기록");advancedFlow.Controls.Add(profiles);
   AdvLabel(profiles,"현재 메인 화면의 시간과 동작을 저장해 나중에 다시 적용해요.",20,46,455,29,8,Palette.Muted,FontStyle.Regular);
   profileName=new TextBox{Location=new Point(20,81),Size=new Size(290,30),Font=UIFont.Make(9,FontStyle.Regular),BorderStyle=BorderStyle.FixedSingle};profiles.Controls.Add(profileName);
   AdvButton(profiles,"현재 설정 저장",320,80,155,delegate{SaveProfile();},ButtonTone.Primary);
   profileBox=AdvCombo(profiles,20,124,290);profileBox.Tag=null;RefreshProfileList();
   AdvButton(profiles,"적용",320,123,73,delegate{ApplyProfile();},ButtonTone.Soft);
   AdvButton(profiles,"삭제",402,123,73,delegate{DeleteProfile();},ButtonTone.Soft);
   AdvLabel(profiles,"프로필은 이 Windows 사용자 계정에만 저장돼요.",20,166,455,20,8,Palette.Muted,FontStyle.Regular);

   CardPanel history=AdvCard("예약 기록",226,"프로필 · 기록");advancedFlow.Controls.Add(history);
   historyList=AdvLabel(history,"아직 기록이 없어요.",20,47,455,125,8,Palette.Muted,FontStyle.Regular);historyList.TextAlign=ContentAlignment.TopLeft;
   AdvButton(history,"기록 지우기",20,178,455,delegate{settings.History.Clear();SaveSettings();RefreshHistory();},ButtonTone.Soft);RefreshHistory();

   CardPanel language=AdvCard("언어 · 업데이트",177,"원격 · 언어");advancedFlow.Controls.Add(language);
   AdvLabel(language,"화면 언어",20,48,100,26,9,Palette.Ink,FontStyle.Regular);
   languageBox=AdvCombo(language,125,46,170,"한국어","English","日本語","简体中文");languageBox.SelectedItem=settings.Language;
   AdvButton(language,"언어 적용",310,45,165,delegate{settings.Language=Canonical((string)languageBox.SelectedItem);SaveSettings();ApplyLanguage();},ButtonTone.Primary);
   updateStatus=AdvLabel(language,"현재 버전 · v"+UpdateManager.VersionText,20,88,455,25,8,Palette.Muted,FontStyle.Regular);
   updateButton=AdvButton(language,"업데이트 확인",20,122,455,delegate{BeginUpdateCheck(true);},ButtonTone.Soft);

   CardPanel remote=AdvCard("휴대폰 · 다른 PC에서 원격 예약",323,"원격 · 언어");advancedFlow.Controls.Add(remote);
   remoteEnabled=AdvCheck(remote,"같은 네트워크 또는 VPN에서 원격 접속 허용",20,48,455,26,settings.RemoteEnabled);
   remoteInfo=AdvLabel(remote,"원격 서버가 꺼져 있어요.",20,80,455,107,8,Palette.Muted,FontStyle.Regular);remoteInfo.TextAlign=ContentAlignment.TopLeft;
   AdvButton(remote,"첫 번째 연결 주소 복사",20,192,455,delegate{CopyRemoteAddress();},ButtonTone.Soft);
   AdvButton(remote,"모든 기기 연결 해제 · 코드 재발급",20,231,455,delegate{remoteServer.ClearTokens();RotatePairingCode();SaveSettings();RefreshRemoteInfo();},ButtonTone.Primary);
   AdvLabel(remote,"LAN은 암호화되지 않으므로 신뢰할 수 있는 사설망에서만 사용하세요. 외부에서는 두 기기를 VPN에 연결하세요.",20,270,455,34,7.5f,Palette.Muted,FontStyle.Regular);

   repeatEnabled.CheckedChanged+=delegate{settings.RepeatEnabled=repeatEnabled.Checked;SaveSettings();if(settings.RepeatEnabled)ScheduleNextRepeat(DateTime.Now);else{repeatJob.Cancel();nextRepeatDue=null;UpdateWakeTask();RefreshView("반복 예약을 껐어요");}RefreshAdvancedStatus();};
   startWithWindows.CheckedChanged+=delegate{SetStartup(startWithWindows.Checked);};
   trayCountdown.CheckedChanged+=delegate{settings.TrayCountdown=trayCountdown.Checked;SaveSettings();RefreshView(null);};
   wakeEnabled.CheckedChanged+=delegate{settings.WakeEnabled=wakeEnabled.Checked;SaveSettings();UpdateWakeTask();};
   remoteEnabled.CheckedChanged+=delegate{settings.RemoteEnabled=remoteEnabled.Checked;SaveSettings();if(settings.RemoteEnabled)StartRemoteServer(true);else{remoteServer.Stop();RefreshRemoteInfo();}};
   SelectAdvancedTab(advancedTab);
  }
  void SelectAdvancedTab(string group){if(String.IsNullOrEmpty(group))return;advancedTab=group;if(advancedFlow!=null){int groupHeight=0;foreach(Control c in advancedFlow.Controls){CardPanel card=c as CardPanel;if(card!=null&&card.Group==group)groupHeight=Math.Max(groupHeight,card.Height);}advancedFlow.SuspendLayout();foreach(Control c in advancedFlow.Controls){CardPanel card=c as CardPanel;if(card!=null){if(card.Group==group)card.Height=groupHeight;card.Visible=card.Group==group;}}advancedFlow.AutoScrollPosition=new Point(0,0);advancedFlow.ResumeLayout(true);}foreach(SoftButton tab in advancedTabButtons){bool selected=tab.Tag as string==group;tab.Selected=selected;tab.AccessibleName=tab.Text;}}
  void ShowMain(){showingAdvanced=false;advancedView.Visible=false;foreach(Control c in mainContent)c.Visible=true;advancedNav.Selected=false;InvalidateNavigation();}
  void ShowAdvanced(){showingAdvanced=true;foreach(Control c in mainContent)c.Visible=false;advancedView.Visible=true;advancedView.BringToFront();advancedNav.Selected=true;RefreshAdvancedStatus();RefreshRemoteInfo();InvalidateNavigation();}
  void InvalidateNavigation(){if(advancedNav!=null)advancedNav.Invalidate();foreach(Control c in Controls)InvalidateNavigationChild(c);}
  void InvalidateNavigationChild(Control c){if(c.AccessibleName=="예약")c.Invalidate();foreach(Control child in c.Controls)InvalidateNavigationChild(child);}
  void SaveSettings(){try{SettingsStore.Save(settings);}catch(Exception ex){if(!demo)MessageBox.Show(this,"설정을 저장하지 못했어요. "+ex.Message,"설정 저장",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
  void SaveRepeat(){settings.RepeatMode=Canonical((string)repeatMode.SelectedItem);settings.RepeatTime=repeatTime.Value.ToString("HH:mm");settings.RepeatAction=Canonical((string)repeatAction.SelectedItem);List<int> days=new List<int>();for(int i=0;i<repeatDayBoxes.Count;i++)if(repeatDayBoxes[i].Checked)days.Add(i+1);if(settings.RepeatMode=="요일 선택"&&days.Count==0){MessageBox.Show(this,"요일 선택 반복은 하루 이상 선택해 주세요.","반복 일정",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}settings.RepeatDays=days.ToArray();settings.RepeatEnabled=repeatEnabled.Checked;SaveSettings();if(settings.RepeatEnabled)ScheduleNextRepeat(DateTime.Now);RefreshAdvancedStatus();MessageBox.Show(this,settings.RepeatEnabled?T("반복 일정을 저장했어요."):T("반복 일정을 저장했어요. 사용하려면 반복 예약을 켜 주세요."),T("반복 일정"),MessageBoxButtons.OK,MessageBoxIcon.Information);}
  void SavePresets(){string[] pieces=presetInputs.Text.Split(',');if(pieces.Length!=4){MessageBox.Show(this,"분 단위 숫자 네 개를 쉼표로 구분해 입력해 주세요.","빠른 시간",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}int[] values=new int[4];for(int i=0;i<4;i++)if(!Int32.TryParse(pieces[i].Trim(),out values[i])||values[i]<1||values[i]>10080){MessageBox.Show(this,"각 값은 1분부터 10,080분 사이여야 해요.","빠른 시간",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}settings.Presets=values;SaveSettings();p10.Text=PresetText(values[0]);p30.Text=PresetText(values[1]);p60.Text=PresetText(values[2]);p120.Text=PresetText(values[3]);p10.Tag=p10.Text;p30.Tag=p30.Text;p60.Tag=p60.Text;p120.Tag=p120.Text;RefreshView("빠른 시간 버튼을 저장했어요.");}
  string PresetText(int m){return m>=60&&m%60==0?(m/60)+"시간":m+"분";}
  void SaveConditions(){settings.GuardBattery=guardBattery.Checked;settings.BatteryLimit=(int)batteryLimit.Value;settings.RequireAC=requireAC.Checked;settings.RequireIdle=requireIdle.Checked;settings.IdleMinutes=(int)idleMinutes.Value;settings.GuardTimeoutMinutes=(int)guardTimeout.Value;SaveSettings();RefreshAdvancedStatus();}
  void SaveSystemOptions(){settings.StartWithWindows=startWithWindows.Checked;settings.WakeEnabled=wakeEnabled.Checked;settings.TrayCountdown=trayCountdown.Checked;SetStartup(settings.StartWithWindows);SaveSettings();UpdateWakeTask();RefreshView(null);RefreshAdvancedStatus();}
  void SetStartup(bool enabled){if(demo){settings.StartWithWindows=enabled;SaveSettings();return;}try{using(RegistryKey key=Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run",true)??Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run")){if(enabled)key.SetValue("WindowsTimer","\""+Application.ExecutablePath+"\"");else key.DeleteValue("WindowsTimer",false);}settings.StartWithWindows=enabled;SaveSettings();}catch(Exception ex){startWithWindows.Checked=settings.StartWithWindows;MessageBox.Show(this,"자동 실행 설정을 변경하지 못했어요. "+ex.Message,"Windows 시작",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
  void SaveProfile(){string name=profileName.Text.Trim();if(name.Length==0){MessageBox.Show(this,T("프로필 이름을 입력해 주세요."),T("프로필"),MessageBoxButtons.OK,MessageBoxIcon.Information);return;}TimerProfile p=settings.Profiles.Find(x=>String.Equals(x.Name,name,StringComparison.OrdinalIgnoreCase));if(p==null){p=new TimerProfile();settings.Profiles.Add(p);}p.Name=name;p.Action=selectedAction;p.Minutes=minutes;p.Relative=relative;p.AtTime=datePicker.Value;SaveSettings();RefreshProfileList();profileBox.SelectedItem=name;}
  void RefreshProfileList(){if(profileBox==null)return;string old=profileBox.SelectedItem as string;profileBox.Items.Clear();foreach(TimerProfile p in settings.Profiles)profileBox.Items.Add(p.Name);if(old!=null&&profileBox.Items.Contains(old))profileBox.SelectedItem=old;else if(profileBox.Items.Count>0)profileBox.SelectedIndex=0;}
  void ApplyProfile(){if(profileBox==null||profileBox.SelectedItem==null)return;TimerProfile p=settings.Profiles.Find(x=>x.Name==(string)profileBox.SelectedItem);if(p==null)return;if(!p.Relative&&(p.AtTime==DateTime.MinValue||p.AtTime<=DateTime.Now||p.AtTime>DateTime.Now.AddDays(7))){MessageBox.Show(this,T("프로필의 날짜가 지났거나 예약 가능 범위를 벗어났어요."),T("프로필"),MessageBoxButtons.OK,MessageBoxIcon.Information);return;}selectedAction=p.Action;minutes=Math.Max(1,Math.Min(10080,p.Minutes));relative=p.Relative;if(!relative)datePicker.Value=p.AtTime;RefreshView(null);ShowMain();}
  void DeleteProfile(){if(profileBox==null||profileBox.SelectedItem==null)return;string name=(string)profileBox.SelectedItem;settings.Profiles.RemoveAll(x=>x.Name==name);SaveSettings();RefreshProfileList();}
  void AddHistory(string text){settings.History.Insert(0,new HistoryEntry{When=DateTime.Now.ToString("yyyy-MM-dd HH:mm"),Text=text});if(settings.History.Count>100)settings.History.RemoveRange(100,settings.History.Count-100);SaveSettings();RefreshHistory();}
  string TranslateHistory(string text){string[] names={"일회 예약","원격 예약","실행","실행 실패","보호 조건 미충족으로 취소"};foreach(string name in names){string prefix=name+" · ";if(text.StartsWith(prefix,StringComparison.Ordinal)){string rest=text.Substring(prefix.Length);string[] parts=rest.Split(new string[]{" · "},2,StringSplitOptions.None);return T(name)+" · "+T(parts[0])+(parts.Length>1?" · "+parts[1]:"");}}return T(text);}
  void RefreshHistory(){if(historyList==null)return;if(settings.History.Count==0){historyList.Text=T("아직 기록이 없어요.");return;}StringBuilder b=new StringBuilder();foreach(HistoryEntry e in settings.History){b.Append(e.When).Append("  ·  ").Append(TranslateHistory(e.Text)).Append("\r\n");if(b.Length>900)break;}historyList.Text=b.ToString();}
  void RefreshAdvancedStatus(){if(advancedHint==null||!showingAdvanced)return;DateTime now=DateTime.Now;if((now-lastAdvancedRefresh).TotalSeconds<1)return;lastAdvancedRefresh=now;string repeat=settings.RepeatEnabled?(nextRepeatDue.HasValue?T("다음 반복")+": "+nextRepeatDue.Value.ToString(settings.Language=="English"?"MMM dd HH:mm":settings.Language=="한국어"?"MM월 dd일 HH:mm":"MM/dd HH:mm"):T("반복 예약 사용 중")):T("반복 예약 꺼짐");bool ac,known;int battery;string power=SystemState.TryPower(out ac,out battery,out known)?(ac?T("AC 전원 연결"):(battery>=0?T("배터리 잔량")+" "+battery+"%":T("배터리 잔량 알 수 없음"))):T("전원 상태 알 수 없음");advancedHint.Text=repeat+"  ·  "+power+"  ·  "+T("유휴")+" "+((int)SystemState.IdleMinutes())+" "+T("분");}
  void StartRemoteServer(bool showError){if(demo){if(remoteInfo!=null)remoteInfo.Text=T("데모에서는 실제 원격 서버를 시작하지 않아요.");return;}remoteServer.ScheduleCommand=RemoteSchedule;remoteServer.CancelCommand=RemoteCancel;remoteServer.StatusCommand=RemoteStatus;remoteServer.TokensChanged=delegate{if(IsDisposed||!IsHandleCreated)return;try{BeginInvoke(new MethodInvoker(delegate{SaveSettings();RefreshRemoteInfo();}));}catch{}};bool ok=remoteServer.Start(settings.RemoteTokens);if(!ok){if(showError)MessageBox.Show(this,T("원격 연결을 시작하지 못했어요.")+" "+remoteServer.Error+"\n"+T("사설 Wi-Fi 또는 VPN 연결을 확인해 주세요."),T("원격 예약"),MessageBoxButtons.OK,MessageBoxIcon.Warning);settings.RemoteEnabled=false;SaveSettings();if(remoteEnabled!=null&&remoteEnabled.Checked)remoteEnabled.Checked=false;}RefreshRemoteInfo();}
  void RotatePairingCode(){remoteServer.RotatePairing();}
  void RefreshRemoteInfo(){if(remoteInfo==null)return;if(!settings.RemoteEnabled){remoteInfo.Text=T("원격 서버가 꺼져 있어요.");return;}if(remoteServer.Addresses==null||remoteServer.Addresses.Count==0){remoteInfo.Text=T("원격 서버를 사용할 수 없어요.")+(String.IsNullOrEmpty(remoteServer.Error)?"":" "+remoteServer.Error);return;}StringBuilder b=new StringBuilder();b.Append(T("휴대폰 브라우저에 현재 연결된 네트워크 주소를 입력하세요.")).Append("\r\n");int count=Math.Min(3,remoteServer.Addresses.Count);for(int i=0;i<count;i++)b.Append(remoteServer.Addresses[i]).Append("\r\n");if(remoteServer.Addresses.Count>count)b.Append(T("외")).Append(" ").Append(remoteServer.Addresses.Count-count).Append(T("개 주소")).Append("\r\n");b.Append(T("연결 코드")).Append(": ").Append(remoteServer.PairingCode).Append("  ·  5 ").Append(T("분 유효"));remoteInfo.Text=b.ToString();}
  string UpdateStatusKey(){switch(updateDisplayState){case UpdateDisplayState.Checking:return "최신 버전 확인 중...";case UpdateDisplayState.Current:return "최신 버전입니다 · v{0}";case UpdateDisplayState.Downloading:return "새 버전 v{0} 다운로드 중...";case UpdateDisplayState.Ready:return "새 버전 v{0} 준비 완료";case UpdateDisplayState.Waiting:return "예약 종료 후 v{0} 적용";case UpdateDisplayState.Installing:return "새 버전 v{0} 적용 중...";case UpdateDisplayState.Authentication:return "GitHub 로그인이 필요해요. gh auth login을 실행해 주세요.";case UpdateDisplayState.NetworkError:return "업데이트 서버에 연결할 수 없어요.";case UpdateDisplayState.ReleaseError:return "릴리스 정보를 확인할 수 없어요.";case UpdateDisplayState.VerificationError:return "업데이트 파일 검증에 실패했어요.";case UpdateDisplayState.InstallError:return "업데이트 적용에 실패했어요. 기존 버전을 유지합니다.";case UpdateDisplayState.Demo:return "안전 데모에서는 업데이트하지 않아요.";default:return "현재 버전 · v{0}";}}
  void RenderUpdateStatus(){if(updateStatus==null)return;string version=(updateDisplayVersion??UpdateManager.CurrentVersion).ToString(3);updateStatus.Text=String.Format(T(UpdateStatusKey()),version);if(updateButton!=null){updateButton.Text=T(updateCheckRunning?"확인 중...":"업데이트 확인");updateButton.Enabled=!updateCheckRunning&&!updateApplyStarted;}}
  void SetUpdateState(UpdateDisplayState state,Version version){updateDisplayState=state;if(version!=null)updateDisplayVersion=version;RenderUpdateStatus();}
  void BeginUpdateCheck(bool manual){if(demo){SetUpdateState(UpdateDisplayState.Demo,null);return;}if(updateCheckRunning||updateApplyStarted)return;updateCheckRunning=true;SetUpdateState(UpdateDisplayState.Checking,null);ThreadPool.QueueUserWorkItem(delegate{
   UpdateInfo info=null;string downloaded=null;UpdateDisplayState result=UpdateDisplayState.NetworkError;Version version=null;
   try{info=UpdateManager.CheckLatest();version=info.LatestVersion;if(info.IsNewer){PostUpdateUi(delegate{SetUpdateState(UpdateDisplayState.Downloading,version);});downloaded=UpdateManager.DownloadAndVerify(info);result=UpdateDisplayState.Ready;}else result=UpdateDisplayState.Current;}
   catch(UpdateException ex){switch(ex.Failure){case UpdateFailure.Authentication:result=UpdateDisplayState.Authentication;break;case UpdateFailure.InvalidRelease:result=UpdateDisplayState.ReleaseError;break;case UpdateFailure.Verification:result=UpdateDisplayState.VerificationError;break;case UpdateFailure.Install:result=UpdateDisplayState.InstallError;break;default:result=UpdateDisplayState.NetworkError;break;}}
   catch{result=UpdateDisplayState.NetworkError;}
   string readyFile=downloaded;UpdateDisplayState finalState=result;Version finalVersion=version;UpdateInfo finalInfo=info;
   PostUpdateUi(delegate{updateCheckRunning=false;if(readyFile!=null){pendingUpdate=finalInfo;pendingUpdateFile=readyFile;SetUpdateState(UpdateDisplayState.Ready,finalVersion);ApplyPendingUpdate();}else SetUpdateState(finalState,finalVersion);});
  });}
  void PostUpdateUi(MethodInvoker action){if(IsDisposed||!IsHandleCreated)return;try{BeginInvoke(action);}catch{}}
  void ApplyPendingUpdate(){if(updateApplyStarted||String.IsNullOrEmpty(pendingUpdateFile)||pendingUpdate==null)return;DateTime now=DateTime.Now;if(job.Due.HasValue||(repeatJob.Due.HasValue&&repeatJob.Due.Value<=now.AddMinutes(2))){SetUpdateState(UpdateDisplayState.Waiting,pendingUpdate.LatestVersion);return;}
   try{SetUpdateState(UpdateDisplayState.Installing,pendingUpdate.LatestVersion);string digest=UpdateManager.ComputeSha256(pendingUpdateFile);string arguments="--apply-update \""+Application.ExecutablePath+"\" "+Process.GetCurrentProcess().Id.ToString()+" "+digest+(demo?" --demo":"");Process.Start(new ProcessStartInfo{FileName=pendingUpdateFile,Arguments=arguments,WorkingDirectory=Path.GetDirectoryName(Application.ExecutablePath),UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden});updateApplyStarted=true;exiting=true;Close();}
   catch{updateApplyStarted=false;try{File.Delete(pendingUpdateFile);}catch{}pendingUpdateFile=null;pendingUpdate=null;SetUpdateState(UpdateDisplayState.InstallError,null);}
  }
  void CopyRemoteAddress(){if(remoteServer.Addresses==null||remoteServer.Addresses.Count==0){RefreshRemoteInfo();return;}try{Clipboard.SetText(remoteServer.Addresses[0]);}catch{}}
  string RemoteSchedule(string action,int mins){string result="";try{Invoke(new MethodInvoker(delegate{if(job.Due.HasValue){result="이미 일회 예약이 있어요. 먼저 취소한 뒤 다시 요청해 주세요.";return;}selectedAction=action;minutes=mins;relative=true;job.Start(DateTime.Now,DateTime.Now.AddMinutes(mins),action);AddHistory("원격 예약 · "+action+" · "+job.Due.Value.ToString("yyyy-MM-dd HH:mm"));UpdateWakeTask();RefreshView(null);Reveal();result="예약했어요. 실행 60초 전에 PC에서 취소할 수 있습니다.";}));}catch{result="PC 앱에 연결하지 못했어요.";}return result;}
  string RemoteCancel(){string result="";try{Invoke(new MethodInvoker(delegate{if(!job.Due.HasValue){result="취소할 일회 예약이 없습니다.";return;}job.Cancel();AddHistory("원격에서 예약 취소");UpdateWakeTask();RefreshView("원격 요청으로 예약을 취소했어요.");result="예약을 취소했어요.";}));}catch{result="PC 앱에 연결하지 못했어요.";}return result;}
  string RemoteStatus(){string result="";try{Invoke(new MethodInvoker(delegate{Schedule current=CurrentSchedule();result=current==null?(settings.RepeatEnabled&&nextRepeatDue.HasValue?"반복 일정 · 다음 "+nextRepeatDue.Value.ToString("yyyy-MM-dd HH:mm"):"예약 없음"):(current.Action+" · "+current.Due.Value.ToString("yyyy-MM-dd HH:mm"));}));}catch{result="상태를 확인할 수 없습니다.";}return result;}
  void BuildShell(){
   Panel rail=new Panel{Location=new Point(0,0),Size=new Size(88,ClientSize.Height),BackColor=Palette.Deep,Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left};rail.Paint+=delegate(object s,PaintEventArgs e){Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using(LinearGradientBrush bg=new LinearGradientBrush(rail.ClientRectangle,Palette.Brown,Palette.Deep,90))g.FillRectangle(bg,rail.ClientRectangle);using(SolidBrush mark=new SolidBrush(Palette.Clay))using(GraphicsPath p=Shape.RoundRect(new Rectangle(20,23,48,48),16))g.FillPath(mark,p);using(Pen ring=new Pen(Color.FromArgb(250,248,241),2.3f))g.DrawEllipse(ring,33,36,22,22);using(Pen hand=new Pen(Color.FromArgb(250,248,241),2.3f)){g.DrawLine(hand,44,47,44,40);g.DrawLine(hand,44,47,50,50);}using(SolidBrush indicator=new SolidBrush(Color.FromArgb(117,241,205)))g.FillEllipse(indicator,58,60,6,6);};Controls.Add(rail);
   Panel nav=new Panel{Location=new Point(13,124),Size=new Size(62,62),BackColor=Color.Transparent,Cursor=Cursors.Hand,AccessibleName="예약"};nav.Paint+=delegate(object s,PaintEventArgs e){Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;using(GraphicsPath p=Shape.RoundRect(new Rectangle(0,0,62,62),16))using(SolidBrush b=new SolidBrush(showingAdvanced?Color.Transparent:Color.FromArgb(64,Palette.Clay)))g.FillPath(b,p);using(Pen p=new Pen(Color.FromArgb(255,218,194),2f)){g.DrawEllipse(p,20,12,22,22);g.DrawLine(p,31,17,31,23);g.DrawLine(p,31,23,36,26);}using(Font f=UIFont.Make(8,FontStyle.Bold))using(SolidBrush b=new SolidBrush(Color.FromArgb(250,232,218)))using(StringFormat sf=new StringFormat()){sf.Alignment=StringAlignment.Center;sf.LineAlignment=StringAlignment.Far;g.DrawString(T("예약"),f,b,new RectangleF(0,0,62,59),sf);}};nav.Click+=delegate{ShowMain();};nav.MouseClick+=delegate{ShowMain();};rail.Controls.Add(nav);
   advancedNav=AddButton(rail,"고급",13,198,62,57,ButtonTone.Soft,delegate{ShowAdvanced();});advancedNav.Font=UIFont.Make(8,FontStyle.Bold);advancedNav.ForeColor=Color.FromArgb(250,232,218);
   infoButton=AddButton(rail,"정보",18,ClientSize.Height-79,52,44,ButtonTone.Quiet,delegate{ShowAbout();});infoButton.Anchor=AnchorStyles.Bottom|AnchorStyles.Left;infoButton.ForeColor=Color.FromArgb(240,225,211);infoButton.Font=UIFont.Make(9,FontStyle.Regular);
   AddLabel(rail,"v"+UpdateManager.VersionText,15,ClientSize.Height-31,58,18,7,Color.FromArgb(157,144,132),FontStyle.Regular).Anchor=AnchorStyles.Bottom|AnchorStyles.Left;
   AddLabel(this,"윈도우 타이머  /  예약 관리",120,24,560,18,9,Palette.ClayDark,FontStyle.Bold);
   headline=AddLabel(this,"하루의 끝을, 차분하게.",120,45,630,43,26,Palette.Brown,FontStyle.Bold);
   subhead=AddLabel(this,"원하는 시간을 예약하고 편안하게 마무리하세요.",122,91,660,25,11,Palette.Muted,FontStyle.Regular);
   badge=new StatusBadge{Location=new Point(950,48),Size=new Size(230,38),StatusText="예약 대기 중"};Controls.Add(badge);mainContent.Add(badge);
   heroPanel=new HeroPanel(heroImage){Location=new Point(116,144),Size=new Size(556,548)};Controls.Add(heroPanel);mainContent.Add(heroPanel);
   footer=AddLabel(this,"앱이 실행되는 동안 예약이 유지돼요. 절전에서 깨어나면 실행 전에 60초 더 기다려요.",122,712,710,26,9,Palette.Muted,FontStyle.Regular);
  }
  void BuildSettings(){
   CardPanel card=new CardPanel{Location=new Point(695,144),Size=new Size(485,548),Radius=23};Controls.Add(card);mainContent.Add(card);
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
   p10=AddButton(card,PresetText(settings.Presets[0]),27,379,96,39,ButtonTone.Soft,delegate{minutes=settings.Presets[0];relative=true;RefreshView(null);});
   p30=AddButton(card,PresetText(settings.Presets[1]),130,379,96,39,ButtonTone.Soft,delegate{minutes=settings.Presets[1];relative=true;RefreshView(null);});
   p60=AddButton(card,PresetText(settings.Presets[2]),233,379,96,39,ButtonTone.Soft,delegate{minutes=settings.Presets[2];relative=true;RefreshView(null);});
   p120=AddButton(card,PresetText(settings.Presets[3]),336,379,96,39,ButtonTone.Soft,delegate{minutes=settings.Presets[3];relative=true;RefreshView(null);});
   mainButton=AddButton(card,"예약 시작",28,426,429,58,ButtonTone.Primary,delegate{StartJob();});
   cancelButton=AddButton(card,"예약 취소",28,426,205,58,ButtonTone.Soft,delegate{CancelCurrent();});
   extendButton=AddButton(card,"10분 연장",245,426,212,58,ButtonTone.Primary,delegate{ExtendCurrent();});
   dueLabel=AddLabel(card,"실행 전에 60초 안내를 표시해요.",29,489,428,23,9,Palette.Muted,FontStyle.Regular);
   safetyLabel=AddLabel(card,"저장하지 않은 파일이 있으면 Windows가 종료를 막을 수 있어요.",29,514,428,28,8,Palette.Muted,FontStyle.Regular);
  }
  string T(string ko){if(settings==null||settings.Language=="한국어")return ko;Dictionary<string,string[]> d=Translations.Data;string[] v;if(d.TryGetValue(ko,out v)){int i=settings.Language=="English"?0:settings.Language=="日本語"?1:2;return v[i];}if(ko.EndsWith("분")){string n=ko.Substring(0,ko.Length-1);return settings.Language=="English"?n+" min":settings.Language=="日本語"?n+"分":n+"分钟";}if(ko.EndsWith("시간")){string n=ko.Substring(0,ko.Length-2);return settings.Language=="English"?n+" hr":settings.Language=="日本語"?n+"時間":n+"小时";}return ko;}
  internal static string Canonical(string value){if(value==null)return null;foreach(KeyValuePair<string,string[]> p in Translations.Data){if(value==p.Key||Array.IndexOf(p.Value,value)>=0)return p.Key;}return value;}
  void TranslateControls(Control parent){foreach(Control c in parent.Controls){if(c.Tag is string)c.Text=T((string)c.Tag);if(c is ComboBox&&c.Tag is string[]){ComboBox box=(ComboBox)c;int index=box.SelectedIndex;string[] canon=(string[])box.Tag;box.Items.Clear();foreach(string item in canon)box.Items.Add(T(item));if(index>=0&&index<box.Items.Count)box.SelectedIndex=index;}TranslateControls(c);}}
  void ApplyLanguage(){Translations.CurrentLanguage=settings.Language;TranslateControls(this);if(advancedView!=null){TranslateControls(advancedView);SelectAdvancedTab(advancedTab);}if(advancedNav!=null)advancedNav.Text=T("고급");if(p10!=null){p10.Text=T(PresetText(settings.Presets[0]));p30.Text=T(PresetText(settings.Presets[1]));p60.Text=T(PresetText(settings.Presets[2]));p120.Text=T(PresetText(settings.Presets[3]));}if(datePicker!=null)datePicker.CustomFormat=settings.Language=="English"?"yyyy-MM-dd   HH:mm":settings.Language=="日本語"?"yyyy年 MM月 dd日   HH:mm":settings.Language=="简体中文"?"yyyy年 MM月 dd日   HH:mm":"yyyy년 MM월 dd일   HH:mm";if(historyList!=null)RefreshHistory();RefreshRemoteInfo();lastAdvancedRefresh=DateTime.MinValue;RefreshAdvancedStatus();Text=T("윈도우 타이머")+(demo?" · "+T("안전 데모"):"");if(heroPanel!=null)heroPanel.Invalidate();RefreshView(null);Invalidate(true);}
  Schedule CurrentSchedule(){return job.Due.HasValue?job:(repeatJob.Due.HasValue?repeatJob:null);}
  bool CurrentIsRepeat(){return !job.Due.HasValue&&repeatJob.Due.HasValue;}
  void RefreshView(string message){Schedule current=CurrentSchedule();bool active=current!=null;shutdownButton.Selected=selectedAction=="종료";restartButton.Selected=selectedAction=="재부팅";relativeButton.Selected=relative;dateButton.Selected=!relative;
   shutdownButton.Enabled=restartButton.Enabled=relativeButton.Enabled=dateButton.Enabled=minusButton.Enabled=plusButton.Enabled=p10.Enabled=p30.Enabled=p60.Enabled=p120.Enabled=!active;
   minusButton.Visible=plusButton.Visible=number.Visible=valueHint.Visible=p10.Visible=p30.Visible=p60.Visible=p120.Visible=relative&&!active;
   datePicker.Visible=!relative&&!active;
   mainButton.Visible=!active;cancelButton.Visible=extendButton.Visible=active;
   if(active){badge.Active=true;badge.StatusText=T(CurrentIsRepeat()?"반복 예약 · ":"예약됨 · ")+T(current.Action);number.Text=FormatTime(current.Due.Value-DateTime.Now);valueHint.Text=T("남은 시간");datePicker.Visible=false;p10.Visible=p30.Visible=p60.Visible=p120.Visible=false;minusButton.Visible=plusButton.Visible=false;dueLabel.Text=current.Due.Value.ToString(settings.Language=="English"?"yyyy-MM-dd  HH:mm":settings.Language=="한국어"?"yyyy년 MM월 dd일  HH:mm":"yyyy/MM/dd  HH:mm")+" "+T("실행 예정");}
   else {badge.Active=false;badge.StatusText=T(message??"예약 대기 중");number.Text=minutes.ToString("00");valueHint.Text=relative?T("분 뒤 실행"):T("선택한 날짜와 시간에 실행");dueLabel.Text=T(message??(relative?"분 단위로 빠르게 예약할 수 있어요.":"오늘부터 7일 안의 시간을 지정할 수 있어요."));}
   badge.Invalidate(); DateTime? trayDue=job.Due.HasValue?job.Due:repeatJob.Due;string trayText=trayDue.HasValue?(settings.TrayCountdown?FormatTime(trayDue.Value-DateTime.Now):trayDue.Value.ToString("HH:mm"))+" "+T(job.Due.HasValue?job.Action:repeatJob.Action):T("예약 대기 중");if(tray!=null)try{tray.Text=T("윈도우 타이머")+" · "+trayText;}catch{}
   p10.Selected=relative&&minutes==settings.Presets[0];p30.Selected=relative&&minutes==settings.Presets[1];p60.Selected=relative&&minutes==settings.Presets[2];p120.Selected=relative&&minutes==settings.Presets[3];
   Invalidate();
  }
  static string FormatTime(TimeSpan t){if(t<TimeSpan.Zero)t=TimeSpan.Zero;int h=(int)Math.Floor(t.TotalHours);return h.ToString("00")+":"+t.Minutes.ToString("00")+":"+t.Seconds.ToString("00");}
  void StartJob(){try{DateTime now=DateTime.Now;DateTime due=relative?now.AddMinutes(minutes):datePicker.Value;job.Start(now,due,selectedAction);lastTick=now;AddHistory("일회 예약 · "+selectedAction+" · "+due.ToString("yyyy-MM-dd HH:mm"));UpdateWakeTask();RefreshView(null);}catch(ArgumentException e){MessageBox.Show(this,T(e.Message),T("시간을 확인해 주세요"),MessageBoxButtons.OK,MessageBoxIcon.Information);}}
  void Reveal(){Show();WindowState=FormWindowState.Normal;Activate();}
  void ExitApp(){if((job.Due.HasValue||repeatJob.Due.HasValue)&&MessageBox.Show(this,T("앱을 끝내면 현재 예약도 취소돼요. 종료할까요?"),T("예약 취소 및 앱 종료"),MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;job.Cancel();repeatJob.Cancel();exiting=true;Close();}
  void ShowAbout(){using(Form f=new Form()){f.Text=T("윈도우 타이머 정보");f.ClientSize=new Size(410,270);f.FormBorderStyle=FormBorderStyle.FixedDialog;f.MaximizeBox=false;f.MinimizeBox=false;f.StartPosition=FormStartPosition.CenterParent;f.BackColor=Palette.Canvas;f.Font=UIFont.Make(10,FontStyle.Regular);f.Icon=Icon;AddLabel(f,T("윈도우 타이머"),26,22,350,40,22,Palette.Brown,FontStyle.Bold);string aboutText=T("종료·재부팅·절전 예약과 반복 일정, 전원 보호, 프로필, 원격 예약을 지원합니다.")+"\n\n"+T("버전")+" "+UpdateManager.VersionText+"  ·  "+T("개발자")+" Lebi (@cybereun)";AddLabel(f,aboutText,28,78,350,94,10,Palette.Muted,FontStyle.Regular);SoftButton b=new SoftButton(T("닫기"),ButtonTone.Primary){Location=new Point(27,205),Size=new Size(356,42)};b.Click+=delegate{f.Close();};f.Controls.Add(b);f.ShowDialog(this);}}
  void CancelCurrent(){if(job.Due.HasValue){job.Cancel();AddHistory("예약 취소");RefreshView("예약을 취소했어요");}else if(repeatJob.Due.HasValue){repeatJob.Cancel();AddHistory("다가오는 반복 실행 취소");ScheduleNextRepeat(DateTime.Now);RefreshView("이번 반복 실행을 취소했어요");}UpdateWakeTask();}
  void ExtendCurrent(){Schedule current=CurrentSchedule();if(current==null)return;current.Extend();AddHistory("예약을 10분 연장");UpdateWakeTask();RefreshView("예약을 10분 연장했어요");}
  void TickJob(){DateTime now=DateTime.Now;bool resumed=(now-lastTick).TotalSeconds>5;lastTick=now;ProcessSchedule(job,false,now,resumed);ProcessSchedule(repeatJob,true,now,resumed);if(number!=null&&CurrentSchedule()!=null)number.Text=FormatTime(CurrentSchedule().Due.Value-now);RefreshAdvancedStatus();if(pendingUpdateFile!=null&&!job.Due.HasValue&&(!repeatJob.Due.HasValue||repeatJob.Due.Value>now.AddMinutes(2)))ApplyPendingUpdate();}
  void ProcessSchedule(Schedule schedule,bool repeating,DateTime now,bool resumed){if(!schedule.Due.HasValue)return;
   if(schedule.Due.Value<=now.AddSeconds(60)){string reason;if(!ConditionsMet(out reason)){if(schedule.GuardTimedOut(now,settings.GuardTimeoutMinutes)){schedule.Cancel();AddHistory("보호 조건 미충족으로 취소 · "+reason);if(repeating)ScheduleNextRepeat(now);RefreshView("보호 조건을 충족하지 않아 예약을 취소했어요");UpdateWakeTask();return;}schedule.Defer(now.AddMinutes(5));RefreshView("보호 조건 대기 중 · "+reason);UpdateWakeTask();return;}schedule.ClearGuardWait();}
   int result=schedule.Tick(now,resumed);if(result==0)return;
   if(result==1){string actionText=T(schedule.Action);RefreshView(null);dueLabel.Text=T("곧 Windows ")+actionText+T("돼요 · 지금 취소할 수 있어요");Reveal();if(tray!=null)tray.ShowBalloonTip(5000,"Windows "+actionText+" "+T("예정"),T("실행 60초 전이에요. 원하지 않으면 예약을 취소해 주세요."),ToolTipIcon.Warning);System.Media.SystemSounds.Exclamation.Play();return;}
   if(result==2){string requested=schedule.Action;DateTime when=schedule.Due.Value;schedule.Cancel();if(repeating&&settings.RepeatEnabled)ScheduleNextRepeat(now.AddSeconds(1));UpdateWakeTask();RefreshView(null);AddHistory("실행 · "+requested+" · "+when.ToString("yyyy-MM-dd HH:mm"));if(demo){RefreshView("데모 완료 · 실제 종료 명령은 실행하지 않았어요");return;}try{ExecuteAction(requested);RefreshView(T("Windows에 ")+T(requested)+T("를 요청했어요"));}catch(Exception ex){AddHistory("실행 실패 · "+requested);Reveal();MessageBox.Show(this,ex.Message,"실행하지 못했어요",MessageBoxButtons.OK,MessageBoxIcon.Error);}}
  }
  bool ConditionsMet(out string reason){reason="";bool ac;int percent;bool known;if(!SystemState.TryPower(out ac,out percent,out known)){if(settings.RequireAC||settings.GuardBattery){reason="전원 상태 확인 불가";return false;}}if(settings.RequireAC&&(!known||!ac)){reason="AC 전원 미연결";return false;}if(settings.GuardBattery&&(percent<0||percent<settings.BatteryLimit)){reason="배터리 잔량 기준 미달";return false;}if(settings.RequireIdle&&SystemState.IdleMinutes()<settings.IdleMinutes){reason="사용자 입력 감지";return false;}return true;}
  void ExecuteAction(string requested){if(requested=="절전"){if(!Application.SetSuspendState(PowerState.Suspend,false,false))throw new Exception("Windows가 절전 요청을 거부했어요.");return;}if(requested=="최대 절전"){ProcessStartInfo h=new ProcessStartInfo("shutdown.exe","/h"){UseShellExecute=false,CreateNoWindow=true};using(Process p=Process.Start(h)){p.WaitForExit(5000);if(!p.HasExited||p.ExitCode!=0)throw new Exception("Windows가 최대 절전 요청을 처리하지 못했어요.");}return;}if(requested!="종료"&&requested!="재부팅")throw new ArgumentException("지원하지 않는 동작입니다.");string args=requested=="종료"?"/s /t 0":"/r /t 0";ProcessStartInfo info=new ProcessStartInfo("shutdown.exe",args){UseShellExecute=false,CreateNoWindow=true};using(Process p=Process.Start(info)){p.WaitForExit(5000);if(!p.HasExited||p.ExitCode!=0)throw new Exception("Windows가 요청을 처리하지 못했어요.");}}
  void ScheduleNextRepeat(DateTime after){repeatJob.Cancel();nextRepeatDue=null;if(!settings.RepeatEnabled||String.IsNullOrEmpty(settings.RepeatTime))return;DateTime? due=Schedule.NextOccurrence(after,settings.RepeatMode,settings.RepeatTime,settings.RepeatDays);if(!due.HasValue){RefreshAdvancedStatus();return;}repeatJob.Start(after,due.Value,settings.RepeatAction??"종료");nextRepeatDue=due;UpdateWakeTask();RefreshAdvancedStatus();}
  void UpdateWakeTask(){if(demo)return;if(!settings.WakeEnabled){WakeTask.Remove();return;}DateTime? due=job.Due;if(repeatJob.Due.HasValue&&(!due.HasValue||repeatJob.Due.Value<due.Value))due=repeatJob.Due;if(due.HasValue)WakeTask.Set(due.Value);else WakeTask.Remove();}
 }

 static class WakeTask {
  const string Name="WindowsTimer-Wake";
  public static void Set(DateTime due){try{if(due<=DateTime.Now.AddSeconds(5)){Remove();return;}string exe=System.Security.SecurityElement.Escape(Application.ExecutablePath);string user=System.Security.SecurityElement.Escape(System.Security.Principal.WindowsIdentity.GetCurrent().Name);string boundary=due.ToString("yyyy-MM-dd'T'HH:mm:ss");string xml="<?xml version=\"1.0\" encoding=\"UTF-16\"?><Task version=\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\"><RegistrationInfo><Description>Wake WindowsTimer at a scheduled time.</Description></RegistrationInfo><Triggers><TimeTrigger><StartBoundary>"+boundary+"</StartBoundary><Enabled>true</Enabled></TimeTrigger></Triggers><Principals><Principal id=\"Author\"><UserId>"+user+"</UserId><LogonType>InteractiveToken</LogonType><RunLevel>LeastPrivilege</RunLevel></Principal></Principals><Settings><MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy><DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries><StopIfGoingOnBatteries>false</StopIfGoingOnBatteries><WakeToRun>true</WakeToRun><StartWhenAvailable>true</StartWhenAvailable><ExecutionTimeLimit>PT1M</ExecutionTimeLimit><Enabled>true</Enabled></Settings><Actions Context=\"Author\"><Exec><Command>"+exe+"</Command><Arguments>--wake</Arguments><WorkingDirectory>"+System.Security.SecurityElement.Escape(Path.GetDirectoryName(Application.ExecutablePath))+"</WorkingDirectory></Exec></Actions></Task>";string file=Path.Combine(Path.GetTempPath(),"WindowsTimer-Wake-"+Guid.NewGuid().ToString("N")+".xml");try{File.WriteAllText(file,xml,Encoding.Unicode);Run("schtasks.exe","/Create /TN \""+Name+"\" /XML \""+file+"\" /F");}finally{try{File.Delete(file);}catch{}}}catch{} }
  public static void Remove(){try{Run("schtasks.exe","/Delete /TN \""+Name+"\" /F");}catch{}}
  static void Run(string exe,string args){using(Process p=Process.Start(new ProcessStartInfo(exe,args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WindowStyle=ProcessWindowStyle.Hidden})){p.WaitForExit(10000);}}
 }

 static class Translations {
  public static string CurrentLanguage="한국어";
  public static readonly Dictionary<string,string[]> Data=new Dictionary<string,string[]>{
   {"예약",new string[]{"Schedule","予約","预约"}},{"고급",new string[]{"Advanced","詳細","高级"}},{"정보",new string[]{"About","情報","关于"}},{"예약 대기 중",new string[]{"Ready","待機中","等待预约"}},{"예약 시작",new string[]{"Schedule","予約する","开始预约"}},{"예약 취소",new string[]{"Cancel schedule","予約を取り消す","取消预约"}},{"10분 연장",new string[]{"Add 10 minutes","10分延長","延长10分钟"}},{"종료",new string[]{"Shut down","シャットダウン","关机"}},{"월",new string[]{"Mon","月","一"}},{"화",new string[]{"Tue","火","二"}},{"수",new string[]{"Wed","水","三"}},{"목",new string[]{"Thu","木","四"}},{"금",new string[]{"Fri","金","五"}},{"토",new string[]{"Sat","土","六"}},{"일",new string[]{"Sun","日","日"}},
   {"윈도우 타이머  /  예약 관리",new string[]{"Windows Timer  /  Schedule","Windows Timer  /  予約管理","Windows Timer  /  计划管理"}},{"하루의 끝을, 차분하게.",new string[]{"End your day calmly.","一日を穏やかに。","让一天平静地结束。"}},{"원하는 시간을 예약하고 편안하게 마무리하세요.",new string[]{"Schedule a time and wind down.","時間を予約して、ゆっくり休みましょう。","设定时间，安心结束一天。"}},{"앱이 실행되는 동안 예약이 유지돼요. 절전에서 깨어나면 실행 전에 60초 더 기다려요.",new string[]{"Schedules stay active while the app runs. After waking, it waits 60 seconds.","アプリ実行中は予約が維持されます。スリープ復帰後は60秒待ちます。","应用运行期间预约有效。电脑从睡眠恢复后会再等待60秒。"}},
   {"01  /  작업 선택",new string[]{"01  /  ACTION","01  /  操作","01  /  操作"}},{"무엇을 할까요?",new string[]{"What should happen?","どうしますか？","要执行什么操作？"}},{"재부팅",new string[]{"Restart","再起動","重启"}},{"02  /  시간 설정",new string[]{"02  /  TIME","02  /  時刻","02  /  时间"}},{"언제 마칠까요?",new string[]{"When should it happen?","いつ実行しますか？","何时执行？"}},{"분 후",new string[]{"In minutes","分後","分钟后"}},{"날짜 · 시간",new string[]{"Date · time","日付 · 時刻","日期 · 时间"}},{"분 뒤 실행",new string[]{"Run in","分後に実行","分钟后执行"}},
   {"고급 설정",new string[]{"Advanced settings","詳細設定","高级设置"}},{"반복 예약과 안전 조건을 설정해요. 메인 예약 화면은 현재 설정을 그대로 유지합니다.",new string[]{"Configure recurring schedules and safeguards. The main screen stays the same.","繰り返し予約と安全条件を設定します。メイン画面はそのままです。","设置重复计划和安全条件，主预约界面保持不变。"}},{"반복 일정",new string[]{"Recurring schedule","繰り返し予約","重复计划"}},{"매일 또는 선택한 요일에 반복 실행",new string[]{"Repeat daily or on selected weekdays","毎日または選択した曜日に実行","每天或在选定星期重复"}},{"매일",new string[]{"Daily","毎日","每天"}},{"요일 선택",new string[]{"Selected days","曜日を選択","选择星期"}},{"빠른 시간 버튼",new string[]{"Quick time buttons","クイック時間ボタン","快捷时间按钮"}},{"메인 화면의 네 버튼에 표시할 분 단위를 입력하세요. 예: 10, 30, 60, 120",new string[]{"Set the four minute values shown on the main screen, e.g. 10, 30, 60, 120.","メイン画面の4つのボタンに表示する分数を入力します。例: 10, 30, 60, 120","设置主界面四个快捷按钮的分钟数，例如 10、30、60、120。"}},{"빠른 시간 저장",new string[]{"Save quick times","クイック時間を保存","保存快捷时间"}},
   {"전원 · 사용 상태 보호",new string[]{"Power and activity safeguards","電源・使用状況の保護","电源与使用状态保护"}},{"배터리 잔량이 기준보다 낮으면 대기",new string[]{"Wait if battery is below the limit","バッテリー残量が基準未満なら待機","电量低于阈值时等待"}},{"AC 전원이 연결되어 있을 때만 실행",new string[]{"Run only on AC power","AC電源接続時のみ実行","仅在接通交流电时执行"}},{"입력 없이 지정 시간 동안 유휴 상태일 때 실행",new string[]{"Run only after the PC is idle","指定時間アイドル状態の場合のみ実行","电脑空闲达到指定时间后执行"}},{"보호 조건 저장",new string[]{"Save safeguards","保護条件を保存","保存保护条件"}},{"Windows · 알림 설정",new string[]{"Windows and notifications","Windows・通知","Windows 与通知"}},{"Windows 로그인 시 자동 실행",new string[]{"Start with Windows","Windows起動時に実行","Windows 登录时启动"}},{"절전 중 예약 시각에 PC 깨우기",new string[]{"Wake the PC for a schedule","予約時刻にPCを起動","在预约时间唤醒电脑"}},{"작업 표시줄 아이콘에 남은 시간 표시",new string[]{"Show countdown in the tray","タスクトレイに残り時間を表示","在系统托盘显示倒计时"}},{"Windows 설정 저장",new string[]{"Save Windows settings","Windows設定を保存","保存 Windows 设置"}},
   {"한 번 예약 · 다른 동작",new string[]{"One-time schedule · other actions","1回予約・その他の操作","单次预约 · 其他操作"}},{"절전",new string[]{"Sleep","スリープ","睡眠"}},{"최대 절전",new string[]{"Hibernate","休止状態","休眠"}},{"분 뒤",new string[]{"minutes","分後","分钟后"}},{"이 동작 예약",new string[]{"Schedule this action","この操作を予約","预约此操作"}},{"프로필",new string[]{"Profiles","プロファイル","配置"}},{"현재 메인 화면의 시간과 동작을 저장해 나중에 다시 적용해요.",new string[]{"Save the current main-screen time and action for later.","メイン画面の時刻と操作を保存して、後で再適用します。","保存主界面的时间和操作设置，以便稍后应用。"}},{"현재 설정 저장",new string[]{"Save current settings","現在の設定を保存","保存当前设置"}},{"적용",new string[]{"Apply","適用","应用"}},{"삭제",new string[]{"Delete","削除","删除"}},{"예약 기록",new string[]{"Schedule history","予約履歴","预约记录"}},{"기록 지우기",new string[]{"Clear history","履歴を消去","清除记录"}},{"아직 기록이 없어요.",new string[]{"No history yet.","履歴はありません。","暂无记录。"}},
   {"언어 · 업데이트",new string[]{"Language and updates","言語・更新","语言与更新"}},{"화면 언어",new string[]{"Display language","表示言語","显示语言"}},{"한국어",new string[]{"Korean","韓国語","韩语"}},{"English",new string[]{"English","英語","英语"}},{"日本語",new string[]{"Japanese","日本語","日语"}},{"简体中文",new string[]{"Simplified Chinese","簡体字中国語","简体中文"}},{"언어 적용",new string[]{"Apply language","言語を適用","应用语言"}},{"새 버전 확인",new string[]{"Check for updates","更新を確認","检查更新"}},
   {"휴대폰 · 다른 PC에서 원격 예약",new string[]{"Remote scheduling from phone or PC","スマホ・別のPCから遠隔予約","通过手机或其他电脑远程预约"}},{"모든 기기 연결 해제",new string[]{"Disconnect all devices","すべての端末を解除","断开所有设备"}},
   {"윈도우 타이머",new string[]{"Windows Timer","Windows Timer","Windows Timer"}},{"예약됨 ·",new string[]{"Scheduled ·","予約済み ·","已预约 ·"}},{"반복 예약 ·",new string[]{"Recurring ·","繰り返し ·","重复 ·"}},{"남은 시간",new string[]{"Time remaining","残り時間","剩余时间"}},{"선택한 날짜와 시간에 실행",new string[]{"Run at the selected date and time","指定した日時に実行","在选定日期和时间执行"}},{"분 단위로 빠르게 예약할 수 있어요.",new string[]{"Schedule quickly by minutes.","分単位ですばやく予約できます。","可以按分钟快速预约。"}},{"오늘부터 7일 안의 시간을 지정할 수 있어요.",new string[]{"Choose a time within the next 7 days.","今日から7日以内の時刻を指定できます。","可以指定未来7天内的时间。"}},{"실행 예정",new string[]{"scheduled","実行予定","执行"}},{"실행 전에 60초 안내를 표시해요.",new string[]{"A 60-second warning appears before the action.","実行前に60秒の通知を表示します。","执行前会显示60秒提醒。"}},{"저장하지 않은 파일이 있으면 Windows가 종료를 막을 수 있어요.",new string[]{"Windows may block shutdown if files are unsaved.","未保存のファイルがあるとWindowsが終了を止める場合があります。","如果有未保存的文件，Windows 可能会阻止关机。"}},{"닫기",new string[]{"Close","閉じる","关闭"}},{"윈도우 타이머 정보",new string[]{"About Windows Timer","Windows Timerについて","关于 Windows Timer"}},{"반복 일정 저장",new string[]{"Save recurring schedule","繰り返し予約を保存","保存重复计划"}},{"다음 실행은 60초 알림과 취소 기능을 거쳐요.",new string[]{"A 60-second warning and cancellation are available before each run.","毎回、実行前に60秒の通知と取り消しができます。","每次执行前都会提供60秒提醒和取消机会。"}},{"깨우기는 PC·BIOS의 절전 타이머 지원이 필요해요.",new string[]{"Wake timers must be supported by the PC and BIOS.","PCとBIOSがスリープ解除タイマーに対応している必要があります。","电脑和 BIOS 必须支持唤醒定时器。"}},{"한 번 예약도 메인 화면에서 취소하거나 10분 연장할 수 있어요.",new string[]{"One-time schedules can be canceled or extended by 10 minutes on the main screen.","1回予約もメイン画面から取り消すか10分延長できます。","单次预约也可以在主界面取消或延长10分钟。"}},{"새 버전은 비공개 GitHub 릴리스에서 확인할 수 있어요.",new string[]{"Check new versions on the private GitHub releases page.","新しいバージョンは非公開GitHubリリースで確認できます。","可在私有 GitHub Releases 页面查看新版本。"}},
    {"반복 예약 꺼짐",new string[]{"Recurring schedule off","繰り返し予約はオフ","重复计划已关闭"}},{"반복 예약 사용 중",new string[]{"Recurring schedule on","繰り返し予約を使用中","重复计划已启用"}},{"다음 반복",new string[]{"Next run","次回","下次执行"}},{"AC 전원 연결",new string[]{"AC power connected","AC電源に接続","已连接交流电源"}},{"배터리 잔량",new string[]{"Battery","バッテリー","电量"}},{"배터리 잔량 알 수 없음",new string[]{"Battery level unavailable","バッテリー残量を確認できません","无法读取电量"}},{"전원 상태 알 수 없음",new string[]{"Power status unavailable","電源状態を確認できません","无法读取电源状态"}},{"유휴",new string[]{"Idle","アイドル","空闲"}},{"프로필 이름을 입력해 주세요.",new string[]{"Enter a profile name.","プロファイル名を入力してください。","请输入配置名称。"}},{"프로필의 날짜가 지났거나 예약 가능 범위를 벗어났어요.",new string[]{"The profile date has passed or is outside the scheduling window.","プロファイルの日付が過ぎたか、予約可能な範囲外です。","配置日期已过或超出可预约范围。"}},{"반복 일정을 저장했어요.",new string[]{"Recurring schedule saved.","繰り返し予約を保存しました。","重复计划已保存。"}},{"반복 일정을 저장했어요. 사용하려면 반복 예약을 켜 주세요.",new string[]{"Recurring settings saved. Turn on the recurring schedule to use them.","繰り返し設定を保存しました。使用するには繰り返し予約をオンにしてください。","重复设置已保存。请启用重复计划以使用。"}},{"첫 번째 연결 주소 복사",new string[]{"Copy first connection address","最初の接続先をコピー","复制第一个连接地址"}},{"모든 기기 연결 해제 · 코드 재발급",new string[]{"Disconnect all devices · renew code","全端末を解除・コードを再発行","断开所有设备 · 重新生成代码"}},{"원격 서버가 꺼져 있어요.",new string[]{"Remote access is off.","リモート接続はオフです。","远程访问已关闭。"}},{"휴대폰 브라우저에 현재 연결된 네트워크 주소를 입력하세요.",new string[]{"Open one of these network addresses in your phone browser.","スマホのブラウザーで次のネットワークアドレスを開いてください。","在手机浏览器中打开以下网络地址。"}},{"연결 코드",new string[]{"Pairing code","接続コード","配对码"}},{"분 유효",new string[]{"minutes","分間有効","分钟有效"}},{"외",new string[]{"and","ほか","另有"}},{"개 주소",new string[]{"addresses","件のアドレス","个地址"}},{"원격 서버를 사용할 수 없어요.",new string[]{"Remote server is unavailable.","リモートサーバーを利用できません。","远程服务器不可用。"}},{"데모에서는 실제 원격 서버를 시작하지 않아요.",new string[]{"The demo does not start a real remote server.","デモでは実際のリモートサーバーを起動しません。","演示模式不会启动真实远程服务器。"}},{"같은 네트워크 또는 VPN에서 원격 접속 허용",new string[]{"Allow remote access on LAN or VPN","LANまたはVPNからのアクセスを許可","允许通过局域网或 VPN 远程访问"}},{"외부 접속은 기기 간 VPN 연결이 필요해요. 공용 Wi-Fi에서는 사용하지 마세요.",new string[]{"For access from outside, connect both devices to a VPN. Use trusted Wi-Fi on a LAN.","外部接続には両方の端末でVPNが必要です。LANでは信頼できるWi-Fiを使用してください。","外部连接需要两台设备连接同一 VPN。局域网连接请使用可信 Wi-Fi。"}},{"LAN은 암호화되지 않으므로 신뢰할 수 있는 사설망에서만 사용하세요. 외부에서는 두 기기를 VPN에 연결하세요.",new string[]{"LAN traffic is not encrypted. Use only a trusted private network; connect both devices through a VPN for internet access.","LAN通信は暗号化されません。信頼できるプライベートネットワークのみで使用し、外部接続では両方の端末をVPNに接続してください。","局域网通信未加密。请仅在可信的专用网络中使用；通过互联网访问时，请让两台设备连接 VPN。"}},{"곧 Windows ",new string[]{"Windows will soon ","まもなくWindowsが","Windows 即将"}},{"돼요 · 지금 취소할 수 있어요",new string[]{" · you can cancel now","します · 今すぐ取り消せます"," · 现在可以取消"}},{"실행 60초 전이에요. 원하지 않으면 예약을 취소해 주세요.",new string[]{"The action is due in 60 seconds. Cancel the schedule if you changed your mind.","実行まで60秒です。中止する場合は予約を取り消してください。","还有 60 秒执行。如需取消，请取消预约。"}},{"예정",new string[]{"scheduled","予定","即将执行"}},{"보호 조건 대기 중",new string[]{"Waiting for safeguards","保護条件を確認中","正在等待保护条件"}},{"보호 조건을 충족하지 않아 예약을 취소했어요",new string[]{"The schedule was canceled because its safeguards were not met.","保護条件を満たさなかったため、予約を取り消しました。","未满足保护条件，已取消预约。"}},{"예약을 취소했어요",new string[]{"Schedule canceled.","予約を取り消しました。","预约已取消。"}},{"이번 반복 실행을 취소했어요",new string[]{"Canceled this occurrence.","今回の実行を取り消しました。","已取消本次执行。"}},{"예약을 10분 연장했어요",new string[]{"Schedule extended by 10 minutes.","予約を10分延長しました。","预约已延长 10 分钟。"}},{"데모 완료 · 실제 종료 명령은 실행하지 않았어요",new string[]{"Demo complete · no Windows power command was issued.","デモが完了しました · Windowsへの電源命令は実行していません。","演示完成 · 未执行任何 Windows 电源命令。"}},{"시간을 확인해 주세요",new string[]{"Check the time","時刻を確認してください","请检查时间"}},{"현재보다 뒤의 시간을 선택해 주세요.",new string[]{"Choose a time in the future.","現在より後の時刻を選択してください。","请选择未来的时间。"}},{"원격 연결을 시작하지 못했어요.",new string[]{"Could not start remote access.","リモート接続を開始できませんでした。","无法启动远程访问。"}},{"사설 Wi-Fi 또는 VPN 연결을 확인해 주세요.",new string[]{"Check your private Wi-Fi or VPN connection.","プライベートWi-FiまたはVPN接続を確認してください。","请检查专用 Wi-Fi 或 VPN 连接。"}},{"기준 %",new string[]{"Limit %","基準 %","阈值 %"}},{"유휴 분",new string[]{"Idle minutes","アイドル時間（分）","空闲分钟数"}},{"최대 대기 분",new string[]{"Maximum wait (min)","最大待機時間（分）","最长等待（分钟）"}},{"조건을 충족하지 못하면 예약을 보류하고 제한 시간 후 취소해요.",new string[]{"If safeguards are not met, the schedule waits and is canceled after the limit.","条件を満たさない場合は予約を保留し、制限時間後に取り消します。","如果未满足保护条件，预约将等待并在超时后取消。"}},{"종료·재부팅 외에도 절전과 최대 절전을 예약할 수 있어요.",new string[]{"You can also schedule sleep and hibernate.","シャットダウン・再起動のほか、スリープと休止状態を予約できます。","除关机和重启外，还可预约睡眠和休眠。"}},{"프로필은 이 Windows 사용자 계정에만 저장돼요.",new string[]{"Profiles are stored only for this Windows account.","プロファイルはこのWindowsユーザーアカウントにのみ保存されます。","配置仅保存在当前 Windows 用户账户中。"}},{"창 열기",new string[]{"Open window","ウィンドウを開く","打开窗口"}},{"앱 종료",new string[]{"Exit app","アプリを終了","退出应用"}},{"예약을 유지하고 있어요. 트레이 메뉴에서 취소할 수 있어요.",new string[]{"The schedule is still active. Cancel it from the tray menu.","予約は維持されています。トレイメニューから取り消せます。","预约仍在运行。可从托盘菜单取消。"}},{"트레이 아이콘에서 다시 열거나 종료할 수 있어요.",new string[]{"Reopen or exit from the tray icon.","トレイアイコンから再表示または終了できます。","可从托盘图标重新打开或退出。"}},{"앱을 끝내면 현재 예약도 취소돼요. 종료할까요?",new string[]{"Exiting the app will cancel active schedules. Exit anyway?","アプリを終了すると予約も取り消されます。終了しますか？","退出应用也会取消当前预约。仍要退出吗？"}},{"예약 취소 및 앱 종료",new string[]{"Cancel schedules and exit","予約を取り消して終了","取消预约并退出"}},{"종료·재부팅·절전 예약과 반복 일정, 전원 보호, 프로필, 원격 예약을 지원합니다.",new string[]{"Schedule shutdown, restart, sleep, hibernate, recurring actions, safeguards, profiles, and remote actions.","シャットダウン・再起動・スリープ・休止状態、繰り返し予約、保護条件、プロファイル、リモート予約に対応します。","支持预约关机、重启、睡眠、休眠、重复计划、保护条件、配置和远程预约。"}},{"버전",new string[]{"Version","バージョン","版本"}},{"개발자",new string[]{"Developer","開発者","开发者"}},{"일회 예약",new string[]{"One-time schedule","1回予約","单次预约"}},{"원격 예약",new string[]{"Remote schedule","リモート予約","远程预约"}},{"실행",new string[]{"Run","実行","执行"}},{"실행 실패",new string[]{"Run failed","実行失敗","执行失败"}},{"보호 조건 미충족으로 취소",new string[]{"Canceled by safeguards","保護条件により取り消し","因保护条件未满足而取消"}},{"다가오는 반복 실행 취소",new string[]{"Canceled the next recurring run","次回の繰り返し実行を取り消し","已取消下一次重复执行"}},{"원격에서 예약 취소",new string[]{"Canceled remotely","リモートから予約を取り消し","已从远程取消预约"}},{"전원 상태 확인 불가",new string[]{"Power status unavailable","電源状態を確認できません","无法读取电源状态"}},{"AC 전원 미연결",new string[]{"AC power is not connected","AC電源に接続されていません","未连接交流电源"}},{"배터리 잔량 기준 미달",new string[]{"Battery is below the limit","バッテリー残量が基準未満","电量低于阈值"}},{"사용자 입력 감지",new string[]{"User input detected","ユーザー入力を検出","检测到用户输入"}},{"Windows에 ",new string[]{"Windows ","Windowsに","Windows"}},{"를 요청했어요",new string[]{" request sent.","に要求しました。","已发送请求。"}},{"안전 데모",new string[]{"Safe demo","安全デモ","安全演示"}},{"차분한 하루의 마무리",new string[]{"End your day calmly.","一日を穏やかに。","让一天平静地结束。"}},{"원하는 순간을 정하면 마지막 60초를 알려드려요",new string[]{"Choose a time; we will warn you 60 seconds before.","時刻を決めると、60秒前にお知らせします。","设定时间后，我们会在执行前60秒提醒。"}}
  };
  static Translations(){
   Data.Add("일정",new string[]{"Schedule","スケジュール","日程"});
   Data.Add("보호 · 시스템",new string[]{"Safety · system","保護・システム","保护 · 系统"});
   Data.Add("프로필 · 기록",new string[]{"Profiles · history","プロファイル・履歴","配置 · 记录"});
   Data.Add("원격 · 언어",new string[]{"Remote · language","リモート・言語","远程 · 语言"});
   Data.Add("업데이트 확인",new string[]{"Check for updates","更新を確認","检查更新"});
   Data.Add("확인 중...",new string[]{"Checking...","確認中...","正在检查..."});
   Data.Add("현재 버전 · v{0}",new string[]{"Current version · v{0}","現在のバージョン · v{0}","当前版本 · v{0}"});
   Data.Add("최신 버전 확인 중...",new string[]{"Checking for the latest version...","最新バージョンを確認中...","正在检查最新版本..."});
   Data.Add("최신 버전입니다 · v{0}",new string[]{"You're up to date · v{0}","最新バージョンです · v{0}","已是最新版本 · v{0}"});
   Data.Add("새 버전 v{0} 다운로드 중...",new string[]{"Downloading v{0}...","v{0}をダウンロード中...","正在下载 v{0}..."});
   Data.Add("새 버전 v{0} 준비 완료",new string[]{"v{0} is ready to install","v{0}の準備ができました","v{0} 已准备就绪"});
   Data.Add("예약 종료 후 v{0} 적용",new string[]{"v{0} will install after the schedule","予約終了後に v{0} を適用します","预约结束后将安装 v{0}"});
   Data.Add("새 버전 v{0} 적용 중...",new string[]{"Installing v{0}...","v{0}を適用中...","正在安装 v{0}..."});
   Data.Add("GitHub 로그인이 필요해요. gh auth login을 실행해 주세요.",new string[]{"Sign in to GitHub with `gh auth login`.","`gh auth login` で GitHub にログインしてください。","请运行 `gh auth login` 登录 GitHub。"});
   Data.Add("업데이트 서버에 연결할 수 없어요.",new string[]{"Could not connect to the update server.","更新サーバーに接続できませんでした。","无法连接到更新服务器。"});
   Data.Add("릴리스 정보를 확인할 수 없어요.",new string[]{"Could not read the release information.","リリース情報を確認できませんでした。","无法读取发布信息。"});
   Data.Add("업데이트 파일 검증에 실패했어요.",new string[]{"The downloaded update failed verification.","ダウンロードした更新ファイルの検証に失敗しました。","下载的更新文件验证失败。"});
   Data.Add("업데이트 적용에 실패했어요. 기존 버전을 유지합니다.",new string[]{"Update failed. The current version was kept.","更新に失敗したため、現在のバージョンを維持します。","更新失败，已保留当前版本。"});
   Data.Add("안전 데모에서는 업데이트하지 않아요.",new string[]{"Updates are disabled in the safe demo.","安全デモでは更新しません。","安全演示模式不会更新。"});
  }
  public static string Get(string korean,string language){if(String.IsNullOrEmpty(language)||language=="한국어")return korean;string[] value;if(Data.TryGetValue(korean,out value))return value[language=="English"?0:language=="日本語"?1:2];return korean;}
 }

 static class Program {
  [STAThread] static int Main(string[] args){if(args.Length>0&&args[0]=="--apply-update")return UpdateManager.ApplyUpdateCommand(args);if(args.Length>0&&args[0]=="--cleanup-update"){UpdateManager.CompleteUpdateCleanup(args);args=Array.IndexOf(args,"--demo")>=0?new string[]{"--demo"}:new string[0];}if(Array.IndexOf(args,"--self-test")>=0)return Tests.Run();if(Array.IndexOf(args,"--wake")>=0)return 0;bool demo=Array.IndexOf(args,"--demo")>=0;bool created;string mutexName=demo?"Local\\WindowsTimer.Demo":"Local\\WindowsTimer.Desktop";using(Mutex mutex=new Mutex(true,mutexName,out created)){if(!created){MessageBox.Show("윈도우 타이머가 이미 실행 중이에요. 작업 표시줄의 아이콘을 확인해 주세요.","윈도우 타이머");return 0;}Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new MainForm(demo));}UIFont.Cleanup();return 0;}
 }
 static class Tests {
  static int checks;
  static void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
  public static int Run(){try{checks=0;DateTime now=new DateTime(2026,10,21,12,0,0);Schedule s=new Schedule();bool rejected=false;try{s.Start(now,now,"종료");}catch(ArgumentException){rejected=true;}Check(rejected,"past rejected");bool invalidActionRejected=false;try{s.Start(now,now.AddMinutes(1),"unknown");}catch(ArgumentException){invalidActionRejected=true;}Check(invalidActionRejected,"unsupported action rejected");s.Start(now,now.AddMinutes(2),"종료");Check(s.Tick(now,false)==0,"not early");Check(s.Tick(now.AddMinutes(1),false)==1,"warning");Check(s.Tick(now.AddSeconds(119),false)==0,"not due");Check(s.Tick(now.AddMinutes(2),false)==2,"due");s.Cancel();Check(s.Tick(now.AddHours(1),false)==0,"cancel");s.Start(now,now.AddMinutes(2),"재부팅");s.Tick(now.AddMinutes(1),false);s.Extend();Check(!s.Warned&&s.Due==now.AddMinutes(12),"extend resets warning");Check(s.Tick(now.AddHours(1),true)==1&&s.Due==now.AddHours(1).AddSeconds(60),"resume grace");s.Cancel();s.Start(now,now.AddMinutes(2),"종료");Check(s.Tick(now.AddMinutes(1),false)==1,"defer warning");s.Defer(now.AddMinutes(7));Check(!s.Warned&&s.Tick(now.AddMinutes(6),false)==1,"defer resets warning");s.Cancel();s.Start(now,now.AddMinutes(2),"종료");Check(!s.GuardTimedOut(now,10),"guard wait begins");s.Defer(now.AddMinutes(5));Check(!s.GuardTimedOut(now.AddMinutes(5),10),"guard wait survives deferral");Check(s.GuardTimedOut(now.AddMinutes(11),10),"guard timeout");s.ClearGuardWait();Check(!s.GuardTimedOut(now.AddMinutes(30),10),"guard wait resets when conditions recover");s.Cancel();DateTime monday=new DateTime(2026,10,26,8,0,0);Check(Schedule.NextOccurrence(monday,"매일","23:00",null)==new DateTime(2026,10,26,23,0,0),"daily next occurrence");Check(Schedule.NextOccurrence(new DateTime(2026,10,26,23,1,0),"매일","23:00",null)==new DateTime(2026,10,27,23,0,0),"daily rollover");Check(Schedule.NextOccurrence(monday,"요일 선택","07:00",new int[]{2})==new DateTime(2026,10,27,7,0,0),"weekday next occurrence");Check(!Schedule.NextOccurrence(monday,"요일 선택","09:00",new int[0]).HasValue,"empty weekdays rejected");s.Start(now,now.AddSeconds(10),"종료");Check(s.Tick(now,false)==1,"short warning");Check(s.Tick(now.AddSeconds(10),false)==2,"short due");Check(SettingsStore.UnprotectToken(SettingsStore.ProtectToken("safe-test-token"))=="safe-test-token","per-user token protection roundtrip");Check(Translations.Data.Count>80,"translation catalog loads");Check(UpdateManager.ParseVersion("v1.2.3").Equals(new Version(1,2,3,0)),"release version parses");Check(UpdateManager.ParseVersion("invalid") == null,"invalid release version rejected");Check(UpdateManager.IsNewer(new Version(1,1,1),new Version(1,1,2)),"newer release detected");Check(!UpdateManager.IsNewer(new Version(1,1,2),new Version(1,1,1)),"older release ignored");Check(UpdateManager.ExecutableAssetName(new Version(1,1,2))=="WindowsTimer-v1.1.2.exe","release asset name matches version");Check(UpdateManager.VersionText=="1.1.2","assembly version shown in UI");Check(Translations.Get("업데이트 확인","English")=="Check for updates","update button English translation");Check(Translations.Get("최신 버전입니다 · v{0}","日本語")=="最新バージョンです · v{0}","update status Japanese translation");Check(Translations.Get("업데이트 서버에 연결할 수 없어요.","简体中文")=="无法连接到更新服务器。","update status Chinese translation");Check(Translations.Get("예약 시작","English")=="Schedule","English UI translation");Check(Translations.Get("안전 데모","English")=="Safe demo","safe demo title translation");Check(Translations.Get("일정","日本語")=="スケジュール","schedule tab Japanese translation");Check(Translations.Get("보호 · 시스템","简体中文")=="保护 · 系统","safety tab Chinese translation");Check(Translations.Get("프로필 · 기록","English")=="Profiles · history","history tab English translation");Check(Translations.Get("원격 · 언어","日本語")=="リモート・言語","remote tab Japanese translation");Check(MainForm.Canonical("Sleep")=="절전","English sleep action mapping");Check(MainForm.Canonical("休眠")=="최대 절전","Chinese hibernate action mapping");Check(MainForm.Canonical("Shut down")=="종료","English shutdown action mapping");string originalSettingsPath=SettingsStore.DirectoryPath;string testSettingsPath=Path.Combine(Path.GetTempPath(),"WindowsTimerSelfTest-"+Guid.NewGuid().ToString("N"));try{SettingsStore.DirectoryPath=testSettingsPath;AppSettings stored=new AppSettings();stored.RemoteTokens.Add("safe-test-token");SettingsStore.Save(stored);string json=File.ReadAllText(Path.Combine(testSettingsPath,"settings.json"));Check(json.Contains("dpapi:")&&!json.Contains("safe-test-token"),"remote token encrypted at rest");Check(SettingsStore.Load().RemoteTokens.Count==1&&SettingsStore.Load().RemoteTokens[0]=="safe-test-token","encrypted remote token settings roundtrip");}finally{SettingsStore.DirectoryPath=originalSettingsPath;if(testSettingsPath.StartsWith(Path.GetTempPath(),StringComparison.OrdinalIgnoreCase)&&Directory.Exists(testSettingsPath))Directory.Delete(testSettingsPath,true);}File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results.txt"),"PASS: "+checks+" scheduling, guard, recurrence, updates, localization, and token-storage checks. No shutdown commands executed.\r\n");return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"test-results.txt"),"FAIL after "+checks+" checks: "+ex);return 1;}}
 }
}
