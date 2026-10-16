class Jello < Formula
  include Language::Python::Virtualenv

  desc "Filter JSON and JSON Lines data with Python syntax"
  homepage "https://github.com/kellyjonbrazil/jello"
  url "https://files.pythonhosted.org/packages/fb/5e/fe41641ce367cb8b57a6514372fffd7aed4a8c916cd7dc0bb7e9ae8f6ae2/jello-1.6.1.tar.gz"
  sha256 "eee1d43f2d9bb3b3b8c857b713e56191badb9a03a2274defaad1e727fad35521"
  license "MIT"
  revision 1

  bottle do
    sha256 cellar: :any, x86_64_windows: "8b3fffacf42a624832b35d598a8ee048b7ea8a3272010c062fe063441f7fd077"
  end

  depends_on "python@3.15"

  resource "pygments" do
    url "https://files.pythonhosted.org/packages/49/2e/ced460408999b33da6b31b0021b0f37d329e202d4169aeb164493778f25b/pygments-2.21.0.tar.gz"
    sha256 "610ca751c9bc2492b38eb9a38a7fbc93edbbb2d7182edaf34e66ae493dee5c8c"
  end

  def install
    virtualenv_install_with_resources
    man1.install "man/jello.1"
  end

  test do
    assert_equal "1\n", pipe_output("#{bin}/jello _.foo", "{\"foo\":1}")
  end
end
