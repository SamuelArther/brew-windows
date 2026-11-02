class Mikmod < Formula
  desc "Portable tracked music player"
  homepage "https://mikmod.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/mikmod/mikmod/3.2.10/mikmod-3.2.10.tar.gz"
  sha256 "465e99d89d762608b7d0c0a103a58eec68c8c28ae6bbd196354c13433e40d20a"
  license "GPL-2.0-or-later"

  livecheck do
    url :stable
    regex(%r{url=.*?/mikmod[._-](\d+(?:\.\d+)+)\.t}i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "7798ae82771568235cd1b09dbeba51ee30df5e5ce4bde7412c07462b338d6e80"
  end

  depends_on "libmikmod"

  uses_from_macos "ncurses"

  deny_network_access!

  def install
    system "./configure", "--disable-debug",
                          "--disable-dependency-tracking",
                          "--prefix=#{prefix}"
    system "make", "install"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mikmod -V")
  end
end
