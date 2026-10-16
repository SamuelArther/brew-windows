class Mcpsnoop < Formula
  desc "Transparent proxy and TUI for debugging MCP traffic"
  homepage "https://github.com/kerlenton/mcpsnoop"
  url "https://github.com/kerlenton/mcpsnoop/releases/download/v0.27.0/mcpsnoop_0.27.0_windows_amd64.zip"
  sha256 "c928589cba7687c5a1650df8a5ce643a1d02ebfb1ce9d45d7da961a7be42d74a"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/kerlenton/mcpsnoop/archive/refs/tags/v0.27.0.tar.gz"
  end

  def install
    bin.install "mcpsnoop.exe"
  end

  test do
    ENV["MCPSNOOP_HOME"] = testpath
    assert_match version.to_s, shell_output("#{bin}/mcpsnoop version")

    # Wrap a trivial "server" so the shim writes a real session, then check it.
    system bin/"mcpsnoop", "--label", "brewtest", "--", "true"
    assert_match "brewtest", shell_output("#{bin}/mcpsnoop export -T text")
  end
end
