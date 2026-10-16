class Pdfcpu < Formula
  desc "PDF processor written in Go"
  homepage "https://pdfcpu.io"
  url "https://github.com/pdfcpu/pdfcpu/releases/download/v0.16.2/pdfcpu_0.16.2_Windows_x86_64.zip"
  sha256 "941ef71ea3b8b5385734627a8d60c199486042b66a4a675bd0074695ae11c26d"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/pdfcpu/pdfcpu/archive/refs/tags/v0.16.2.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "pdfcpu.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/pdfcpu version 2>&1")
  end
end
