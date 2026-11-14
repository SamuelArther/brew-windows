class Lsoff < Formula
  desc "List listening TCP and UDP ports"
  homepage "https://github.com/yutat23/lsoff"
  url "https://github.com/yutat23/lsoff/archive/refs/tags/v0.1.6.tar.gz"
  sha256 "d6acf3cace6fce7d56e02c3310023fb9bebf82d8a99ff672f1c4270141c25641"
  license "MIT"
  head "https://github.com/yutat23/lsoff.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "adce1670555830310f5e22bee1a663dbf145127e8fb650168544e753f223898d"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: :goreleaser)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/lsoff --version")

    output = shell_output("#{bin}/lsoff --kill 2>&1", 2)
    assert_match "-k requires a port", output
  end
end
