class Reddix < Formula
  desc "Reddit, refined for the terminal"
  homepage "https://github.com/ck-zhang/reddix"
  url "https://github.com/ck-zhang/reddix/archive/refs/tags/v0.2.9.tar.gz"
  sha256 "0c1af2b263d3c47cc64acd9addc3ba7c44731f2bace4ee8ad5189eb2a510b9d9"
  license "MIT"
  head "https://github.com/ck-zhang/reddix.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "1ef26f0af05a44219411c49397e5f4224338d7b8988f4bf55bf7ce7c2e0f68be"
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
    assert_match version.to_s, shell_output("#{bin}/reddix --version")

    begin
      output_log = testpath/"output.log"
      if OS.mac?
        pid = spawn bin/"reddix", testpath, [:out, :err] => output_log.to_s
      else
        require "pty"
        r, _w, pid = PTY.spawn("#{bin}/reddix #{testpath} > #{output_log}")
        r.winsize = [80, 130]
      end
      sleep 1
      assert_match "Sign in to load comments", output_log.read
    ensure
      Process.kill("TERM", pid)
      Process.wait(pid)
    end
  end
end
