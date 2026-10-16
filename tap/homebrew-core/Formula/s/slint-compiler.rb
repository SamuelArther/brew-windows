class SlintCompiler < Formula
  desc "Compiler for the Slint UI markup language"
  homepage "https://slint.dev/"
  url "https://github.com/slint-ui/slint/releases/download/v1.18.1/slint-compiler-Windows-AMD64.tar.gz"
  sha256 "c409d28d3875a0026e76b410e9885cf193531ae6dac9c885d9b6549a7d3f4cf1"
  license "GPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/slint-ui/slint/archive/refs/tags/v1.18.1.tar.gz"
  end

  def install
    bin.install "slint-compiler.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/slint-compiler --version 2>&1")
  end
end
