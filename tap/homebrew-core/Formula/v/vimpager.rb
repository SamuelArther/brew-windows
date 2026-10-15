class Vimpager < Formula
  desc "Use ViM as PAGER"
  homepage "https://github.com/rkitover/vimpager"
  url "https://repo.msys2.org/msys/x86_64/vimpager-2.06.r382.gce697c4-1-any.pkg.tar.zst"
  sha256 "fa19ed601790af6a6f21acf25a914d0e3bd21da69a0fdf3efb8df63301bbf2bf"
  license "BSD-2-Clause"

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/rkitover/vimpager/archive/refs/tags/2.06.tar.gz"
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    assert_path_exists bin/"vimcat"
  end
end
