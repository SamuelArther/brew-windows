class Navi < Formula
  desc "Interactive cheatsheet tool for the command-line"
  homepage "https://github.com/denisidoro/navi"
  url "https://github.com/denisidoro/navi/releases/download/v2.24.0/navi-v2.24.0-x86_64-pc-windows-gnu.zip"
  sha256 "db5b4babe00b93de5636c07df3d8c149972373a69f131e686b0c565051651bb9"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/denisidoro/navi/archive/refs/tags/v2.24.0.tar.gz"
  end

  def install
    bin.install "navi.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/navi --version 2>&1")
  end
end
