class Faker < Formula
  include Language::Python::Virtualenv

  desc "Python-based fake data generator"
  homepage "https://faker.readthedocs.io"
  url "https://github.com/joke2k/faker/archive/refs/tags/v40.44.0.tar.gz"
  sha256 "ca982a56617f085f350934c90606a79e149d8ff547c4821deacbca01592f4806"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "a660ca97d929584b8501ffab7ca9545a3ce9cad43eeefa44ae3b9b9dcc2cbca2"
  end

  depends_on "python@3.15"

  def install
    virtualenv_install_with_resources
  end

  test do
    assert_match "{'ssn': '150-19-7120', 'name': 'Christian Blake'}",
                 shell_output("#{bin}/faker --seed 12345 profile ssn,name")
  end
end
