class Clippy < Formula
  desc "Copy files from your terminal that actually paste into GUI apps"
  homepage "https://github.com/SamuelArther/brew-windows/tree/main/wintools"
  url "https://github.com/SamuelArther/brew-windows/releases/download/wintools-1.0.3/wintools-1.0.3.zip"
  sha256 "82a71bdf0b54d1c31bb140791ad3ef7b7c4f7bea1394de5697c78d96758281ea"

  # Windows-native rewrite (brew-windows' wintools) of a macOS/Linux-only formula.

  def install
    libexec.install "wintools.exe"
    windows_exe "libexec/wintools.exe", "clippy", "clippy"
    windows_exe "libexec/wintools.exe", "pasty", "pasty"
  end

  test do
    assert_match "clippy", shell_output("#{bin}/clippy --version")
    assert_match "pasty", shell_output("#{bin}/pasty --version")
    (testpath/"test.txt").write("test content\n")
    json = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2025-03-26\"}}\n" \
           "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/call\",\"params\":{\"name\":\"buffer_copy\",\"arguments\":{\"file\":\"test.txt\"}}}\n"
    assert_match "Copied 2 lines from test.txt", pipe_output("#{bin}/clippy mcp-server 2>&1", json, 0)
  end
end
