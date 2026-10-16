class Gor < Formula
  desc "Real-time HTTP traffic replay tool written in Go"
  homepage "https://goreplay.org"
  url "https://github.com/probelabs/goreplay/releases/download/1.3.3/gor-1.3.3_windows.zip"
  sha256 "84a1ff5fe0944f5e804017be6496fadb5a6c0c5792472752cbdd066c8743d240"
  license "LGPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/probelabs/goreplay/archive/refs/tags/1.3.3.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "gor.exe"
  end

  test do
    (testpath/"test").write "Hello"
    test_port = free_port
    server_pid = spawn bin/"gor", "file-server", ":#{test_port}"
    sleep 2
    assert_equal "Hello", shell_output("curl -s http://localhost:#{test_port}/test")
  ensure
    Process.kill "TERM", server_pid
  end
end
