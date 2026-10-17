class MdTui < Formula
  desc "Markdown renderer in the terminal written in rust"
  homepage "https://github.com/henriklovhaug/md-tui"
  url "https://github.com/henriklovhaug/md-tui/archive/refs/tags/v0.11.0.tar.gz"
  sha256 "b86f03f536235e3016a0f5287ee86972e9a92293586b1c426d0e067b0dd3c0b2"
  license "AGPL-3.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "074d8da4092e2d489dba9d7c3cb73590bf2c4b9b3827e8c0beb10db3b35f78aa"
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
    require "pty"
    require "io/console"

    (testpath/"test.md").write "# Hello World"
    PTY.spawn(bin/"mdt", testpath/"test.md") do |r, w, _pid|
      r.winsize = [80, 43]
      sleep 1
      w.write "q"
      assert_match "Hello World", r.read
    rescue Errno::EIO
      # GNU/Linux raises EIO when read is done on closed pty
    end
  end
end
