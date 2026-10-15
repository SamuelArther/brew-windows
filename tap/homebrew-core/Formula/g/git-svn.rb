class GitSvn < Formula
  desc "Bidirectional operation between a Subversion repository and Git"
  homepage "https://git-scm.com"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-git-svn-2.56.0.2-1-any.pkg.tar.zst"
  sha256 "060b2b626f187a0440a3d7ee3180e2d94677d6489c83e631f4b5ed798a651664"
  license "GPL-2.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://mirrors.edge.kernel.org/pub/software/scm/git/git-2.56.0.tar.xz"
  end

  def install
    install_tree
    install_library
  end

  test do
    assert_path_exists share/"doc/git-doc/git-svn.adoc"
  end
end
