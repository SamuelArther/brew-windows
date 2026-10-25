class Serie < Formula
  desc "Rich git commit graph in your terminal"
  homepage "https://lusingander.github.io/serie/"
  url "https://github.com/lusingander/serie/archive/refs/tags/v0.10.1.tar.gz"
  sha256 "23f2631b8263c1bca47c616aafa0ffa7b6e39f50160e740eacd69afa9eda76a8"
  license "MIT"
  head "https://github.com/lusingander/serie.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "5498dd142a9daff8486b3f1728ff3c63bc6f0cc1a8a8ace1a5065450b8036542"
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
    assert_match version.to_s, shell_output("#{bin}/serie --version")

    system "git", "init"
    system "git", "commit", "--allow-empty", "-m", "Initial commit"

    begin
      output_log = testpath/"output.log"
      if OS.mac?
        pid = spawn bin/"serie", [:out, :err] => output_log.to_s
      else
        require "pty"
        r, _w, pid = PTY.spawn("#{bin}/serie > #{output_log}")
        r.winsize = [80, 130]
      end
      sleep 1
      assert_match "Initial commit", output_log.read
    ensure
      Process.kill("TERM", pid)
      Process.wait(pid)
    end
  end
end
