class Termaid < Formula
  include Language::Python::Virtualenv

  desc "Render Mermaid diagrams in the terminal"
  homepage "https://github.com/fasouto/termaid"
  url "https://files.pythonhosted.org/packages/fd/5e/5f1a8e99de2cb3be31bed3768bd11a6ddc9f108cec17684a3d927f7eb61a/termaid-0.12.0.tar.gz"
  sha256 "37a44eb453671888d793e2c126a89f2d5276ad71c6dced16e53a161be29c66bb"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "fe7ff7d2e442fd2fb1e7519b59d1aa8f3f53bbf179d5afeaa42bf1ee7bc5f630"
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
