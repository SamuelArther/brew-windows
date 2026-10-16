class Leaves < Formula
  desc "Text-mode disk usage visualization utility"
  homepage "https://github.com/patonw/leaves"
  url "https://github.com/patonw/leaves/archive/refs/tags/v0.2.0.tar.gz"
  sha256 "e7635268ddfeddadb2d29ec4f7af22599a5ce6a006082d1621d4de32fd93496b"
  license "MPL-2.0"
  head "https://github.com/patonw/leaves.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "9958722d1cde1f76150886f356fe0d9f70f797c8cecb9fe7c3016411ab834d44"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", "--locked"
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    # leaves is a TUI application
    assert_match version.to_s, shell_output("#{bin}/leaves --version")
  end
end
