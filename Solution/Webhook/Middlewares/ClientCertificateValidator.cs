using System.Security.Cryptography.X509Certificates;

namespace Webhook.Middlewares;

public sealed class ClientCertificateValidator
{
    private readonly X509Certificate2Collection _trustedRoots;

    public ClientCertificateValidator(IConfiguration configuration)
    {
        var pathsValue = configuration["Webhook:ClientCertificate:AuthorityPath"];
        if (string.IsNullOrWhiteSpace(pathsValue))
        {
            throw new InvalidOperationException("Webhook:ClientCertificate:AuthorityPath is not configured.");
        }

        var paths = pathsValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var roots = new X509Certificate2Collection();

        foreach (var path in paths)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Authority certificate file not found: {path}", path);
            }

            roots.Add(X509CertificateLoader.LoadCertificateFromFile(path));
        }

        _trustedRoots = roots;
    }

    public bool IsAllowed(X509Certificate2 certificate)
        => IsChainValid(certificate)
           && HasValidLifetime(certificate)
           && HasClientAuthenticationEku(certificate)
           && HasDigitalSignatureKeyUsage(certificate)
           && HasStrongPublicKey(certificate)
           && IsLeafCertificate(certificate);

    private bool IsChainValid(X509Certificate2 certificate)
    {
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.AddRange(_trustedRoots);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

        if (!chain.Build(certificate) || chain.ChainElements.Count < 2)
        {
            return false;
        }

        var rootThumbprint = chain.ChainElements[^1].Certificate.Thumbprint;
        return _trustedRoots.Cast<X509Certificate2>().Any(root =>
            string.Equals(root.Thumbprint, rootThumbprint, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasValidLifetime(X509Certificate2 certificate)
    {
        var now = DateTimeOffset.UtcNow;
        return now >= certificate.NotBefore.ToUniversalTime()
               && now <= certificate.NotAfter.ToUniversalTime();
    }

    private static bool HasClientAuthenticationEku(X509Certificate2 certificate)
    {
        foreach (var extension in certificate.Extensions)
        {
            if (extension is not X509EnhancedKeyUsageExtension ekuExtension)
            {
                continue;
            }

            foreach (var oid in ekuExtension.EnhancedKeyUsages)
            {
                if (string.Equals(oid.Value, "1.3.6.1.5.5.7.3.2", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasDigitalSignatureKeyUsage(X509Certificate2 certificate)
    {
        foreach (var extension in certificate.Extensions)
        {
            if (extension is X509KeyUsageExtension keyUsage)
            {
                return keyUsage.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature);
            }
        }

        return false;
    }

    private static bool HasStrongPublicKey(X509Certificate2 certificate)
    {
        using var rsa = certificate.GetRSAPublicKey();
        if (rsa is not null)
        {
            return rsa.KeySize >= 2048;
        }

        using var ecdsa = certificate.GetECDsaPublicKey();
        if (ecdsa is not null)
        {
            return ecdsa.KeySize >= 256;
        }

        return false;
    }

    private static bool IsLeafCertificate(X509Certificate2 certificate)
    {
        foreach (var extension in certificate.Extensions)
        {
            if (extension is X509BasicConstraintsExtension constraints)
            {
                return !constraints.CertificateAuthority;
            }
        }

        return true;
    }
}
