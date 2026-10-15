class DnscryptProxy < Formula
  desc "Secure communications between a client and a DNS resolver"
  homepage "https://dnscrypt.info"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-dnscrypt-proxy-2.1.18-1-any.pkg.tar.zst"
  sha256 "6a555691d90864a7c31c9a48d24e61d34e1c633bfe1ab4b6f2b532b331318e40"
  license "ISC"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/DNSCrypt/dnscrypt-proxy/archive/refs/tags/2.1.18.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/dnscrypt-proxy.exe", "dnscrypt-proxy"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dnscrypt-proxy --version 2>&1")
  end
end
