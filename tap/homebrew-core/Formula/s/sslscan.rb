class Sslscan < Formula
  desc "Test SSL/TLS enabled services to discover supported cipher suites"
  homepage "https://github.com/rbsec/sslscan"
  url "https://github.com/rbsec/sslscan/archive/refs/tags/2.2.4.tar.gz"
  sha256 "93674469cfbfea7326e5236165b1824df5fbab32a9e0499f03edc10b36befff2"
  license "GPL-3.0-or-later" => { with: "cryptsetup-OpenSSL-exception" }
  head "https://github.com/rbsec/sslscan.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "6a4544f0464ca266230ab85f02647f81b8da37167e7a4dd698a422088c6c9fba"
  end

  depends_on "openssl@3" # TODO: OpenSSL 4 issue - https://github.com/rbsec/sslscan/issues/361

  def install
    system "make"
    system "make", "install", "PREFIX=#{prefix}"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sslscan --version")
    system bin/"sslscan", "google.com"
  end
end
