class Libtirpc < Formula
  desc "Port of Sun's Transport-Independent RPC library to Linux"
  homepage "https://sourceforge.net/projects/libtirpc/"
  url "https://repo.msys2.org/msys/x86_64/libtirpc-1.3.8-1-x86_64.pkg.tar.zst"
  sha256 "53252533ec393e9d4cd33a3302d21af4aa4d058174b165911207d80e41019450"
  license "BSD-3-Clause"

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://downloads.sourceforge.net/project/libtirpc/libtirpc/1.3.8/libtirpc-1.3.8.tar.bz2"
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    assert_path_exists bin/"msys-tirpc-3.dll"
  end
end
