class Mussh < Formula
  desc "Multi-host SSH wrapper"
  homepage "https://mussh.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/mussh/mussh/1.0/mussh-1.0.tgz"
  sha256 "6ba883cfaacc3f54c2643e8790556ff7b17da73c9e0d4e18346a51791fedd267"
  license "GPL-1.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "fe0760f0d5b9c30608de04a0633c1ad01da6adb3c38a0d9f8c511f98710fa6bd"
  end

  def install
    bin.install "mussh"
    man1.install "mussh.1"
  end

  test do
    system bin/"mussh", "--help"
  end
end
