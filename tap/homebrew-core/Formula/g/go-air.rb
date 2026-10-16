class GoAir < Formula
  desc "Live reload for Go apps"
  homepage "https://github.com/air-verse/air"
  url "https://github.com/air-verse/air/releases/download/v1.67.4/air_1.67.4_windows_amd64.tar.gz"
  sha256 "c35dbd568cff98055cc382d003bfda06bcdae82389568fb5e60ce5da278fa79d"
  license "GPL-3.0-or-later"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/air-verse/air/archive/refs/tags/v1.67.4.tar.gz"
  end

  conflicts_with "air", because: "both install binaries with the same name"

  def install
    bin.install "air.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/air -v 2>&1")
  end
end
