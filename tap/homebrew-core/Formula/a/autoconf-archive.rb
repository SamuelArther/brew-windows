class AutoconfArchive < Formula
  desc "Collection of over 500 reusable autoconf macros"
  homepage "https://savannah.gnu.org/projects/autoconf-archive/"
  url "https://repo.msys2.org/msys/x86_64/autoconf-archive-2024.10.16-1-any.pkg.tar.zst"
  sha256 "0c3620d40a546b7210c0bacff2c5aa34de3ce3d8b2c7fefcb719fceefd7da9d9"
  license "GPL-3.0-or-later"

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://ftpmirror.gnu.org/autoconf-archive/autoconf-archive-2024.10.16.tar.xz"
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    assert_path_exists share/"aclocal/ax_absolute_header.m4"
  end
end
