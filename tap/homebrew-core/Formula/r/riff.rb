class Riff < Formula
  desc "Diff filter highlighting which line parts have changed"
  homepage "https://github.com/walles/riff"
  url "https://github.com/walles/riff/archive/refs/tags/3.7.0.tar.gz"
  sha256 "996d3037788c971fde859720557a04b6910e68b887e058df037a868fddad726b"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "ae87560b5f7b2dc90973f29fadfd78af06eb34ca81cf3c78448dfe92a820c99e"
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
    # Windows has no /etc/passwd; any file compared with itself shows no differences.
    same = OS.windows? ? (testpath/"same.txt").tap { |f| f.write "same\n" } : "/etc/passwd"
    assert_empty shell_output("#{bin}/riff #{same} #{same}")
    assert_match version.to_s, shell_output("#{bin}/riff --version")
  end
end
