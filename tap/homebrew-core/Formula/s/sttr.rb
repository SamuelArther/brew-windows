class Sttr < Formula
  desc "CLI to perform various operations on string"
  homepage "https://github.com/abhimanyu003/sttr"
  url "https://github.com/abhimanyu003/sttr/releases/download/v0.2.30/sttr_Windows_x86_64.zip"
  sha256 "3f15bd8c7801fd2c5b10b86129e636bf1d39a1521229c285f01492c1145c21a1"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/abhimanyu003/sttr/archive/refs/tags/v0.2.30.tar.gz"
  end

  def install
    bin.install "sttr.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/sttr version 2>&1")
  end
end
