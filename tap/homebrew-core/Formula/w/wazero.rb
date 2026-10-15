class Wazero < Formula
  desc "Zero dependency WebAssembly runtime"
  homepage "https://wazero.io"
  url "https://github.com/wazero/wazero/releases/download/v1.12.0/wazero_1.12.0_windows_amd64.zip"
  sha256 "32342e7e6ffc6d1016ae35e9e15479ef718076f27afdad1e89bb1bb6796efeee"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/wazero/wazero/archive/refs/tags/v1.12.0.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "wazero.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/wazero version 2>&1")
  end
end
