class Ydiff < Formula
  include Language::Python::Virtualenv

  desc "View colored diff with side by side and auto pager support"
  homepage "https://github.com/ymattw/ydiff"
  url "https://files.pythonhosted.org/packages/25/08/93b7e744645d94ae2f5dcddbbd2cfd5ec1420768994987a21a10b226be86/ydiff-1.5.tar.gz"
  sha256 "dd5272afd7409902678c1ac6d1945d73cf85104ac6cda1e67684a67617918a20"
  license "BSD-3-Clause"

  bottle do
    sha256 cellar: :any, x86_64_windows: "1bcdef6808900812257c1e6206869173c955c185ef2e14139b4762f2a5754318"
  end

  depends_on "python@3.15"

  allow_network_access! :build

  def install
    virtualenv_install_with_resources
  end

  test do
    diff_fixture = test_fixtures("test.diff").read
    assert_equal diff_fixture,
      pipe_output("#{bin}/ydiff -cnever", diff_fixture)
  end
end
