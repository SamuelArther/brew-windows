class Ghorg < Formula
  desc "Quickly clone an entire org's or user's repositories into one directory"
  homepage "https://github.com/gabrie30/ghorg"
  url "https://github.com/gabrie30/ghorg/releases/download/v1.11.16/ghorg_1.11.16_Windows_x86_64.tar.gz"
  sha256 "d272ee08f1c83bbbb1a3d0a31255e97cff97153c3b0c0a712cc2ccb93601adbb"
  license "Apache-2.0"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/gabrie30/ghorg/archive/refs/tags/v1.11.16.tar.gz"
  end

  def install
    bin.install "ghorg.exe"
  end

  test do
    assert_match "No clones found", shell_output("#{bin}/ghorg ls")
  end
end
