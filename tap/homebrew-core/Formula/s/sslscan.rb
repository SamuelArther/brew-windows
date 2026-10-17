class Sslscan < Formula
  desc "Test SSL/TLS enabled services to discover supported cipher suites"
  homepage "https://github.com/rbsec/sslscan"
  url "https://github.com/rbsec/sslscan/archive/refs/tags/2.2.3.tar.gz"
  sha256 "b0498467604c3f4eb7a1b3258ee9f37b709f7844d7edf338d40e85af40ede960"
  license "GPL-3.0-or-later" => { with: "cryptsetup-OpenSSL-exception" }
  head "https://github.com/rbsec/sslscan.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "576b44f2ea16080983a2b18cf37a3084506e9d5d76ea3731c4cbf866e4231d58"
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
