class Viddy < Formula
  desc "Modern watch command"
  homepage "https://github.com/sachaos/viddy"
  url "https://github.com/sachaos/viddy/releases/download/v1.3.1/viddy-v1.3.1-windows-x86_64.tar.gz"
  sha256 "b77c34a18d10fb3c9338ef292a59de00f3d695aa423fd3b6ff617ef6840e2a70"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sachaos/viddy/archive/refs/tags/v1.3.1.tar.gz"
  end

  def install
    bin.install "viddy.exe"
  end

  test do
    begin
      pid = spawn bin/"viddy", "--interval", "1", "date"
      sleep 2
    ensure
      Process.kill("TERM", pid)
      Process.wait(pid)
    end

    assert_match "viddy #{version}", shell_output("#{bin}/viddy --version")
  end
end
