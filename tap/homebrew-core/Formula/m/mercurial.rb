class Mercurial < Formula
  desc "Scalable distributed version control system"
  homepage "https://mercurial-scm.org/"
  url "https://repo.msys2.org/msys/x86_64/mercurial-7.2.4-1-x86_64.pkg.tar.zst"
  sha256 "8342f06a4eca1d94745b76c8022a76d6f942784d94f532317628ac519f0171e6"
  license "GPL-2.0-or-later"
  revision 1

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://www.mercurial-scm.org/release/"
    regex(/href=.*?mercurial[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists bin/"hg"
  end
end
