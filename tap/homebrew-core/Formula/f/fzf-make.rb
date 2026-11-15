class FzfMake < Formula
  desc "Fuzzy finder with preview window for various command runners including make"
  homepage "https://github.com/kyu08/fzf-make"
  url "https://github.com/kyu08/fzf-make/archive/refs/tags/v0.76.0.tar.gz"
  sha256 "9183f49fe2026477288630a06b50a899a00d5a2786d45e2d4860e9f3e3ac79d4"
  license "MIT"
  head "https://github.com/kyu08/fzf-make.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "e7e0fc8c78aa51dfbe9122cfb42ad51eff248d7cd54941650037fcc9bdaac3c6"
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
    assert_match version.to_s, shell_output("#{bin}/fzf-make -v")

    (testpath/"Makefile").write <<~MAKE
      brew:
        cc test.c -o test
    MAKE

    begin
      output_log = testpath/"output.log"
      if OS.mac?
        pid = spawn bin/"fzf-make", [:out, :err] => output_log.to_s
      else
        require "pty"
        r, _w, pid = PTY.spawn("#{bin}/fzf-make > #{output_log} 2>&1")
        r.winsize = [80, 130]
      end
      sleep 5
      assert_match "make brew", output_log.read
    ensure
      Process.kill("TERM", pid)
      Process.wait(pid)
    end
  end
end
