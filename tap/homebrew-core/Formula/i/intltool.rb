class Intltool < Formula
  desc "String tool"
  homepage "https://wiki.freedesktop.org/www/Software/intltool"
  url "https://repo.msys2.org/msys/x86_64/intltool-0.51.0-5-x86_64.pkg.tar.zst"
  sha256 "4023842bde4f234e298b87b610d01920bf523a0305e0283653af1462370fc3b0"
  license "GPL-2.0-or-later"
  revision 2

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://launchpad.net/intltool/trunk/0.51.0/+download/intltool-0.51.0.tar.gz"
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    assert_path_exists bin/"intltool-extract"
  end
end
