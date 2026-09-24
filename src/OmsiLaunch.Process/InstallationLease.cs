using System.Security.Cryptography;
using System.Text;

namespace OmsiLaunch.Process;

public sealed class InstallationLease : IDisposable
{
    private readonly Semaphore semaphore;
    private bool owned;
    private InstallationLease(Semaphore semaphore, bool owned) { this.semaphore = semaphore; this.owned = owned; }
    public static InstallationLease Acquire(string installationRoot)
    {
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(OmsiLaunch.Api.InstallationPaths.IdentityKey(installationRoot))));
        var semaphore = new Semaphore(1, 1, "Local\\OmsiLaunch.Installation." + key);
        if (!semaphore.WaitOne(0)) { semaphore.Dispose(); throw new InvalidOperationException("OL_E_INSTALLATION_BUSY"); }
        return new InstallationLease(semaphore, true);
    }
    // One installation has one lease name however its root is spelled. The
    // controller's own directory carries a trailing separator while an explicit
    // argument usually does not; both must map to the same key.
    public static string NormalizeRoot(string installationRoot) => OmsiLaunch.Api.InstallationPaths.NormalizeRoot(installationRoot);
    public void Dispose() { if (owned) { semaphore.Release(); owned = false; } semaphore.Dispose(); }
}
