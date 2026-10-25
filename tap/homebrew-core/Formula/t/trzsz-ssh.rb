class TrzszSsh < Formula
  desc "Highly OpenSSH-compatible client with extended features"
  homepage "https://trzsz.github.io/tssh"
  url "https://github.com/trzsz/trzsz-ssh/archive/refs/tags/v0.1.27.tar.gz"
  sha256 "8655411ebe865b8d91463bc9ab336c14f8ff3eecf4889c3b2547e26559796b6f"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "e45f4b49d63af055733f1383a17c96e49287e2b25dafd6548ebbbeb9c576a8eb"
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
