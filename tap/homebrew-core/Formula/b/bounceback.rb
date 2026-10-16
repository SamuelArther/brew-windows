class Bounceback < Formula
  desc "Stealth redirector for red team operation security"
  homepage "https://github.com/D00Movenok/BounceBack"
  url "https://github.com/D00Movenok/BounceBack/releases/download/v1.5.3/BounceBack_1.5.3_windows_amd64.zip"
  sha256 "ba01af84fa0b0b9f66c3a5586eb7eda5d9ccb0b71a71d552c665d058e2d71967"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/D00Movenok/BounceBack/archive/refs/tags/v1.5.3.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/bounceback.exe", "bounceback"
  end

  test do
    assert_path_exists bin/"bounceback.exe"
  end
end
