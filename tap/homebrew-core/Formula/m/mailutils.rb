class Mailutils < Formula
  desc "Swiss Army knife of email handling"
  homepage "https://mailutils.org/"
  url "https://ftpmirror.gnu.org/mailutils/mailutils-3.21.tar.gz"
  mirror "https://ftp.gnu.org/gnu/mailutils/mailutils-3.21.tar.gz"
  sha256 "5e305de7fcf2f744c8b210f1cfe904d49842bfc6d13a913031ec4dbf0c669c54"
  license "GPL-3.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "4f764a26d3c4b764cfcf94d94f8b1d2dbb6bf3f31f2e616ea5f00cb7f62183d8"
  end

  depends_on "gdbm"
  depends_on "gnutls"
  depends_on "gsasl"
  depends_on "libtool"
  depends_on "libunistring"
  depends_on "readline"

  uses_from_macos "libxcrypt"

  on_macos do
    depends_on "gettext"
  end

  def install
    # This is hardcoded to be owned by `root`, but we have no privileges on installation.
    inreplace buildpath.glob("dotlock/Makefile.*") do |s|
      s.gsub! "chown root:mail", "true"
      s.gsub! "chmod 2755", "chmod 755"
    end

    system "./configure", "--disable-mh",
                          "--disable-python",
                          "--disable-silent-rules",
                          "--without-fribidi",
                          "--without-guile",
                          "--without-tokyocabinet",
                          *std_configure_args
    system "make", "install"
  end

  test do
    system bin/"movemail", "--version"
  end
end
