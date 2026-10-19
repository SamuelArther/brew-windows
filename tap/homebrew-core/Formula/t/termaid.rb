class Termaid < Formula
  include Language::Python::Virtualenv

  desc "Render Mermaid diagrams in the terminal"
  homepage "https://github.com/fasouto/termaid"
  url "https://files.pythonhosted.org/packages/6c/5c/e2bc0d27c30de7a55a1c7fac8326454353058385864c5cda4a0d9d19ec59/termaid-0.9.1.tar.gz"
  sha256 "e81ccdf92590182873b5a6437d709264fd70611a91f130f14418e80fad4c7ecf"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "961eecfa937cf4c8aeb858c838d7e8cd0e592cfe67aa74a458ce25e463094378"
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
