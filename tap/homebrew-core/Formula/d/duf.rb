class Duf < Formula
  desc "Disk Usage/Free Utility - a better 'df' alternative"
  homepage "https://github.com/muesli/duf"
  url "https://github.com/muesli/duf/releases/download/v0.9.1/duf_0.9.1_windows_x86_64.zip"
  sha256 "503934be81f847d9ddb1b739834217480633435ad16515dd199e372c0b2e1afc"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/muesli/duf/archive/refs/tags/v0.9.1.tar.gz"
  end

  def install
    bin.install "duf.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/duf --version 2>&1")
  end
end
