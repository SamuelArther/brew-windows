class Xcv < Formula
  desc "Cut, copy and paste files with Bash"
  homepage "https://github.com/busterc/xcv"
  url "https://github.com/busterc/xcv/archive/refs/tags/v1.0.1.tar.gz"
  sha256 "f2898f78bb05f4334073adb8cdb36de0f91869636a7770c8e955cee8758c0644"
  license "ISC"
  head "https://github.com/busterc/xcv.git", branch: "master"

  deny_network_access!

  bottle do
    sha256 cellar: :any, x86_64_windows: "d45ad0b646bc72e6ebc8f72da02a536581bb4c4a09fabaa06c58a90ce32d15a6"
  end

  def install
    bin.install "xcv"
  end

  test do
    system bin/"xcv"
  end
end
