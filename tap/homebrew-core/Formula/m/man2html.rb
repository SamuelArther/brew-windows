class Man2html < Formula
  desc "Convert nroff man pages to HTML"
  homepage "https://savannah.nongnu.org/projects/man2html/"
  url "https://repo.msys2.org/msys/x86_64/man2html-3.0.1-2-any.pkg.tar.zst"
  sha256 "a68acb30e910ff10ff13019d6a4bac1190667f2209ce4594b608a638c6f7aef9"
  license "GPL-2.0-or-later"

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://www.mhonarc.org/release/misc/"
    regex(/href=.*?man2html[._-]?v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    assert_path_exists bin/"man.cgi"
  end
end
