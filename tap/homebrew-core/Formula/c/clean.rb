class Clean < Formula
  desc "Search for files matching a regex and delete them"
  homepage "https://clean.sourceforge.net/"
  url "https://downloads.sourceforge.net/project/clean/clean/3.4/clean-3.4.tar.bz2"
  sha256 "761f3a9e1ed50747b6a62a8113fa362a7cc74d359ac6e8e30ba6b30d59115320"
  license :public_domain

  deny_network_access!

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "8d3279da6d2d078846ceefeff9ea3f6cbe66aff2c2c4848dac00b4c1125334b5"
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
