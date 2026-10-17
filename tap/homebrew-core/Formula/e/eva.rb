class Eva < Formula
  desc "Calculator REPL, similar to bc(1)"
  homepage "https://github.com/oppiliappan/eva/"
  url "https://github.com/oppiliappan/eva/archive/refs/tags/v0.3.1.tar.gz"
  sha256 "d6a6eb8e0d46de1fea9bd00c361bd7955fcd7cc8f3310b786aad48c1dce7b3f7"
  license "MIT"
  head "https://github.com/oppiliappan/eva.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "ea62fdfa264c1a7d649143ea4990080fff2d6b7f6614f85983b57351f067ece9"
  end

  depends_on "rust" => :build

  deny_network_access!

  def fetch
    system "cargo", "fetch", *std_cargo_fetch_args
  end

  def install
    system "cargo", "install", *std_cargo_args
  end

  test do
    assert_equal "6.0000000000", shell_output("#{bin}/eva '2 + abs(-4)'").strip
  end
end
