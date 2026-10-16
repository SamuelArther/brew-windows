class Webify < Formula
  desc "Wrapper for shell commands as web services"
  homepage "https://github.com/beefsack/webify"
  url "https://github.com/beefsack/webify/releases/download/v1.5.0/webify-v1.5.0-windows-amd64.zip"
  sha256 "1d4937cf7758fb21e0ae974cea153ed1f91a1b71456151fc00229eb0b427e619"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/beefsack/webify/archive/refs/tags/v1.5.0.tar.gz"
  end

  def install
    bin.install "webify.exe"
  end

  test do
    port = free_port
    spawn bin/"webify", "-addr=:#{port}", "cat"
    sleep 1
    assert_equal "Homebrew", shell_output("curl -s -d Homebrew http://localhost:#{port}")
  end
end
