class Hexhog < Formula
  desc "Hex viewer/editor"
  homepage "https://github.com/DVDTSB/hexhog"
  url "https://github.com/DVDTSB/hexhog/archive/refs/tags/v0.1.3.tar.gz"
  sha256 "5858dcb32b3f12647784c9a6ba2e107e157b9a82884bcfed3e994a70c7584b29"
  license "MIT"
  head "https://github.com/DVDTSB/hexhog.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8b03d43b9f2ee3d1c6f75006067ee2280c7ca2864825e7b046d608860a7df04e"
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
    assert_match version.to_s, shell_output("#{bin}/hexhog --version")

    begin
      output_log = testpath/"output.log"

      (testpath/"testfile").write("Hello, Hexhog!")
      if OS.mac?
        pid = spawn bin/"hexhog", testpath/"testfile", [:out, :err] => output_log.to_s
      else
        require "pty"
        r, _w, pid = PTY.spawn("#{bin}/hexhog #{testpath}/testfile > #{output_log}")
        r.winsize = [80, 130]
      end
      sleep 1
      assert_match "hexhog", output_log.read
    ensure
      Process.kill("TERM", pid)
      Process.wait(pid)
    end
  end
end
