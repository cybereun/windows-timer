using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;

namespace WindowsTimer {
 sealed class RemoteTunnel {
  readonly object gate=new object();
  bool active;
  Process process;
  string address,origin;
  Action<string> statusChanged,ready,stopped;
  public bool IsActive { get { lock(gate)return active; } }
  public string Address { get { lock(gate)return address; } }

  public void Start(string localAddress,Action<string> onStatus,Action<string> onReady,Action<string> onStopped) {
   Uri local;if(!Uri.TryCreate(localAddress,UriKind.Absolute,out local)||local.Scheme!="http"||!String.Equals(local.Host,"127.0.0.1",StringComparison.OrdinalIgnoreCase)||local.Port<1||local.Port>65535)throw new ArgumentException("Local remote server address is invalid.");
   lock(gate){if(active)return;active=true;address=null;origin=localAddress;statusChanged=onStatus;ready=onReady;stopped=onStopped;}
   Thread t=new Thread(Run);t.IsBackground=true;t.Name="WindowsTimer internet tunnel";t.Start();
  }
  public void Stop() {
   Process p;
   lock(gate){active=false;address=null;origin=null;p=process;process=null;}
   if(p!=null)try{if(!p.HasExited)p.Kill();}catch{}
  }
  void Run() {
   Process p=null;
   try {
    ReportStatus("Cloudflare 터널 프로그램을 확인하고 있어요.");
    string executable=EnsureCloudflared(ReportStatus);
    if(!IsActive)return;
    ReportStatus("Cloudflare 인터넷 주소를 만드는 중이에요.");
    string localAddress;lock(gate)localAddress=origin;if(String.IsNullOrEmpty(localAddress))return;
    p=new Process();p.StartInfo=new ProcessStartInfo{FileName=executable,Arguments="tunnel --url \""+localAddress+"\"",UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=Path.GetDirectoryName(executable)};
    p.OutputDataReceived+=delegate(object sender,DataReceivedEventArgs e){ReadOutput(e.Data);};
    p.ErrorDataReceived+=delegate(object sender,DataReceivedEventArgs e){ReadOutput(e.Data);};
    p.EnableRaisingEvents=true;p.Exited+=delegate{ProcessExited(p);};
    lock(gate){if(!active){p.Dispose();return;}process=p;}
    if(!p.Start())throw new InvalidOperationException("터널 프로그램을 시작하지 못했어요.");
    p.BeginOutputReadLine();p.BeginErrorReadLine();
    DateTime deadline=DateTime.UtcNow.AddSeconds(60);
    while(IsActive&&Address==null&&DateTime.UtcNow<deadline){if(p.HasExited)throw new InvalidOperationException("인터넷 터널이 시작되지 않았어요.");Thread.Sleep(250);}
    if(!IsActive)return;
    if(Address==null)throw new TimeoutException("인터넷 주소를 받지 못했어요.");
    ReportStatus("임시 인터넷 주소를 연결 확인하고 있어요.");
    string publicAddress=Address;if(!WaitForPage(publicAddress,30000))throw new TimeoutException("인터넷 주소를 열 수 없어요.");
    Action<string> callback;lock(gate){if(!active)return;callback=ready;}if(callback!=null)try{callback(publicAddress);}catch{}
   }catch(Exception){
    if(p!=null)try{if(!p.HasExited)p.Kill();}catch{}
    bool notify=false;lock(gate){if(active){active=false;address=null;if(Object.ReferenceEquals(process,p))process=null;notify=true;}}
    if(notify)ReportStopped("임시 인터넷 연결에 실패했어요. 네트워크 연결을 확인해 주세요.");
   }finally{if(p!=null){bool keep;lock(gate)keep=active&&Object.ReferenceEquals(process,p);if(!keep)try{p.Dispose();}catch{}}}
  }
  void ReadOutput(string line) {
   if(String.IsNullOrEmpty(line))return;
   string parsed=ExtractQuickUrl(line);if(parsed==null)return;
   lock(gate){if(!active||address!=null)return;address=parsed;}
  }
  bool WaitForPage(string url,int timeoutMilliseconds){DateTime deadline=DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);while(IsActive&&DateTime.UtcNow<deadline){try{ServicePointManager.SecurityProtocol=ServicePointManager.SecurityProtocol|(SecurityProtocolType)3072;HttpWebRequest request=(HttpWebRequest)WebRequest.Create(url);request.Method="GET";request.Timeout=5000;request.ReadWriteTimeout=5000;using(HttpWebResponse response=(HttpWebResponse)request.GetResponse()){if(response.StatusCode==HttpStatusCode.OK)using(StreamReader reader=new StreamReader(response.GetResponseStream(),Encoding.UTF8)){string page=reader.ReadToEnd();if(page.Contains("id='pair'")&&page.Contains("id='code'"))return true;}}}catch{}if(IsActive)Thread.Sleep(1000);}return false;}
  void ProcessExited(Process p) {
   bool notify=false;lock(gate){if(Object.ReferenceEquals(process,p)){process=null;if(active){active=false;address=null;notify=true;}}}
   if(notify)ReportStopped("임시 인터넷 연결이 끊어졌어요.");
  }
  void ReportStatus(string value){Action<string> callback;lock(gate)callback=statusChanged;if(callback!=null)try{callback(value);}catch{}}
  void ReportStopped(string value){Action<string> callback;lock(gate)callback=stopped;if(callback!=null)try{callback(value);}catch{}}

  internal static string ExtractQuickUrl(string line) {
   if(String.IsNullOrEmpty(line))return null;
   Match m=Regex.Match(line,@"https://[a-z0-9-]+\.trycloudflare\.com(?:/)?",RegexOptions.IgnoreCase);
   if(!m.Success)return null;
   return m.Value.TrimEnd('/')+"/";
  }

  static string EnsureCloudflared(Action<string> reportStatus) {
   bool x64=IntPtr.Size==8;string assetName=x64?"cloudflared-windows-amd64.exe":"cloudflared-windows-386.exe";
   string directory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WindowsTimer","cloudflared");Directory.CreateDirectory(directory);
   string executable=Path.Combine(directory,assetName),sidecar=executable+".sha256";
   ReleaseAsset latest=GetLatestAsset(assetName);
   if(File.Exists(executable)&&File.Exists(sidecar))try{string saved=File.ReadAllText(sidecar).Trim();if(String.Equals(saved,latest.Sha256,StringComparison.OrdinalIgnoreCase)&&String.Equals(HashFile(executable),latest.Sha256,StringComparison.OrdinalIgnoreCase))return executable;}catch{}
   if(reportStatus!=null)try{reportStatus("Cloudflare 터널 프로그램을 내려받고 검증하고 있어요.");}catch{}
   DownloadAsset(latest,executable);
   return executable;
  }
  sealed class ReleaseAsset { public string Version,Url,Sha256; }
  static ReleaseAsset GetLatestAsset(string assetName) {
   string json=DownloadText("https://api.github.com/repos/cloudflare/cloudflared/releases/latest",4*1024*1024);
   Dictionary<string,object> data=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(json);if(data==null)throw new InvalidDataException("Cloudflare release information is invalid.");
   ReleaseAsset result=new ReleaseAsset();object version,body,rawAssets;if(data.TryGetValue("tag_name",out version))result.Version=Convert.ToString(version);if(data.TryGetValue("body",out body))result.Sha256=ReadDigest(Convert.ToString(body),assetName);
   if(!data.TryGetValue("assets",out rawAssets)||!(rawAssets is ArrayList))throw new InvalidDataException("Cloudflare executable is missing from the release.");
   foreach(object raw in (ArrayList)rawAssets){Dictionary<string,object> item=raw as Dictionary<string,object>;if(item==null)continue;object name,url,digest;if(!item.TryGetValue("name",out name)||!String.Equals(Convert.ToString(name),assetName,StringComparison.OrdinalIgnoreCase))continue;if(item.TryGetValue("browser_download_url",out url))result.Url=Convert.ToString(url);if(item.TryGetValue("digest",out digest)){string value=Convert.ToString(digest);if(value.StartsWith("sha256:",StringComparison.OrdinalIgnoreCase))result.Sha256=value.Substring(7);}}
   if(String.IsNullOrEmpty(result.Url)||!IsHash(result.Sha256))throw new InvalidDataException("Cloudflare executable checksum is missing.");
   Uri download;if(!Uri.TryCreate(result.Url,UriKind.Absolute,out download)||download.Scheme!="https"||!String.Equals(download.Host,"github.com",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Cloudflare download URL is invalid.");
   return result;
  }
  static string ReadDigest(string body,string assetName) {
   if(String.IsNullOrEmpty(body))return null;Match m=Regex.Match(body,Regex.Escape(assetName)+@"\s*:\s*([a-f0-9]{64})",RegexOptions.IgnoreCase);return m.Success?m.Groups[1].Value.ToLowerInvariant():null;
  }
  static bool IsHash(string value){return !String.IsNullOrEmpty(value)&&Regex.IsMatch(value,@"^[a-f0-9]{64}$",RegexOptions.IgnoreCase);}
  static string DownloadText(string url,int limit) {
   ServicePointManager.SecurityProtocol=ServicePointManager.SecurityProtocol|(SecurityProtocolType)3072;
   HttpWebRequest request=(HttpWebRequest)WebRequest.Create(url);request.Method="GET";request.UserAgent="WindowsTimer/"+UpdateManager.VersionText;request.Accept="application/vnd.github+json";request.Timeout=20000;request.ReadWriteTimeout=20000;
   using(HttpWebResponse response=(HttpWebResponse)request.GetResponse()){if(response.StatusCode!=HttpStatusCode.OK)throw new WebException("Release metadata request failed.");using(Stream input=response.GetResponseStream())using(MemoryStream output=new MemoryStream()){byte[] buffer=new byte[8192];int n;while((n=input.Read(buffer,0,buffer.Length))>0){if(output.Length+n>limit)throw new InvalidDataException("Release metadata is too large.");output.Write(buffer,0,n);}return Encoding.UTF8.GetString(output.ToArray());}}
  }
  static void DownloadAsset(ReleaseAsset asset,string executable) {
   ServicePointManager.SecurityProtocol=ServicePointManager.SecurityProtocol|(SecurityProtocolType)3072;
   HttpWebRequest request=(HttpWebRequest)WebRequest.Create(asset.Url);request.Method="GET";request.UserAgent="WindowsTimer/"+UpdateManager.VersionText;request.Timeout=45000;request.ReadWriteTimeout=45000;
   string temporary=executable+".download";
   try{using(HttpWebResponse response=(HttpWebResponse)request.GetResponse()){string host=response.ResponseUri.Host;if(response.StatusCode!=HttpStatusCode.OK||!(String.Equals(host,"github.com",StringComparison.OrdinalIgnoreCase)||host.EndsWith(".githubusercontent.com",StringComparison.OrdinalIgnoreCase)))throw new WebException("Cloudflare download host is invalid.");long length=response.ContentLength;if(length>120L*1024L*1024L)throw new InvalidDataException("Cloudflare executable is too large.");using(Stream input=response.GetResponseStream())using(FileStream output=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None)){byte[] buffer=new byte[65536];int n;long total=0;while((n=input.Read(buffer,0,buffer.Length))>0){total+=n;if(total>120L*1024L*1024L)throw new InvalidDataException("Cloudflare executable is too large.");output.Write(buffer,0,n);}}}
    string actual=HashFile(temporary);if(!String.Equals(actual,asset.Sha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Cloudflare executable checksum does not match.");
    if(File.Exists(executable))File.Replace(temporary,executable,null);else File.Move(temporary,executable);File.WriteAllText(executable+".sha256",asset.Sha256,Encoding.ASCII);
   }finally{try{if(File.Exists(temporary))File.Delete(temporary);}catch{}}
  }
  static string HashFile(string path){using(SHA256 sha=SHA256.Create())using(FileStream s=File.OpenRead(path)){byte[] hash=sha.ComputeHash(s);StringBuilder b=new StringBuilder();foreach(byte x in hash)b.Append(x.ToString("x2"));return b.ToString();}}
 }
}
