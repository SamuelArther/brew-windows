class Riff < Formula
  desc "Diff filter highlighting which line parts have changed"
  homepage "https://github.com/walles/riff"
  url "https://github.com/walles/riff/archive/refs/tags/3.6.2.tar.gz"
  sha256 "2d84d005f33444143eb8f68eb72024cd7eb9addd0b933665aaf44de7e071c175"
  license "MIT"

  no_autobump! because: :bumped_by_upstream

  bottle do
    sha256 cellar: :any, x86_64_windows: "0037210b7e2539a394df59e8a1052b57dd1e6ed23b0c82b2278ef77a3c35aa1d"
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
