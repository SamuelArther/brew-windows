class Help2man < Formula
  desc "Automatically generate simple man pages"
  homepage "https://www.gnu.org/software/help2man/"
  url "https://repo.msys2.org/msys/x86_64/help2man-1.49.3-2-x86_64.pkg.tar.zst"
  sha256 "5fdc4366dddf7c5bcc39b5c01fcc076e811324987acf6b82f278ad8c5bf41d01"
  license "GPL-3.0-or-later"

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://ftpmirror.gnu.org/help2man/help2man-1.49.3.tar.xz"
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists bin/"help2man"
  end
end
