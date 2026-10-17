class Ttf2pt1 < Formula
  desc "True Type Font to Postscript Type 1 converter"
  homepage "https://ttf2pt1.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/ttf2pt1/ttf2pt1/3.4.4/ttf2pt1-3.4.4.tgz"
  sha256 "ae926288be910073883b5c8a3b8fc168fde52b91199fdf13e92d72328945e1d0"
  # Uses a BSD license that removes a different clause from BSD-4-Clause than BSD-3-Clause,
  # i.e. it omits the non-endorsement clause (4) and retains the acknowledgement clause (3)
  license :cannot_represent

  livecheck do
    url :stable
    regex(%r{url=.*?/ttf2pt1[._-]v?(\d+(?:\.\d+)+)\.t}i)
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/ttf2pt1-3.4.4"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "04bac45e9b8bd5bb39a7288c316d4551ddf74ff0e8e5dcb2d335032dc208a2d1"
  end

  deny_network_access!

  def install
    # CFLAGS_SYS is used to fix compilation with newer Clang
    system "make", "all", "INSTDIR=#{prefix}", "CFLAGS_SYS=-Wno-implicit-function-declaration"
    bin.install "ttf2pt1"
    man1.install "ttf2pt1.1"
  end

  test do
    assert_match "Cannot open file '/not/a/file'", shell_output("#{bin}/ttf2pt1 -e /not/a/file 2>&1", 1)
  end
end
