class PythonMarkdown < Formula
  include Language::Python::Virtualenv

  desc "Python implementation of Markdown"
  homepage "https://python-markdown.github.io"
  url "https://files.pythonhosted.org/packages/f4/32/0b4d0297064244f04af4331b65de16237cc3cce07474171ee14222a28981/markdown-3.11.2.tar.gz"
  sha256 "87bf5d90f2d2882e2c02fcda182c2800456b985bff38913867c49706481514b0"
  license "BSD-3-Clause"
  head "https://github.com/Python-Markdown/markdown.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b851d5fd48bddc894327f80fc89e26a56ebd49576ca00f8d16a530061ca9ae3e"
  end

  depends_on "python@3.15"

  def install
    virtualenv_install_with_resources
  end

  test do
    (testpath/"test.md").write("# Hello World!")
    assert_equal "<h1>Hello World!</h1>", shell_output("#{bin}/markdown_py test.md").strip
  end
end
