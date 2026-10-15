class Biodiff < Formula
  desc "Hex diff viewer using alignment algorithms from biology"
  homepage "https://github.com/8051Enthusiast/biodiff"
  url "https://github.com/8051Enthusiast/biodiff/releases/download/v1.2.1/biodiff-windows-1.2.1.zip"
  sha256 "51b23a190a21e089a5108b1ed72b04c8c45222bed1934b259d4d2a6355f1bbc3"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/8051Enthusiast/biodiff.git"
  end

  def install
    bin.install "biodiff.exe"
    bin.install "git-biodiff.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/biodiff --version 2>&1")
  end
end
