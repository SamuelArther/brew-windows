class Ninja < Formula
  desc "Small build system for use with gyp or CMake"
  homepage "https://ninja-build.org/"
  url "https://github.com/ninja-build/ninja/releases/download/v1.13.2/ninja-win.zip"
  sha256 "07fc8261b42b20e71d1720b39068c2e14ffcee6396b76fb7a795fb460b78dc65"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "ninja.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/ninja --version")
  end
end
