class Tut < Formula
  desc "TUI for Mastodon with vim inspired keys"
  homepage "https://tut.anv.nu"
  url "https://github.com/RasmusLindroth/tut/archive/refs/tags/2.0.1.tar.gz"
  sha256 "afa8c49036461a36c091d83ef51f9a3bbd938ee78f817c6467175699a989b863"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "eda6f1f79ff4eee42c54d022b70d773a4cfdeb428534e0be53989016b305cb55"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tut --version")
    assert_match "Instance:", pipe_output("#{bin}/tut --new-user 2> /dev/null")
  end
end
