using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace WindowsTimer {
 public enum UpdateFailure { Authentication, Network, InvalidRelease, Verification, Install }

 public sealed class UpdateException : Exception {
  public UpdateFailure Failure { get; private set; }
  public UpdateException(UpdateFailure failure) : base(failure.ToString()) { Failure=failure; }
  public UpdateException(UpdateFailure failure,Exception inner) : base(failure.ToString(),inner) { Failure=failure; }
 }

 public sealed class UpdateInfo {
  public Version CurrentVersion { get; internal set; }
  public Version LatestVersion { get; internal set; }
  public bool IsNewer { get { return UpdateManager.IsNewer(CurrentVersion,LatestVersion); } }
  public string TagName { get; internal set; }
  public long ExecutableAssetSize { get; internal set; }
  public string ExecutableAssetName { get; internal set; }
  public string Sha256 { get; internal set; }
 }

 public static class UpdateManager {
  const string Repository="cybereun/windows-timer";
  const string LatestReleaseApi="repos/cybereun/windows-timer/releases/latest";
  const long MaximumAssetBytes=100L*1024L*1024L;
  static readonly Regex VersionPattern=new Regex("^v?(\\d+)\\.(\\d+)(?:\\.(\\d+))?(?:\\.(\\d+))?$",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);

  public static Version CurrentVersion { get { return Normalize(Assembly.GetExecutingAssembly().GetName().Version); } }
  public static string VersionText { get { return CurrentVersion.ToString(3); } }

  public static Version ParseVersion(string value) {
   if(String.IsNullOrWhiteSpace(value))return null;
   Match match=VersionPattern.Match(value.Trim());if(!match.Success)return null;
   int major,minor,build=0,revision=0;
   if(!Int32.TryParse(match.Groups[1].Value,out major)||!Int32.TryParse(match.Groups[2].Value,out minor))return null;
   if(match.Groups[3].Success&&!Int32.TryParse(match.Groups[3].Value,out build))return null;
   if(match.Groups[4].Success&&!Int32.TryParse(match.Groups[4].Value,out revision))return null;
   try{return new Version(major,minor,build,revision);}catch{return null;}
  }

  static Version Normalize(Version value) { if(value==null)return new Version(0,0,0);return new Version(Math.Max(0,value.Major),Math.Max(0,value.Minor),Math.Max(0,value.Build),Math.Max(0,value.Revision)); }
  public static bool IsNewer(Version current,Version latest) { return Normalize(latest)>Normalize(current); }
  public static string ExecutableAssetName(Version version) { return "WindowsTimer-v"+Normalize(version).ToString(3)+".exe"; }

  public static UpdateInfo CheckLatest() {
   Dictionary<string,object> release=ReadObject(RunGh("api "+LatestReleaseApi,30000));
   if(IsTrue(release,"draft")||IsTrue(release,"prerelease"))throw new UpdateException(UpdateFailure.InvalidRelease);
   string tag=GetString(release,"tag_name");Version latest=ParseVersion(tag);if(latest==null)throw new UpdateException(UpdateFailure.InvalidRelease);
   UpdateInfo result=new UpdateInfo{CurrentVersion=CurrentVersion,LatestVersion=Normalize(latest),TagName=tag};
   if(!result.IsNewer)return result;
   string expected=ExecutableAssetName(latest);object list;
   IList assets=release.TryGetValue("assets",out list)?list as IList:null;if(assets==null)throw new UpdateException(UpdateFailure.InvalidRelease);
   foreach(object entry in assets){Dictionary<string,object> asset=entry as Dictionary<string,object>;if(asset==null||!String.Equals(GetString(asset,"name"),expected,StringComparison.OrdinalIgnoreCase))continue;
    result.ExecutableAssetSize=GetLong(asset,"size");result.ExecutableAssetName=expected;result.Sha256=ParseDigest(GetString(asset,"digest"));break;
   }
   if(result.ExecutableAssetSize<=0||result.ExecutableAssetSize>MaximumAssetBytes||String.IsNullOrEmpty(result.Sha256))throw new UpdateException(UpdateFailure.InvalidRelease);
   return result;
  }

  public static string DownloadAndVerify(UpdateInfo update) {
   if(update==null||!update.IsNewer||String.IsNullOrEmpty(update.TagName)||String.IsNullOrEmpty(update.ExecutableAssetName)||update.ExecutableAssetSize<=0||String.IsNullOrEmpty(update.Sha256))throw new UpdateException(UpdateFailure.InvalidRelease);
   string id=Guid.NewGuid().ToString("N");string directory=Path.Combine(Path.GetTempPath(),"WindowsTimer-download-"+id);string file=Path.Combine(Path.GetTempPath(),"WindowsTimer-update-"+id+".exe");
   try {
    Directory.CreateDirectory(directory);
    string arguments="release download "+QuoteArgument(update.TagName)+" --repo "+Repository+" --pattern "+QuoteArgument(update.ExecutableAssetName)+" --dir "+QuoteArgument(directory);
    RunGh(arguments,120000);
    string assetPath=Path.Combine(directory,update.ExecutableAssetName);
    if(!File.Exists(assetPath))throw new UpdateException(UpdateFailure.Verification);
    FileInfo info=new FileInfo(assetPath);if(info.Length!=update.ExecutableAssetSize||info.Length>MaximumAssetBytes)throw new UpdateException(UpdateFailure.Verification);
    File.Move(assetPath,file);
    string actual=ComputeSha256(file);if(!String.Equals(actual,update.Sha256,StringComparison.OrdinalIgnoreCase))throw new UpdateException(UpdateFailure.Verification);
    Version downloaded=ParseVersion(FileVersionInfo.GetVersionInfo(file).ProductVersion);if(downloaded==null)downloaded=ParseVersion(FileVersionInfo.GetVersionInfo(file).FileVersion);
    if(downloaded==null||Normalize(downloaded)!=Normalize(update.LatestVersion))throw new UpdateException(UpdateFailure.Verification);
    return file;
   } catch { TryDelete(file);throw; } finally { try{if(Directory.Exists(directory))Directory.Delete(directory,true);}catch{} }
  }

  public static string ComputeSha256(string path) {
   using(SHA256 sha=SHA256.Create())using(FileStream stream=File.OpenRead(path)){byte[] hash=sha.ComputeHash(stream);StringBuilder text=new StringBuilder(hash.Length*2);foreach(byte b in hash)text.Append(b.ToString("x2"));return text.ToString();}
  }

  static string ParseDigest(string digest) {
   if(String.IsNullOrWhiteSpace(digest))return null;string prefix="sha256:";if(!digest.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))return null;
   string hash=digest.Substring(prefix.Length).Trim();return Regex.IsMatch(hash,"^[a-fA-F0-9]{64}$")?hash.ToLowerInvariant():null;
  }

  static string RunGh(string arguments,int timeoutMs) {
   Process process=null;StringBuilder output=new StringBuilder(),error=new StringBuilder();object outputLock=new object(),errorLock=new object();
   try {
    ProcessStartInfo start=new ProcessStartInfo{FileName="gh",Arguments=arguments,UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true};
    process=new Process{StartInfo=start};
    process.OutputDataReceived+=delegate(object sender,DataReceivedEventArgs e){if(e.Data!=null)lock(outputLock)output.AppendLine(e.Data);};
    process.ErrorDataReceived+=delegate(object sender,DataReceivedEventArgs e){if(e.Data!=null)lock(errorLock)error.AppendLine(e.Data);};
    if(!process.Start())throw new UpdateException(UpdateFailure.Authentication);
    process.BeginOutputReadLine();process.BeginErrorReadLine();
    if(!process.WaitForExit(timeoutMs)){try{process.Kill();}catch{}try{process.WaitForExit();}catch{}throw new UpdateException(UpdateFailure.Network);}
    process.WaitForExit();string stdout,stderr;lock(outputLock)stdout=output.ToString();lock(errorLock)stderr=error.ToString();
    if(process.ExitCode!=0)throw new UpdateException(IsAuthenticationFailure(stdout+"\n"+stderr)?UpdateFailure.Authentication:UpdateFailure.Network);
    return stdout;
   } catch(UpdateException){throw;} catch {throw new UpdateException(UpdateFailure.Authentication);} finally {if(process!=null)process.Dispose();}
  }

  static bool IsAuthenticationFailure(string text) {
   return Regex.IsMatch(text??String.Empty,@"HTTP\s+(401|403|404)\b",RegexOptions.IgnoreCase)||
    text.IndexOf("not logged into any GitHub hosts",StringComparison.OrdinalIgnoreCase)>=0||
    text.IndexOf("authentication required",StringComparison.OrdinalIgnoreCase)>=0||
    text.IndexOf("could not resolve to a repository",StringComparison.OrdinalIgnoreCase)>=0;
  }

  static string QuoteArgument(string value) {
   StringBuilder quoted=new StringBuilder("\"");int slashes=0;
   foreach(char c in value){if(c=='\\'){slashes++;continue;}if(c=='\"'){quoted.Append('\\',slashes*2+1);quoted.Append(c);slashes=0;continue;}if(slashes>0){quoted.Append('\\',slashes);slashes=0;}quoted.Append(c);}
   if(slashes>0)quoted.Append('\\',slashes*2);quoted.Append('\"');return quoted.ToString();
  }

  static Dictionary<string,object> ReadObject(string json) {
   try { JavaScriptSerializer serializer=new JavaScriptSerializer{MaxJsonLength=4*1024*1024};Dictionary<string,object> result=serializer.Deserialize<Dictionary<string,object>>(json);if(result==null)throw new Exception();return result; }
   catch { throw new UpdateException(UpdateFailure.InvalidRelease); }
  }
  static string GetString(Dictionary<string,object> value,string key){object item;return value.TryGetValue(key,out item)&&item!=null?Convert.ToString(item):String.Empty;}
  static long GetLong(Dictionary<string,object> value,string key){object item;long number;return value.TryGetValue(key,out item)&&Int64.TryParse(Convert.ToString(item),out number)?number:0;}
  static bool IsTrue(Dictionary<string,object> value,string key){object item;return value.TryGetValue(key,out item)&&item!=null&&Convert.ToBoolean(item);}

  static void TryDelete(string path){try{if(File.Exists(path))File.Delete(path);}catch{}}

  public static int ApplyUpdateCommand(string[] args) {
   string target=null,stage=null,backup=null,source=Application.ExecutablePath;bool replaced=false,demo=Array.IndexOf(args,"--demo")>=0;int oldPid=0;
   try {
    if(args.Length<4||!Int32.TryParse(args[2],out oldPid)||oldPid<=0)throw new UpdateException(UpdateFailure.Install);
    target=Path.GetFullPath(args[1]);string tempRoot=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;string sourceFull=Path.GetFullPath(source);
    if(!sourceFull.StartsWith(tempRoot,StringComparison.OrdinalIgnoreCase)||!Path.GetFileName(sourceFull).StartsWith("WindowsTimer-update-",StringComparison.OrdinalIgnoreCase)||!Regex.IsMatch(Path.GetFileName(target),"^WindowsTimer(?:-v\\d+(?:\\.\\d+){1,3})?\\.exe$",RegexOptions.IgnoreCase)||!File.Exists(target))throw new UpdateException(UpdateFailure.Install);
    string expected=ParseDigest("sha256:"+args[3]);if(String.IsNullOrEmpty(expected)||!String.Equals(ComputeSha256(sourceFull),expected,StringComparison.OrdinalIgnoreCase))throw new UpdateException(UpdateFailure.Verification);
    Version incoming=Normalize(Assembly.GetExecutingAssembly().GetName().Version);Version existing=ParseVersion(FileVersionInfo.GetVersionInfo(target).ProductVersion);if(existing==null)existing=ParseVersion(FileVersionInfo.GetVersionInfo(target).FileVersion);if(existing==null||!IsNewer(existing,incoming))throw new UpdateException(UpdateFailure.Verification);
    string directory=Path.GetDirectoryName(target);stage=Path.Combine(directory,"WindowsTimer-update-stage-"+Guid.NewGuid().ToString("N")+".exe");backup=Path.Combine(directory,"WindowsTimer-update-backup-"+Guid.NewGuid().ToString("N")+".exe");
    File.Copy(sourceFull,stage,false);if(!String.Equals(ComputeSha256(stage),expected,StringComparison.OrdinalIgnoreCase))throw new UpdateException(UpdateFailure.Verification);
    try { using(Process old=Process.GetProcessById(oldPid)){if(!old.WaitForExit(60000))throw new UpdateException(UpdateFailure.Install);} } catch(ArgumentException) { }
    try { File.Replace(stage,target,backup,true);replaced=true; }
    catch {
     if(File.Exists(backup))File.Delete(backup);File.Move(target,backup);
     try { File.Move(stage,target);replaced=true; } catch { if(File.Exists(target))File.Delete(target);if(File.Exists(backup))File.Move(backup,target);throw; }
    }
    string cleanup="--cleanup-update "+Process.GetCurrentProcess().Id.ToString()+" \""+sourceFull+"\""+(demo?" --demo":"");
    Process.Start(new ProcessStartInfo{FileName=target,Arguments=cleanup,WorkingDirectory=directory,UseShellExecute=false});
    TryDelete(backup);return 0;
   } catch(Exception ex) {
    if(replaced&&target!=null&&backup!=null&&File.Exists(backup)){try{if(File.Exists(target))File.Delete(target);File.Move(backup,target);}catch{}}
    TryDelete(stage);
    if(target!=null&&File.Exists(target)){try{Process.Start(new ProcessStartInfo{FileName=target,Arguments=demo?"--demo":"",WorkingDirectory=Path.GetDirectoryName(target),UseShellExecute=false});}catch{}}
    MessageBox.Show("업데이트를 적용하지 못해 현재 버전을 유지합니다. 앱을 다시 확인해 주세요.","윈도우 타이머 업데이트",MessageBoxButtons.OK,MessageBoxIcon.Warning);
    TryDelete(backup);return ex is UpdateException?2:1;
   }
  }

  public static void CompleteUpdateCleanup(string[] args) {
   try {
    int helperPid;if(args.Length<3||!Int32.TryParse(args[1],out helperPid)||helperPid<=0)return;
    string source=Path.GetFullPath(args[2]);string root=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
    if(!source.StartsWith(root,StringComparison.OrdinalIgnoreCase)||!Path.GetFileName(source).StartsWith("WindowsTimer-update-",StringComparison.OrdinalIgnoreCase))return;
    try { using(Process helper=Process.GetProcessById(helperPid)){helper.WaitForExit(15000);} } catch(ArgumentException) { }
    TryDelete(source);
   } catch { }
  }

  public static void CleanupStaleDownloads() {
   try { string root=Path.GetFullPath(Path.GetTempPath());foreach(string file in Directory.GetFiles(root,"WindowsTimer-update-*.exe",SearchOption.TopDirectoryOnly)){try{if(File.GetLastWriteTimeUtc(file)<DateTime.UtcNow.AddDays(-7))File.Delete(file);}catch{}} } catch { }
  }
 }
}
