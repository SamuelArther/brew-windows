class Termaid < Formula
  include Language::Python::Virtualenv

  desc "Render Mermaid diagrams in the terminal"
  homepage "https://github.com/fasouto/termaid"
  url "https://files.pythonhosted.org/packages/a0/f5/1c20309f634783e53d3d23fd5dcd7a794304120aaa7cc2b7fd26ff2fe4f3/termaid-0.13.0.tar.gz"
  sha256 "fed44511be753ccf569099c57ddd0c70c06f54649405f15c3ecec33f7bb3b94c"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "e9653d55bbb6611e2e62c43a7320d735ede690c97e79b157d875f87a12806e17"
  end

  depends_on "python@3.15"

  def install
    virtualenv_install_with_resources
  end

  test do
    output = pipe_output(bin/"termaid", "graph LR\n  A[Start] --> B[End]\n")
    assert_match "Start", output
    assert_match "End", output
    assert_match version.to_s, shell_output("#{bin}/termaid --version")
  end
end
