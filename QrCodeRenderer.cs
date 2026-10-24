using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using QRCoder;

namespace WindowsTimer {
 static class EmbeddedQrLibrary {
  public static void RegisterResolver() {
   AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs args) {
    if (!String.Equals(new AssemblyName(args.Name).Name,"QRCoder",StringComparison.OrdinalIgnoreCase)) return null;
    Assembly owner=Assembly.GetExecutingAssembly();
    using(Stream input=owner.GetManifestResourceStream("WindowsTimer.QRCoder.dll")) {
     if(input==null)return null;
     using(MemoryStream output=new MemoryStream()) { input.CopyTo(output); return Assembly.Load(output.ToArray()); }
    }
   };
  }
 }

 static class RemotePairingQr {
  public static string CreatePayload(string address,string code) {
   Uri uri;
   if(String.IsNullOrWhiteSpace(address)||!Uri.TryCreate(address,UriKind.Absolute,out uri))return null;
   if(uri.Scheme!="http"&&uri.Scheme!="https")return null;
   if(String.IsNullOrEmpty(code)||code.Length!=6) return null;
   for(int i=0;i<code.Length;i++)if(code[i]<'0'||code[i]>'9')return null;
   UriBuilder builder=new UriBuilder(uri);builder.Fragment="code="+code;return builder.Uri.AbsoluteUri;
  }

  public static Bitmap Render(string payload) {
   if(String.IsNullOrWhiteSpace(payload))throw new ArgumentException("QR payload is empty.","payload");
   using(QRCodeGenerator generator=new QRCodeGenerator())
   using(QRCodeData data=generator.CreateQrCode(payload,QRCodeGenerator.ECCLevel.M))
   using(QRCode qr=new QRCode(data))return qr.GetGraphic(5,Color.Black,Color.White,true);
  }
 }
}
