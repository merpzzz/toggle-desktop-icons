using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

static class Desktop {
 internal delegate bool EnumProc(IntPtr h,IntPtr p);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr FindWindow(string c,string t);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr FindWindowEx(IntPtr h,IntPtr after,string c,string t);
 [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb,IntPtr p);
 [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll",SetLastError=true)] static extern IntPtr SendMessageTimeout(IntPtr h,uint m,IntPtr w,IntPtr l,uint flags,uint ms,out IntPtr result);
 [DllImport("user32.dll",SetLastError=true)] internal static extern bool RegisterHotKey(IntPtr h,int id,uint modifiers,uint key);
 [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr h,int id);
 [DllImport("user32.dll")] internal static extern bool DestroyIcon(IntPtr h);
 internal static IntPtr View(){IntPtr v=FindWindowEx(FindWindow("Progman",null),IntPtr.Zero,"SHELLDLL_DefView",null);if(v!=IntPtr.Zero)return v;EnumWindows(delegate(IntPtr h,IntPtr p){v=FindWindowEx(h,IntPtr.Zero,"SHELLDLL_DefView",null);return v==IntPtr.Zero;},IntPtr.Zero);return v;}
 internal static bool? Visible(){var v=View();if(v==IntPtr.Zero)return null;var icons=FindWindowEx(v,IntPtr.Zero,"SysListView32",null);return icons==IntPtr.Zero?(bool?)null:IsWindowVisible(icons);}
 internal static bool Toggle(){var v=View();if(v==IntPtr.Zero)return false;IntPtr result;return SendMessageTimeout(v,0x111,new IntPtr(0x7402),IntPtr.Zero,2,1000,out result)!=IntPtr.Zero;}
 internal static bool WaitFor(bool state){for(int i=0;i<30;i++){if(Visible()==state)return true;Thread.Sleep(50);}return false;}
}
sealed class HotkeyWindow : NativeWindow, IDisposable {
 const uint Ctrl=0x0002, Alt=0x0001, Shift=0x0004, NoRepeat=0x4000;
 uint activeModifiers, activeKey;
 bool registered;
 internal Action Toggle;
 internal string Shortcut {get;private set;}
 internal string Requested {get;private set;}

 internal HotkeyWindow() {
  CreateHandle(new CreateParams { Caption="Desktop Icons Hotkey", Parent=new IntPtr(-3) });
  Shortcut="Tray button only";
  Requested="Ctrl+Alt+H";
  string saved=Load();
  string error;
  if(!Change(saved,false,out error)) {
   // The configured key might be occupied when Windows starts.
   // The user can still use the tray and reconfigure the shortcut.
   Requested=saved;
   Shortcut="Hotkey unavailable (tray only)";
  }
 }
 static string Load() {
  try {
   string path=Path.Combine(AppData.Folder,"hotkey.txt");
   if(File.Exists(path)) return File.ReadAllText(path).Trim();
  } catch {}
  return "Ctrl+Alt+H";
 }
 static bool IsOff(string s) {
  return String.Equals(s,"None",StringComparison.OrdinalIgnoreCase) ||
         String.Equals(s,"Off",StringComparison.OrdinalIgnoreCase) ||
         String.Equals(s,"Disabled",StringComparison.OrdinalIgnoreCase);
 }
 internal static bool TryParse(string input,out uint modifiers,out uint key,out string label) {
  modifiers=0;key=0;label="";
  if(String.IsNullOrWhiteSpace(input))return false;
  input=input.Replace(" ","").Replace("\t","").ToUpperInvariant();
  if(IsOff(input)) {label="None";return true;}
  string[] parts=input.Split('+');
  if(parts.Length<2)return false;
  for(int i=0;i<parts.Length;i++) {
   string part=parts[i];uint mod=0;
   if(part=="CTRL" || part=="CONTROL")mod=Ctrl;
   else if(part=="ALT")mod=Alt;
   else if(part=="SHIFT")mod=Shift;
   if(mod!=0) {
    if(key!=0 || (modifiers&mod)!=0)return false;
    modifiers|=mod;
    continue;
   }
   if(key!=0 || i!=parts.Length-1 || part.Length==0)return false;
   if(part.Length==1 && ((part[0]>='A' && part[0]<='Z') ||
                          (part[0]>='0' && part[0]<='9'))) {
    key=(uint)part[0];
   } else if(part.StartsWith("F",StringComparison.Ordinal)) {
    int n;
    if(!Int32.TryParse(part.Substring(1),out n)||n<1||n>12)return false;
    key=(uint)(0x70+n-1);
   } else return false;
  }
  // Avoid bare letter/F-key bindings and shortcuts with Shift alone.
  if(key==0 || (modifiers&(Ctrl|Alt))==0)return false;
  if((modifiers&Ctrl)!=0)label+="Ctrl+";
  if((modifiers&Alt)!=0)label+="Alt+";
  if((modifiers&Shift)!=0)label+="Shift+";
  label+=(key>=0x70 && key<=0x7B)?"F"+(key-0x70+1).ToString():((char)key).ToString();
  return true;
 }

 internal bool Change(string requested,bool persist,out string error) {
  error="";
  uint modifiers,key;
  string label;
  if(!TryParse(requested,out modifiers,out key,out label)) {
   error="Use a combination such as Ctrl+Alt+H, Ctrl+Shift+K or Alt+Shift+F8.";
   return false;
  }
  bool wasRegistered=registered;
  uint previousModifiers=activeModifiers,previousKey=activeKey;
  string previousShortcut=Shortcut,previousRequested=Requested;
  if(registered)Desktop.UnregisterHotKey(Handle,1);
  registered=false;

  uint actualModifiers=modifiers,actualKey=key;
  string actualLabel=label;
  bool disabled=label=="None";
  bool success=disabled || Desktop.RegisterHotKey(Handle,1,modifiers|NoRepeat,key);
  if(!success && label=="Ctrl+Alt+H") {
   actualModifiers=Ctrl|Alt;actualKey=0x7B;
   success=Desktop.RegisterHotKey(Handle,1,actualModifiers|NoRepeat,actualKey);
   if(success)actualLabel="Ctrl+Alt+F12";
  }
  if(!success) {
   if(wasRegistered) {
    registered=Desktop.RegisterHotKey(Handle,1,previousModifiers|NoRepeat,previousKey);
   }
   Shortcut=registered?previousShortcut:"Tray button only";
   Requested=previousRequested;
   error="That shortcut is already used by Windows or another application. Choose a different combination.";
   return false;
  }
  registered=!disabled;
  activeModifiers=actualModifiers;
  activeKey=actualKey;
  Shortcut=disabled?"Tray button only":actualLabel;
  Requested=disabled?"None":requested;
  if(persist) {
   try {File.WriteAllText(Path.Combine(AppData.Folder,"hotkey.txt"),Requested);}
   catch(Exception e) {
    error="Shortcut changed, but Windows could not save the preference: "+e.Message;
    return false;
   }
  }
  return true;
 }
 protected override void WndProc(ref Message m) {
  if(m.Msg==0x312 && m.WParam.ToInt32()==1) {
   if(Toggle!=null)Toggle();
   return;
  }
  base.WndProc(ref m);
 }
 public void Dispose() {
  if(registered)Desktop.UnregisterHotKey(Handle,1);
  DestroyHandle();
 }
}

sealed class ShortcutDialog : Form {
 readonly TextBox captured=new TextBox {ReadOnly=true,Dock=DockStyle.Fill,TabStop=true};
 readonly CheckBox disabled=new CheckBox {Text="Disable keyboard shortcut (tray only)",AutoSize=true};
 string selection;
 internal string Selection {get{return disabled.Checked?"None":selection;}}

 internal ShortcutDialog(string current) {
  Text="Keyboard shortcut";
  FormBorderStyle=FormBorderStyle.FixedDialog;
  StartPosition=FormStartPosition.CenterScreen;
  MinimizeBox=false;MaximizeBox=false;ShowInTaskbar=false;
  ClientSize=new Size(380,190);
  Font=new Font("Segoe UI",9.5f);
  var layout=new TableLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(12),ColumnCount=1,RowCount=4};
  layout.RowStyles.Add(new RowStyle(SizeType.Absolute,35));
  layout.RowStyles.Add(new RowStyle(SizeType.Absolute,35));
  layout.RowStyles.Add(new RowStyle(SizeType.Absolute,35));
  layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
  layout.Controls.Add(new Label {Text="Click below, then press your preferred key combination:",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft},0,0);
  selection=String.Equals(current,"None",StringComparison.OrdinalIgnoreCase)?"Ctrl+Alt+H":current;
  captured.Text=selection;
  layout.Controls.Add(captured,0,1);
  disabled.Checked=String.Equals(current,"None",StringComparison.OrdinalIgnoreCase);
  disabled.CheckedChanged+=delegate {captured.Enabled=!disabled.Checked;};
  captured.Enabled=!disabled.Checked;
  layout.Controls.Add(disabled,0,2);
  var buttons=new FlowLayoutPanel {Dock=DockStyle.Fill,FlowDirection=FlowDirection.RightToLeft};
  var save=new Button {Text="Save shortcut",Width=115,DialogResult=DialogResult.OK};
  var cancel=new Button {Text="Cancel",Width=80,DialogResult=DialogResult.Cancel};
  var reset=new Button {Text="Reset default",Width=105};
  reset.Click+=delegate {disabled.Checked=false;selection="Ctrl+Alt+H";captured.Text=selection;captured.Focus();};
  buttons.Controls.Add(save);buttons.Controls.Add(cancel);buttons.Controls.Add(reset);
  layout.Controls.Add(buttons,0,3);
  Controls.Add(layout);AcceptButton=save;CancelButton=cancel;
  captured.KeyDown+=delegate(object sender,KeyEventArgs e) {
   e.SuppressKeyPress=true;
   if(e.KeyCode==Keys.ControlKey || e.KeyCode==Keys.Menu || e.KeyCode==Keys.ShiftKey)return;
   if(!e.Control && !e.Alt)return;
   Keys kc=e.KeyCode;
   string name=null;
   if(kc>=Keys.A && kc<=Keys.Z)name=kc.ToString();
   else if(kc>=Keys.D0 && kc<=Keys.D9)name=((int)kc-(int)Keys.D0).ToString();
   else if(kc>=Keys.F1 && kc<=Keys.F12)name=kc.ToString();
   if(name==null)return;
   selection=(e.Control?"Ctrl+":"")+(e.Alt?"Alt+":"")+(e.Shift?"Shift+":"")+name;
   captured.Text=selection;
  };
  Shown+=delegate {captured.Focus();};
 }
}
sealed class DarkColors:ProfessionalColorTable {
 public override Color ToolStripDropDownBackground{get{return Color.FromArgb(30,30,30);}}
 public override Color ImageMarginGradientBegin{get{return Color.FromArgb(30,30,30);}}
 public override Color ImageMarginGradientMiddle{get{return Color.FromArgb(30,30,30);}}
 public override Color ImageMarginGradientEnd{get{return Color.FromArgb(30,30,30);}}
 public override Color MenuItemSelected{get{return Color.FromArgb(65,65,65);}}
 public override Color MenuItemBorder{get{return Color.FromArgb(80,80,80);}}
 public override Color MenuBorder{get{return Color.FromArgb(65,65,65);}}
}
sealed class IconContext:ApplicationContext {
 readonly NotifyIcon tray=new NotifyIcon();readonly HotkeyWindow keys=new HotkeyWindow();readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer{Interval=500};readonly EventWaitHandle stop,toggle;
 readonly ContextMenuStrip menu=new ContextMenuStrip();readonly ToolStripMenuItem action=new ToolStripMenuItem();readonly ToolStripMenuItem shortcutLabel=new ToolStripMenuItem {Enabled=false};readonly Icon shown=MakeIcon(false),hidden=MakeIcon(true);bool? last;bool first=true,closing;
 internal IconContext(EventWaitHandle stop,EventWaitHandle toggle){this.stop=stop;this.toggle=toggle;keys.Toggle=Toggle;menu.Renderer=new ToolStripProfessionalRenderer(new DarkColors());menu.ForeColor=Color.WhiteSmoke;menu.Items.Add(action);action.Click+=delegate{Toggle();};menu.Items.Add(shortcutLabel);menu.Items.Add("Change keyboard shortcut...",null,delegate{ChangeShortcut();});menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Show desktop icons and exit",null,delegate{if(Desktop.Visible()==false)Desktop.Toggle();ExitThread();});menu.Opening+=delegate{UpdateState();};tray.ContextMenuStrip=menu;tray.MouseClick+=delegate(object s,MouseEventArgs e){if(e.Button==MouseButtons.Left)Toggle();};UpdateState();tray.Visible=true;timer.Tick+=delegate{if(stop.WaitOne(0)){ExitThread();return;}if(toggle.WaitOne(0))Toggle();UpdateState();};timer.Start();}
 void ChangeShortcut(){
  using(var dialog=new ShortcutDialog(keys.Requested)){
   while(dialog.ShowDialog()==DialogResult.OK) {
    string error;
    bool changed=keys.Change(dialog.Selection,true,out error);
    if(!changed) {
     MessageBox.Show(error,"Desktop Icons Toggle",MessageBoxButtons.OK,MessageBoxIcon.Warning);
     if(!error.StartsWith("Shortcut changed",StringComparison.Ordinal))continue;
    }
    first=true;
    UpdateState();
    break;
   }
  }
 }
 void Toggle(){if(!Desktop.Toggle()){tray.ShowBalloonTip(2500,"Desktop icons","Windows Explorer's desktop is not ready. Try again in a moment.",ToolTipIcon.Info);}UpdateState();}
 void UpdateState(){bool? visible=Desktop.Visible();if(!first&&visible==last)return;first=false;last=visible;tray.Icon=visible==false?hidden:shown;action.Text=visible==false?"Show desktop icons":"Hide desktop icons";action.Enabled=visible.HasValue;shortcutLabel.Text=keys.Shortcut;tray.Text="Desktop icons: "+(visible==true?"shown":visible==false?"hidden":"waiting")+" | "+keys.Shortcut;try{File.WriteAllText(Path.Combine(AppData.Folder,"status.json"),"{\"shortcut\":\""+keys.Shortcut+"\",\"visible\":"+(visible.HasValue?visible.Value.ToString().ToLowerInvariant():"null")+"}");}catch{}}
 static Icon MakeIcon(bool hidden){using(var b=new Bitmap(32,32))using(var g=Graphics.FromImage(b)){g.Clear(Color.Transparent);g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var brush=new SolidBrush(hidden?Color.FromArgb(110,125,140):Color.FromArgb(90,205,245)))for(int x=0;x<2;x++)for(int y=0;y<2;y++)g.FillRectangle(brush,5+x*13,5+y*13,9,9);if(hidden)using(var pen=new Pen(Color.FromArgb(255,205,85),3)){g.DrawLine(pen,4,28,28,4);}IntPtr h=b.GetHicon();try{using(var icon=Icon.FromHandle(h))return (Icon)icon.Clone();}finally{Desktop.DestroyIcon(h);}}}
 protected override void ExitThreadCore(){if(!closing){closing=true;timer.Stop();timer.Dispose();keys.Dispose();tray.Visible=false;tray.Dispose();menu.Dispose();shown.Dispose();hidden.Dispose();}base.ExitThreadCore();}
}
static class AppData {internal static string Folder {get {var p=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DesktopIconsToggle");Directory.CreateDirectory(p);return p;}}}
static class Program {
 [STAThread]static int Main(string[] args){try{
 if(Array.IndexOf(args,"--test")>=0){bool? before=Desktop.Visible();if(!before.HasValue)throw new Exception("Desktop not found");try{if(!Desktop.Toggle()||!Desktop.WaitFor(!before.Value))throw new Exception("Hide/show transition failed");if(!Desktop.Toggle()||!Desktop.WaitFor(before.Value))throw new Exception("Restore transition failed");File.WriteAllText(Path.Combine(AppData.Folder,"test-result.txt"),"PASS: toggled desktop icons both ways and restored original visibility.");}finally{if(Desktop.Visible()!=before){Desktop.Toggle();Desktop.WaitFor(before.Value);}}return 0;}
 using(var stop=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\DesktopIconsToggle.Stop"))using(var toggle=new EventWaitHandle(false,EventResetMode.AutoReset,"Local\\DesktopIconsToggle.Toggle")){
 if(Array.IndexOf(args,"--stop")>=0){stop.Set();return 0;}if(Array.IndexOf(args,"--toggle")>=0){toggle.Set();return 0;}
 bool created;using(var mutex=new Mutex(true,"Local\\DesktopIconsToggle.App",out created)){if(!created){toggle.Set();return 0;}stop.Reset();toggle.Reset();Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new IconContext(stop,toggle));mutex.ReleaseMutex();}}return 0;
 }catch(Exception e){File.WriteAllText(Path.Combine(AppData.Folder,"error.txt"),e.ToString());return 1;}}
}
