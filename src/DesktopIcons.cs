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
sealed class HotkeyWindow:NativeWindow,IDisposable {
 internal Action Toggle;internal string Shortcut;
 internal HotkeyWindow(){CreateHandle(new CreateParams{Caption="Desktop Icons Hotkey",Parent=new IntPtr(-3)});if(Desktop.RegisterHotKey(Handle,1,0x4003,0x48))Shortcut="Ctrl+Alt+H";else if(Desktop.RegisterHotKey(Handle,1,0x4003,0x7B))Shortcut="Ctrl+Alt+F12";else Shortcut="Tray button only";}
 protected override void WndProc(ref Message m){if(m.Msg==0x312&&m.WParam.ToInt32()==1){if(Toggle!=null)Toggle();return;}base.WndProc(ref m);}
 public void Dispose(){Desktop.UnregisterHotKey(Handle,1);DestroyHandle();}
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
 readonly ContextMenuStrip menu=new ContextMenuStrip();readonly ToolStripMenuItem action=new ToolStripMenuItem();readonly Icon shown=MakeIcon(false),hidden=MakeIcon(true);bool? last;bool first=true,closing;
 internal IconContext(EventWaitHandle stop,EventWaitHandle toggle){this.stop=stop;this.toggle=toggle;keys.Toggle=Toggle;menu.Renderer=new ToolStripProfessionalRenderer(new DarkColors());menu.ForeColor=Color.WhiteSmoke;menu.Items.Add(action);action.Click+=delegate{Toggle();};menu.Items.Add(new ToolStripMenuItem(keys.Shortcut){Enabled=false});menu.Items.Add(new ToolStripSeparator());menu.Items.Add("Show desktop icons and exit",null,delegate{if(Desktop.Visible()==false)Desktop.Toggle();ExitThread();});menu.Opening+=delegate{UpdateState();};tray.ContextMenuStrip=menu;tray.MouseClick+=delegate(object s,MouseEventArgs e){if(e.Button==MouseButtons.Left)Toggle();};UpdateState();tray.Visible=true;timer.Tick+=delegate{if(stop.WaitOne(0)){ExitThread();return;}if(toggle.WaitOne(0))Toggle();UpdateState();};timer.Start();}
 void Toggle(){if(!Desktop.Toggle()){tray.ShowBalloonTip(2500,"Desktop icons","Windows Explorer's desktop is not ready. Try again in a moment.",ToolTipIcon.Info);}UpdateState();}
 void UpdateState(){bool? visible=Desktop.Visible();if(!first&&visible==last)return;first=false;last=visible;tray.Icon=visible==false?hidden:shown;action.Text=visible==false?"Show desktop icons":"Hide desktop icons";action.Enabled=visible.HasValue;tray.Text="Desktop icons: "+(visible==true?"shown":visible==false?"hidden":"waiting")+" | "+keys.Shortcut;try{File.WriteAllText(Path.Combine(AppData.Folder,"status.json"),"{\"shortcut\":\""+keys.Shortcut+"\",\"visible\":"+(visible.HasValue?visible.Value.ToString().ToLowerInvariant():"null")+"}");}catch{}}
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
