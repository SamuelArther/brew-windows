class Ekg2 < Formula
  desc "Multiplatform, multiprotocol, plugin-based instant messenger"
  homepage "https://github.com/ekg2/ekg2"
  license "GPL-2.0-only"
  revision 4

  stable do
    url "https://src.fedoraproject.org/lookaside/extras/ekg2/ekg2-0.3.1.tar.gz/68fc05b432c34622df6561eaabef5a40/ekg2-0.3.1.tar.gz"
    mirror "https://web.archive.org/web/20161227025528/pl.ekg2.org/ekg2-0.3.1.tar.gz"
    sha256 "6ad360f8ca788d4f5baff226200f56922031ceda1ce0814e650fa4d877099c63"

    # Fix the build on OS X 10.9+
    # bugs.ekg2.org/issues/152 [LOST LINK]
    patch do
      file "Patches/ekg2/0.3.1.patch"
      type :unofficial
    end

    # Upstream commit, fix build against OpenSSL 1.1
    patch do
      url "https://github.com/ekg2/ekg2/commit/f05815.patch?full_index=1"
      sha256 "207639edc5e6576c8a67301c63f0b28814d9885f0d4fca5d9d9fc465f4427cd7"
      type :backport
    end
  end

  livecheck do
    url :homepage
    regex(/^ekg2[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  head do
    url "https://github.com/ekg2/ekg2.git", branch: "master"

    depends_on "autoconf" => :build
    depends_on "automake" => :build
    depends_on "gettext" => :build
    depends_on "libtool" => :build
    depends_on "glib"

    on_macos do
      depends_on "gettext"
    end
  end

  # Original source tarball is gone and we use Fedora copy but they already dropped package.
  # ekg2 was also removed from other major distros like Debian/Ubuntu and Gentoo.
  # Last release on 2011-03-17 and last commit on 2019-03-15.
  deprecate! date: "2025-09-13", because: :unmaintained
  disable! date: "2026-05-18", because: :unmaintained

  depends_on "pkgconf" => :build
  depends_on "openssl@3"
  depends_on "readline"

  def install
    # Workaround for Xcode 15
    if DevelopmentTools.clang_build_version >= 1500
      ENV.append_to_cflags "-Wno-implicit-function-declaration"
      ENV.append_to_cflags "-Wno-incompatible-function-pointer-types"
    end

    # Workaround for newer readline
    ENV.append_to_cflags "-DWANT_OBSOLETE_TYPEDEFS" if build.stable?

    args = %W[
      --enable-unicode
      --with-readline=#{formula_opt_prefix("readline")}
      --without-gtk
      --without-libgadu
      --without-perl
      --without-python
      --without-nls
    ]
    if OS.linux?
      # Newer ncurses has opaque structures so old plugin code no longer works
      args << "--without-ncurses"
      # Help old config scripts identify arm64 linux
      args << "--build=aarch64-unknown-linux-gnu" if Hardware::CPU.arm? && Hardware::CPU.is_64_bit? && build.stable?
    end

    configure = build.head? ? "./autogen.sh" : "./configure"
    system configure, *args, *std_configure_args
    system "make", "install"
  end

  test do
    system bin/"ekg2", "--help"
  end
end
