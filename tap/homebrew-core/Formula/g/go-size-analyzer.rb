class GoSizeAnalyzer < Formula
  desc "Analyzing the dependencies in compiled Golang binaries"
  homepage "https://gsa.zxilly.dev/"
  url "https://github.com/Zxilly/go-size-analyzer/releases/download/v1.14.1/go-size-analyzer_1.14.1_windows_amd64.zip"
  sha256 "879ec5684e9984fda77a0a7717c2b221ccb4667a59b1dfd7a0d280d49c35533f"
  license "AGPL-3.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Zxilly/go-size-analyzer/archive/refs/tags/v1.14.1.tar.gz"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    bin.install "gsa.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gsa --version 2>&1")
  end
end
