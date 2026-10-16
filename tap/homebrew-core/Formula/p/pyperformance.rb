class Pyperformance < Formula
  include Language::Python::Virtualenv

  desc "Python benchmark suite"
  homepage "https://github.com/python/pyperformance"
  url "https://files.pythonhosted.org/packages/9e/bc/ebc48f2af24abe0d13681ac7ccb21c3f0ed0128361522f5904d4f21953e9/pyperformance-1.14.0.tar.gz"
  sha256 "91f74393997b604375ad5b79bf569a24076d70181c076a53abad5383a238a8aa"
  license "MIT"
  head "https://github.com/python/pyperformance.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f66d07d321a3bb729705abfc7fa86f77514274eb64b16b4979c688169c3ec1c6"
  end

  depends_on "python@3.15"

  resource "packaging" do
    url "https://files.pythonhosted.org/packages/7d/fa/3944b40b07da9ce895c0e6303a5ab7d53da063554f534556b134a54d6093/packaging-26.3.tar.gz"
    sha256 "94edc256424af38762eb31306eed28beb9f0efc50a8837492c9d6fd6004aed79"
  end

  resource "psutil" do
    url "https://files.pythonhosted.org/packages/aa/c6/d1ddf4abb55e93cebc4f2ed8b5d6dbad109ecb8d63748dd2b20ab5e57ebe/psutil-7.2.2.tar.gz"
    sha256 "0746f5f8d406af344fd547f1c8daa5f5c33dbc293bb8d6a16d80b4bb88f59372"
  end

  resource "pyperf" do
    url "https://files.pythonhosted.org/packages/16/91/39ca77aa58f13e8c65d747ac7e06584b55acabfa98987fb8d546bc24860d/pyperf-2.10.0.tar.gz"
    sha256 "dd93ccfda79214725293e95f1fa6e00cb4a64adcf1326039486d4e1f91caaa62"
  end

  def install
    virtualenv_install_with_resources
  end

  test do
    list_output = shell_output("#{bin}/pyperformance list --benchmarks nbody")
    assert_match "'nbody' benchmarks:", list_output
    assert_match "- nbody", list_output

    groups_output = shell_output("#{bin}/pyperformance list_groups")
    assert_match "tags:", groups_output
  end
end
