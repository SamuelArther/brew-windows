class Proxyfor < Formula
  desc "Proxy CLI for capturing and inspecting HTTP(S) and WS(S) traffic"
  homepage "https://github.com/sigoden/proxyfor"
  url "https://github.com/sigoden/proxyfor/releases/download/v0.5.0/proxyfor-v0.5.0-x86_64-pc-windows-msvc.zip"
  sha256 "0bdff7fd23db97feebace190031a1c6006f514450c44015ff4337b0f1517b88f"
  license any_of: ["Apache-2.0", "MIT"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/sigoden/proxyfor/archive/refs/tags/v0.5.0.tar.gz"
  end

  def install
    bin.install "proxyfor.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/proxyfor --version 2>&1")
  end
end
