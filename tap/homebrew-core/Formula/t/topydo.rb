class Topydo < Formula
  include Language::Python::Virtualenv

  desc "Todo list application using the todo.txt format"
  homepage "https://github.com/topydo/topydo"
  url "https://files.pythonhosted.org/packages/11/6a/8278ac5a59ec633322dcdff278bf74ce8c9d513370cf41d1ecfa5e0376d0/topydo-0.16.tar.gz"
  sha256 "64a12a990ee39950f0a6e14a5c18e6b35c9e4e4f11cfa40c613d5feb4f086b75"
  license "GPL-3.0-or-later"

  bottle do
    sha256 cellar: :any, x86_64_windows: "d0b669e822bef35f6c75151203153cedc4b2e947426504e9c9ffaff9bc0e8f5d"
  end

  depends_on "python@3.15"

  pypi_packages package_name: "topydo[columns,prompt]"

  resource "arrow" do
    url "https://files.pythonhosted.org/packages/b9/33/032cdc44182491aa708d06a68b62434140d8c50820a087fac7af37703357/arrow-1.4.0.tar.gz"
    sha256 "ed0cc050e98001b8779e84d461b0098c4ac597e88704a655582b21d116e526d7"
  end

  # Windows: topydo imports colorama there (`colorama; sys_platform == "win32"`).
  resource "colorama" do
    url "https://files.pythonhosted.org/packages/d8/53/6f443c9a4a8358a93a6792e2acffb9d9d5cb0a5cfd8802644b7b1c9a02e4/colorama-0.4.6.tar.gz"
    sha256 "08695f5cb7ed6e0531a20572697297273c47b8cae5a63ffc6d6ed5c201be6e44"
  end

  resource "prompt-toolkit" do
    url "https://files.pythonhosted.org/packages/7d/ea/39b988c938f75cb75d7045b5c69f8bfed47ee2152c8837fb403de29d6fb8/prompt_toolkit-3.0.53.tar.gz"
    sha256 "9ec8a0ad96d5c56148b3f914aa79c1564c3fde5d2e6b876e7bc327e353cf8fa6"
  end

  resource "python-dateutil" do
    url "https://files.pythonhosted.org/packages/66/c0/0c8b6ad9f17a802ee498c46e004a0eb49bc148f2fd230864601a86dcf6db/python-dateutil-2.9.0.post0.tar.gz"
    sha256 "37dd54208da7e1cd875388217d5e00ebd4179249f90fb72437e91a35459a0ad3"
  end

  resource "six" do
    url "https://files.pythonhosted.org/packages/94/e7/b2c673351809dca68a0e064b6af791aa332cf192da575fd474ed7d6f16a2/six-1.17.0.tar.gz"
    sha256 "ff70335d468e7eb6ec65b95b99d3a2836546063f63acc5171de367e834932a81"
  end

  resource "typing-extensions" do
    url "https://files.pythonhosted.org/packages/f6/cc/6253133b5bb138fc3306cebfbda2c520f545d36b5be2c7255cc528bb45d6/typing_extensions-4.16.0.tar.gz"
    sha256 "dc983d19a509c94dba722ee6abd33940f7c05a89e243c47e907eb4db6f1a43e5"
  end

  resource "tzdata" do
    url "https://files.pythonhosted.org/packages/d9/68/f1b440335057bfce71b6e50a9d09445aa2ecbd08359a337976627b8409e7/tzdata-2026.5.tar.gz"
    sha256 "8cc73c0a0bfca7dbfa59235d60b2eff82231dee33f53d206db1acd9173cfc0a7"
  end

  resource "urwid" do
    url "https://files.pythonhosted.org/packages/3a/df/eb5dcbdf1638c38618096382b4a30b0423ed7d2aac97fd9469b51d6de43f/urwid-4.2.5.tar.gz"
    sha256 "a1fea3a551953fae4e6b727536334233bbb8b88c45230bc91d9751b402a8c16d"
  end

  resource "watchdog" do
    url "https://files.pythonhosted.org/packages/db/7d/7f3d619e951c88ed75c6037b246ddcf2d322812ee8ea189be89511721d54/watchdog-6.0.0.tar.gz"
    sha256 "9ddf7c82fda3ae8e24decda1338ede66e1c99883db93711d8fb941eaa2d8c282"
  end

  resource "wcwidth" do
    url "https://files.pythonhosted.org/packages/f0/b4/7830542634bb2d3e62aa3b586a72d5b3b6c91c3168929e7000ef3fed041d/wcwidth-0.9.2.tar.gz"
    sha256 "ae0ef90b90f6af38b54f1fe6d58662ec33b3cb4b8391958a62416d654231727b"
  end

  def install
    virtualenv_install_with_resources
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/topydo --version")

    system bin/"topydo", "add", "Install Homebrew"
    assert_match "Install Homebrew", shell_output("#{bin}/topydo ls")
    system bin/"topydo", "do", "1"
    assert_empty shell_output("#{bin}/topydo ls")
  end
end
