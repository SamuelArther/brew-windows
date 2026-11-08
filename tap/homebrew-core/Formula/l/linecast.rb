class Linecast < Formula
  include Language::Python::Virtualenv

  desc "Weather, tides, the sun, the moon, and maps, drawn for the terminal"
  homepage "https://github.com/ashuttl/linecast"
  url "https://files.pythonhosted.org/packages/65/36/e4bd56ea369b4b3dacdae8b65edac70c681ff3ff8ea4f27a7716c11299c3/linecast-2.12.0.tar.gz"
  sha256 "3047afd970f9ff15f30095685bbba878e129273c76c19170cce187edd28a3105"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "b7c91ca43dc9b16366616751f8d9babb7a78785a505b50b37610e9c9712d2d38"
  end

  depends_on "python@3.15"

  def install
    virtualenv_install_with_resources
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/linecast --version")

    output = shell_output("#{bin}/linecast sunshine --location 43.657,-70.258 --json")
    assert_match '"schema": 1', output
    assert_match '"sunrise":', output
    assert_match '"sunset":', output
  end
end
