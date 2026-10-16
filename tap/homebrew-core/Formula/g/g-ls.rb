class GLs < Formula
  desc "Powerful and cross-platform ls"
  homepage "https://equationzhao.github.io/g/"
  url "https://github.com/equationzhao/g/releases/download/v0.31.2/g-Windows-amd64.zip"
  sha256 "7dd6fdfec9ee9c8633607231323be23e7f59c089715c45df6946614e21cc6a18"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Equationzhao/g/archive/refs/tags/v0.31.2.tar.gz"
  end

  def install
    bin.install "g-windows-amd64.exe" => "g.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/g --version 2>&1")
  end
end
