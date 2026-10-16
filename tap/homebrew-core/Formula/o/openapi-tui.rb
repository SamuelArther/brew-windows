class OpenapiTui < Formula
  desc "TUI to list, browse and run APIs defined with openapi spec"
  homepage "https://github.com/zaghaghi/openapi-tui"
  url "https://github.com/zaghaghi/openapi-tui/releases/download/0.10.2/openapi-tui-0.10.2-windows-x86_64.tar.gz"
  sha256 "252ae369e38430206b0b08586413059db5dfd39d5242bdc3f3ce736a052a9dfb"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/zaghaghi/openapi-tui/archive/refs/tags/0.10.2.tar.gz"
  end

  def install
    bin.install "openapi-tui.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/openapi-tui --version 2>&1")
  end
end
