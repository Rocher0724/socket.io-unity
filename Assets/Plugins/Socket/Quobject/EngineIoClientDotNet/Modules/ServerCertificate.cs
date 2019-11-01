using System.Net;
using System.Net.Security;

namespace Socket.Quobject.EngineIoClientDotNet.Modules {
  public class ServerCertificate
  {
    public static bool Ignore { get; set; }

    static ServerCertificate()
    {
      ServerCertificate.Ignore = false;
    }

    public static void IgnoreServerCertificateValidation()
    {
      ServicePointManager.ServerCertificateValidationCallback += (RemoteCertificateValidationCallback) ((sender, certificate, chain, sslPolicyErrors) => true);
      ServerCertificate.Ignore = true;
    }
  }
}
