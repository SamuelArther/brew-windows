class DerAscii < Formula
  desc "Reversible DER and BER pretty-printer"
  homepage "https://github.com/google/der-ascii"
  url "https://github.com/google/der-ascii/archive/refs/tags/v0.10.0.tar.gz"
  sha256 "bd1a4d6970d4ed9ec32541bc972ddc7dc64ff9357faf567f30bf1367159daf70"
  license "Apache-2.0"
  head "https://github.com/google/der-ascii.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "4ca5c1fbc7a0fb22bab7352b31d86e156614bacbb2d2d53addc872aacacf5300"
  end

  depends_on "go" => :build

  deny_network_access!

  def install
    system "go", "build", *std_go_args(output: bin/"ascii2der"), "./cmd/ascii2der"
    system "go", "build", *std_go_args(output: bin/"der2ascii"), "./cmd/der2ascii"

    pkgshare.install "samples"
  end

  test do
    cp pkgshare/"samples/cert.txt", testpath
    system bin/"ascii2der", "-i", "cert.txt", "-o", "cert.der"
    output = shell_output("#{bin}/der2ascii -i cert.der")
    assert_match "Internet Widgits Pty Ltd", output
  end
end
