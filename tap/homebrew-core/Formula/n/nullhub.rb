class Nullhub < Formula
  desc "Management console for the Null ecosystem"
  homepage "https://nullhub.io"
  url "https://github.com/nullclaw/nullhub/releases/download/v2026.5.29/nullhub-windows-x86_64.zip"
  sha256 "e776eb1a6d0c4aac85a003a666fe9cd68f53cf9ea4015a080caedf4d8fcb4cd1"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/nullclaw/nullhub/releases/download/v2026.5.29/nullhub-source-v2026.5.29.tar.gz"
  end

  def install
    bin.install "nullhub.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/nullhub --version")

    port = free_port
    pid = spawn bin/"nullhub", "serve", "--host", "127.0.0.1", "--port", port.to_s, "--no-open"

    begin
      output = shell_output("curl --silent --fail --retry 5 --retry-connrefused http://127.0.0.1:#{port}/health")
      assert_equal '{"status":"ok"}', output
    ensure
      Process.kill("TERM", pid)
      Process.wait(pid)
    end
  end
end
