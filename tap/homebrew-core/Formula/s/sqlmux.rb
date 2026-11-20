class Sqlmux < Formula
  desc "Terminal database client with tmux-style panes and Vim keybindings"
  homepage "https://github.com/kackerx/sqlmux"
  url "https://github.com/kackerx/sqlmux/archive/refs/tags/v0.11.0.tar.gz"
  sha256 "49409df9a00fdbf9ca6b8951e8fb11a35070ad2c9456feea8de0870203eb1225"
  license "MIT"
  head "https://github.com/kackerx/sqlmux.git", branch: "main"

  livecheck do
    url :stable
    regex(/^v?(\d+(:?\.\d+)+)$/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "7ebc8040c6997fc3b60d11ea61f98a2402d8c0a9135658067c37a5fd3fc2f9ab"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.version=#{version}"), "./cmd/sqlmux"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sqlmux --version")

    output = shell_output("#{bin}/sqlmux --where 'id = 1' 2>&1", 1)
    assert_match "--where needs --table", output
  end
end
