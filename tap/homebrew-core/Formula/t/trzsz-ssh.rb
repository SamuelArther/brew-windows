class TrzszSsh < Formula
  desc "Highly OpenSSH-compatible client with extended features"
  homepage "https://trzsz.github.io/tssh"
  url "https://github.com/trzsz/trzsz-ssh/archive/refs/tags/v0.1.26.tar.gz"
  sha256 "67c9082543e1785ece3f5ab09f6299cd655e3657593d55cc85751c097c1bb381"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "9b19b447fdf9c7b7c940b9a2efa464eb072a8b34c7753d2158555edaabb255ae"
  end

  depends_on "go" => :build

  conflicts_with "tssh", because: "both install `tssh` binaries"

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(output: bin/"tssh"), "./cmd/tssh"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tssh -v")

    assert_match "invalid option", shell_output("#{bin}/tssh -o abc 2>&1", 11)
    assert_match "invalid bind specification", shell_output("#{bin}/tssh -D xyz 2>&1", 11)
    assert_match "invalid forwarding specification", shell_output("#{bin}/tssh -L 123 2>&1", 11)
  end
end
