class Faker < Formula
  include Language::Python::Virtualenv

  desc "Python-based fake data generator"
  homepage "https://faker.readthedocs.io"
  url "https://github.com/joke2k/faker/archive/refs/tags/v40.63.0.tar.gz"
  sha256 "5f0c1d0aed75f91675dd04eb9138f4109e966d7aecdcdbca119eb0eb6efa7b9d"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "4705bc525975211ff3b10aeca5a849be986d3456aebaff381e2f38e713cde517"
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
