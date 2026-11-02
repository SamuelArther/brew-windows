class Ncftp < Formula
  desc "FTP client with an advanced user interface"
  homepage "https://www.ncftp.com/"
  url "https://www.ncftp.com/public_ftp/ncftp/ncftp-3.3.0-src.tar.gz"
  mirror "https://fossies.org/linux/misc/ncftp-3.3.0-src.tar.gz"
  sha256 "7920f884c2adafc82c8e41c46d6f3d22698785c7b3f56f5677a8d5c866396386"
  license "ClArtistic"

  livecheck do
    url "https://www.ncftp.com/download/"
    regex(/href=.*?ncftp[._-]v?(\d+(?:\.\d+)+)(?:-src)?\.t/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "68a4585a10a38a42725851e0a7eb8d8fd7a6cdc00385e6c9a151876250ffc463"
  end

  uses_from_macos "ncurses"

  deny_network_access!

  def install
    # Fix compile with newer Clang
    ENV.append_to_cflags "-Wno-implicit-function-declaration" if DevelopmentTools.clang_build_version >= 1200

    system "./configure", "--disable-universal",
                          "--disable-precomp",
                          "--with-ncurses",
                          "--mandir=#{man}",
                          *std_configure_args
    system "make"
    system "make", "install"
  end

  test do
    system bin/"ncftp", "-F"
  end
end
