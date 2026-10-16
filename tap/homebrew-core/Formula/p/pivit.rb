class Pivit < Formula
  desc "Sign and verify data using hardware (Yubikey) backed x509 certificates (PIV)"
  homepage "https://github.com/cashapp/pivit"
  url "https://github.com/cashapp/pivit/archive/refs/tags/v0.9.3.tar.gz"
  sha256 "ad52207e4962969a28aa298098b0a360ab6ffc61e4e075d9e0842adccbac4a29"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "b80bd834e1ace2d9a7b1f46b32a95e6f7ae150d1064e9aa6f89623e1a324140b"
  end

  depends_on "go" => :build
  depends_on "pkgconf" => :build

  on_linux do
    depends_on "pcsc-lite"
  end

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    ENV["CGO_ENABLED"] = "1"
    system "go", "build", *std_go_args, "./cmd/pivit"
  end

  test do
    output = shell_output("#{bin}/pivit -p 2>&1", 1).strip
    assert_match "the Smart card resource manager is not running", output
  end
end
