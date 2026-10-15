class PkgConfig < Formula
  desc "Manage compile and link flags for libraries"
  homepage "https://github.com/pkgconf/pkgconf"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-pkgconf-1~3.0.8-1-any.pkg.tar.zst"
  version "3.0.8"
  sha256 "d3f54c0ce414bf3ecf9792417d4e49075881a7ce2c5149d5107c3e00ead4ed92"
  license "ISC"

  livecheck do
    url "https://distfiles.ariadne.space/pkgconf/"
    regex(/href=.*?pkgconf[._-]v?(\d+\.\d+(?:\.[1-8]?\d(?:\.\d+)*)?)\.t/i)
  end

  # pkgconf implementation, following Homebrew pkg-config compatibility naming.

  def install
    install_tree
    windows_exe "libexec/ucrt64/bin/pkgconf.exe", "pkg-config"
    windows_exe "libexec/ucrt64/bin/pkgconf.exe", "pkgconf"
  end

  test do
    (testpath/"foo.pc").write <<~PC
      prefix=/usr
      exec_prefix=${prefix}
      includedir=${prefix}/include
      libdir=${exec_prefix}/lib

      Name: foo
      Description: The foo library
      Version: 1.0.0
      Cflags: -I${includedir}/foo
      Libs: -L${libdir} -lfoo
    PC

    ENV["PKG_CONFIG_LIBDIR"] = testpath
    system bin/"pkgconf", "--validate", "foo"
    assert_equal "1.0.0", shell_output("#{bin}/pkgconf --modversion foo").strip
    assert_equal "-lfoo", shell_output("#{bin}/pkgconf --libs-only-l foo").strip
    assert_equal "-I/usr/include/foo", shell_output("#{bin}/pkgconf --cflags foo").strip
    assert_equal "1.0.0", shell_output("#{bin}/pkg-config --modversion foo").strip
  end
end
