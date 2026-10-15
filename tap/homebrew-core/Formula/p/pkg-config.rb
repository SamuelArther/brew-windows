class PkgConfig < Formula
  desc "Manage compile and link flags for libraries"
  homepage "https://github.com/pkgconf/pkgconf"
  version "3.0.8"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-pkgconf-1~3.0.8-1-any.pkg.tar.zst"
  sha256 "d3f54c0ce414bf3ecf9792417d4e49075881a7ce2c5149d5107c3e00ead4ed92"
  license "ISC"

  # pkgconf implementation, following Homebrew pkg-config compatibility naming.

  def install
    install_tree
    windows_exe "libexec/ucrt64/bin/pkgconf.exe", "pkg-config"
    windows_exe "libexec/ucrt64/bin/pkgconf.exe", "pkgconf"
  end
end
