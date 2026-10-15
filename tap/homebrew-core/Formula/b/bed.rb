class Bed < Formula
  desc "Binary editor written in Go"
  homepage "https://github.com/itchyny/bed"
  url "https://github.com/itchyny/bed/releases/download/v0.2.8/bed_v0.2.8_windows_amd64.zip"
  sha256 "6cf717ab1569eac41cf2ff6630062b497815750da37d50f9384cea6710a7788d"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/itchyny/bed/archive/refs/tags/v0.2.8.tar.gz"
  end

  def install
    bin.install "bed.exe"
  end

  test do
    # bed is a TUI application
    assert_match version.to_s, shell_output("#{bin}/bed -version")
  end
end
