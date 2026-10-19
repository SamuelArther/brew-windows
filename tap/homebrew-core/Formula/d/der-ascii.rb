class DerAscii < Formula
  desc "Reversible DER and BER pretty-printer"
  homepage "https://github.com/google/der-ascii"
  url "https://github.com/google/der-ascii/archive/refs/tags/v0.11.0.tar.gz"
  sha256 "15e7268f6ac03617080751be9208df04a6d431d86c72c4ae5d8334e01b03c4a7"
  license "Apache-2.0"
  head "https://github.com/google/der-ascii.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "2fe5120ddeb70df2d709950dff2a436fee70abbcae68b478f6d0cc904305e4ea"
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
