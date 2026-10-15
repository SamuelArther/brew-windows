class GolangciLint < Formula
  desc "Fast linters runner for Go"
  homepage "https://golangci-lint.run/"
  url "https://github.com/golangci/golangci-lint/releases/download/v2.14.0/golangci-lint-2.14.0-windows-amd64.zip"
  sha256 "b15f903d1649283a2ab6e8d2f0c317eaf1f9f6b3763950e7e8b7e72a43dd724f"
  license "GPL-3.0-only"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "golangci-lint.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/golangci-lint --version")
  end
end
