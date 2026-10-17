class Liblockfile < Formula
  desc "Library providing functions to lock standard mailboxes"
  homepage "https://tracker.debian.org/pkg/liblockfile"
  url "https://deb.debian.org/debian/pool/main/libl/liblockfile/liblockfile_1.17.orig.tar.gz"
  sha256 "6e937f3650afab4aac198f348b89b1ca42edceb17fb6bb0918f642143ccfd15e"
  license "LGPL-2.0-or-later"

  livecheck do
    url "https://deb.debian.org/debian/pool/main/libl/liblockfile/"
    regex(/href=.*?liblockfile[._-]v?(\d+(?:\.\d+)+)\.orig\.t/i)
  end

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "667f5b93165f3c7e5584b72d486772f8fd646f5eff60a1cef32cb746f7593bfd"
  end

  def install
    # brew runs without root privileges (and the group is named "wheel" anyway)
    inreplace "Makefile.in", " -g root ", " "

    args = %W[
      --sysconfdir=#{etc}
      --mandir=#{man}
    ]
    args << "--with-mailgroup=staff" if OS.mac?

    system "./configure", *std_configure_args, *args
    bin.mkpath
    lib.mkpath
    include.mkpath
    man1.mkpath
    man3.mkpath
    system "make"
    system "make", "install"
  end

  test do
    system bin/"dotlockfile", "-l", "locked"
    assert_path_exists testpath/"locked"
    system bin/"dotlockfile", "-u", "locked"
    refute_path_exists testpath/"locked"
  end
end
