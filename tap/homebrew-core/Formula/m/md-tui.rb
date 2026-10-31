class MdTui < Formula
  desc "Markdown renderer in the terminal written in rust"
  homepage "https://github.com/henriklovhaug/md-tui"
  url "https://github.com/henriklovhaug/md-tui/archive/refs/tags/v0.11.2.tar.gz"
  sha256 "64374823e6d3d8200063dff390088b5e7f3a5a6a3d4d19acf9bb5c29171ffff5"
  license "AGPL-3.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "0993b1d48a4d71075a3f4c1bdaed1467b7e8c5709602e16256525bbcb49cd6cd"
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
