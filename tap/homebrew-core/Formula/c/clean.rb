class Clean < Formula
  desc "Search for files matching a regex and delete them"
  homepage "https://clean.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/clean/clean/3.4/clean-3.4.tar.bz2"
  sha256 "761f3a9e1ed50747b6a62a8113fa362a7cc74d359ac6e8e30ba6b30d59115320"
  license :public_domain

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/clean-3.4"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "fdc127e2d445cd958f8c84d1bc34ec978527f46a8a3b71e3f5a20caf98db1bf2"
  end

  def install
    system "make"
    bin.install "clean"
    man1.install "clean.1"
  end

  test do
    touch testpath/"backup1234"
    touch testpath/"backup1234.testing-rm"

    system bin/"clean", "-f", "-l", "-e", "*.testing-rm"
    assert_path_exists testpath/"backup1234"
    refute_path_exists testpath/"backup1234.testing-rm"
  end
end
