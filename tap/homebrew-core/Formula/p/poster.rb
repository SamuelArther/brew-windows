class Poster < Formula
  desc "Create large posters out of PostScript pages"
  homepage "https://schrfr.github.io/poster/"
  url "https://github.com/schrfr/poster/archive/refs/tags/1.0.0.tar.gz"
  sha256 "1df49dfd4e50ffd66e0b6e279b454a76329a36280e0dc73b08e5b5dcd5cff451"
  license "GPL-2.0-or-later"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/poster-1.0.0"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "af1d60c7269b1e7a6a5c64c8d37ea6688579c7bcab32e7ab2bb402dfe86bd2e4"
  end

  def install
    system "make"
    bin.install "poster"
    man1.install "poster.1"
  end

  test do
    system bin/"poster", test_fixtures("test.ps")
  end
end
