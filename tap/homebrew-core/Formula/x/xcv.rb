class Xcv < Formula
  desc "Cut, copy and paste files with Bash"
  homepage "https://github.com/busterc/xcv"
  url "https://github.com/busterc/xcv/archive/refs/tags/v1.0.1.tar.gz"
  sha256 "f2898f78bb05f4334073adb8cdb36de0f91869636a7770c8e955cee8758c0644"
  license "ISC"
  head "https://github.com/busterc/xcv.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/xcv-1.0.1"
    sha256 cellar: :any, x86_64_windows: "11eeca37f3674c0454e6d6e360acef691df01279c29b3e2d0add9c2e70d1a0e0"
  end

  def install
    bin.install "xcv"
  end

  test do
    system bin/"xcv"
  end
end
