class Websocat < Formula
  desc "Command-line client for WebSockets"
  homepage "https://github.com/vi/websocat"
  url "https://github.com/vi/websocat/releases/download/v1.14.1/websocat.x86_64-pc-windows-gnu.exe"
  sha256 "284339b5b86f2b11496137d97b6705bb2ef27e64f21527c1ba240bdb6177ba3b"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/vi/websocat/archive/refs/tags/v1.14.1.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "websocat.x86_64-pc-windows-gnu.exe" => "websocat.exe"
  end

  test do
    system bin/"websocat", "-t", "literal:qwe", "assert:qwe"
  end
end
