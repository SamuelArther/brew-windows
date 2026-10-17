class Cliphist < Formula
  desc "Wayland clipboard manager with support for multimedia"
  homepage "https://github.com/sentriz/cliphist"
  url "https://github.com/sentriz/cliphist/archive/refs/tags/v0.7.0.tar.gz"
  sha256 "8d7dc7b4495e5812b5613274c250ba5d3900933d78888ce7921c01247f191cc8"
  license "GPL-3.0-only"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "21238b1f58abfb5c37cde26fa04964c36c56bc6cb08f96c7613945befeda2778"
  end

  depends_on "go" => :build
  depends_on :linux

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    system "echo test | #{bin}/cliphist store"
    assert_equal "1\ttest", shell_output("#{bin}/cliphist list").strip
  end
end
