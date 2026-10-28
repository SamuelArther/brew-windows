class Tox < Formula
  include Language::Python::Virtualenv

  desc "Generic Python virtualenv management and test command-line tool"
  homepage "https://tox.wiki/en/latest/"
  url "https://files.pythonhosted.org/packages/2c/cc/590d979d858960bad441d1b3b6d5f898806132ff2b25a6cbdcbfadb0ceea/tox-4.67.2.tar.gz"
  sha256 "be272aa5b9503fc084c82da242df124835e23cb86fc337f8e33804f011fc3ca5"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "eca35ec1f39482f35cfb8b27c4ff27f6982dbccdb17b7a98234f12fafd3aa733"
  end

  depends_on "python@3.15"

  resource "cachetools" do
    url "https://files.pythonhosted.org/packages/f2/0f/e02be8857e0fc519a12337568f0bf4b0aad4f3a95d7303e9e9d9fbcca8c2/cachetools-7.2.6.tar.gz"
    sha256 "1096513bdeed468ba505df3d123e4578a5a22c955f4a9ba3912fd71f3f95363a"
  end

  resource "colorama" do
    url "https://files.pythonhosted.org/packages/d8/53/6f443c9a4a8358a93a6792e2acffb9d9d5cb0a5cfd8802644b7b1c9a02e4/colorama-0.4.6.tar.gz"
    sha256 "08695f5cb7ed6e0531a20572697297273c47b8cae5a63ffc6d6ed5c201be6e44"
  end

  resource "distlib" do
    url "https://files.pythonhosted.org/packages/68/bc/d36966de7304047a1001a7559fbf266c464bb5d8cc85464536ef8c9bcc32/distlib-0.4.4.tar.gz"
    sha256 "ca4175b4ac7f71223cc30ed7248c45d2b38c2439d8ee6e498ab67ed63cf3508d"
  end

  resource "filelock" do
    url "https://files.pythonhosted.org/packages/d8/28/3893324bcadf8f89b1616637468d766af3f625008c0735bf704fdfa86617/filelock-4.2.4.tar.gz"
    sha256 "c6d8663535e1650ee430507b4ffbac7b464dd64b7647ea0112a9f406499d163d"
  end

  resource "packaging" do
    url "https://files.pythonhosted.org/packages/7d/fa/3944b40b07da9ce895c0e6303a5ab7d53da063554f534556b134a54d6093/packaging-26.3.tar.gz"
    sha256 "94edc256424af38762eb31306eed28beb9f0efc50a8837492c9d6fd6004aed79"
  end

  resource "platformdirs" do
    url "https://files.pythonhosted.org/packages/1a/43/8c4b5fe23f0e119cf4e1b16c2d16240ab9057cdbb138568c91d5a48abf81/platformdirs-4.13.6.tar.gz"
    sha256 "211d176e24674b5895dedf9021c262bcf9fad008a70610bfb147d4a8d6d8fe1b"
  end

  resource "pluggy" do
    url "https://files.pythonhosted.org/packages/bf/db/7fc19e6f2dc92a966727031389fc2e08b558f0f25eb7403c1119ad4713cd/pluggy-1.7.0.tar.gz"
    sha256 "d1eaa46ebb595891b860ab086b4d09c8588af65ebd4361b8e8f4bb8920b90ba8"
  end

  resource "pyproject-api" do
    url "https://files.pythonhosted.org/packages/eb/43/bafd5193a393ec26f963e17303477e555f7ea5e14e20ecd778048b427e85/pyproject_api-1.12.1.tar.gz"
    sha256 "1429612586cd1147a00a6eaddfff731ab1c4eee3bf17b4f234dbd4668540e600"
  end

  resource "python-discovery" do
    url "https://files.pythonhosted.org/packages/b7/ba/2bdce36c8f1b081db630c61b433fcc7efa5094ba621147923232bdaa7332/python_discovery-1.6.5.tar.gz"
    sha256 "f9420b2107a429ec9912bd10039135fc9bb8f15e99a00e614533977529f56bb3"
  end

  resource "tomli-w" do
    url "https://files.pythonhosted.org/packages/19/75/241269d1da26b624c0d5e110e8149093c759b7a286138f4efd61a60e75fe/tomli_w-1.2.0.tar.gz"
    sha256 "2dd14fac5a47c27be9cd4c976af5a12d87fb1f0b4512f81d69cce3b35ae25021"
  end

  resource "virtualenv" do
    url "https://files.pythonhosted.org/packages/ee/00/c27937345311d2aaf6197203231a59010073d7abffae3971503ac0e65477/virtualenv-21.15.0.tar.gz"
    sha256 "41a0cfec413c886a991bf7657d02f8996e69e192485aa127b410f29f1e35cfd3"
  end

  def install
    virtualenv_install_with_resources
  end

  test do
    assert_match "usage", shell_output("#{bin}/tox --help")
    system bin/"tox"
    pyver = Language::Python.major_minor_version(python3).to_s.delete(".")

    system bin/"tox", "quickstart", "src"
    (testpath/"src/test_trivial.py").write <<~PYTHON
      def test_trivial():
          assert True
    PYTHON
    chdir "src" do
      system bin/"tox", "run"
    end
    assert_path_exists testpath/"src/.tox/py#{pyver}"
  end
end
