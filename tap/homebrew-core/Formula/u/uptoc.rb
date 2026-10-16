class Uptoc < Formula
  desc "Convenient static file deployment tool that supports multiple platforms"
  homepage "https://github.com/bonaysoft/uptoc"
  url "https://github.com/bonaysoft/uptoc/releases/download/v1.4.3/uptoc-windows-amd64.zip"
  sha256 "7896f907b3d03815a2257d593e4f600ab825a329393ac1c74dea9c57644ad9d9"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/bonaysoft/uptoc.git"
  end

  def install
    bin.install "uptoc.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/uptoc -v 2>&1")
    assert_match "uptoc config", shell_output("#{bin}/uptoc ./abc 2>&1", 1)
  end
end
