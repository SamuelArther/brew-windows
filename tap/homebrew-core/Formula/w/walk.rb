class Walk < Formula
  desc "Terminal navigator"
  homepage "https://github.com/antonmedv/walk"
  url "https://github.com/antonmedv/walk/archive/refs/tags/v1.13.0.tar.gz"
  sha256 "9f62377438908757fcb2210bd08bf346391858f21d0ef664d7998abf635880cb"
  license "MIT"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "749f0a965756720ad5560e5fbdd8d2b220f2817761bce1d193b86c4895362d60"
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
    require "pty"
    require "expect"

    PTY.spawn(bin/"walk") do |r, w, _pid|
      r.winsize = [80, 60]
      # walk waits for replies to its terminal background colour and cursor position queries
      r.expect("\e[6n", 10)
      w.write "\e]11;rgb:0000/0000/0000\e\\\e[1;1R"
      sleep 1
      w.write "\e"
      begin
        r.read
      rescue Errno::EIO
        # GNU/Linux raises EIO when read is done on closed pty
      end
    end
  end
end
