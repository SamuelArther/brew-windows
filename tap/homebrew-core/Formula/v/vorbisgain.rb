class Vorbisgain < Formula
  desc "Add Replay Gain volume tags to Ogg Vorbis files"
  homepage "https://sjeng.org/vorbisgain.html"
  url "https://sjeng.org/ftp/vorbis/vorbisgain-0.37.tar.gz"
  sha256 "dd6db051cad972bcac25d47b4a9e40e217bb548a1f16328eddbb4e66613530ec"
  license "LGPL-2.1-only"

  livecheck do
    url "https://sjeng.org/ftp/vorbis/"
    regex(/href=.*?vorbisgain[._-]v?(\d+(?:\.\d+)+)\.(?:t|zip)/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "b148230b69db9e35d5be3ed7eccdb1d1a5dd0a16f2400be289ecab407fcc28f4"
  end

  depends_on "libogg"
  depends_on "libvorbis"

  deny_network_access!

  def install
    # fix implicit-function-declaration errors
    ENV.append_to_cflags "-DGWINSZ_IN_SYS_IOCTL"
    inreplace "misc.c", "#include <string.h>", "#include <string.h>\n#include <unistd.h>"

    system "./configure", "--prefix=#{prefix}", "--mandir=#{man}"
    system "make", "install"
  end

  test do
    system bin/"vorbisgain", "--version"
  end
end
