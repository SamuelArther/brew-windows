class Csvlens < Formula
  desc "Command-line csv viewer"
  homepage "https://github.com/YS-L/csvlens"
  url "https://github.com/YS-L/csvlens/releases/download/v0.15.1/csvlens-x86_64-pc-windows-msvc.zip"
  sha256 "2b7cf7818288b98571382f0b65b8d03139ab2a1d71d2505ecdeb06a33186bf7d"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/YS-L/csvlens/archive/refs/tags/v0.15.1.tar.gz"
  end

  def install
    bin.install "csvlens.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/csvlens --version 2>&1")
  end
end
