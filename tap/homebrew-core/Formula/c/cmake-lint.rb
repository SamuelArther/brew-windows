class CmakeLint < Formula
  include Language::Python::Virtualenv

  desc "Static code checker for CMake files"
  homepage "https://github.com/cmake-lint/cmake-lint"
  url "https://files.pythonhosted.org/packages/9f/46/9b6c39837be138179347611ec8cb79bf00ff3b54966ee8b63e031a05cf76/cmakelint-1.4.3.tar.gz"
  sha256 "98a1e485318b41eeaf4dee3469ca3039d4745985353ecea208d6dd2c1204c71d"
  license "Apache-2.0"

  bottle do
    sha256 cellar: :any, x86_64_windows: "f37a91e4f9c2140e571d43ce9ba40d31cd8c59d5464f5b57861380069cdfb38b"
  end

  depends_on "python@3.15"

  def install
    virtualenv_install_with_resources
  end

  test do
    (testpath/"test.cmake").write <<~CMAKE
      cmake_minimum_required(VERSION 3.10)
      project(TestProject)
    CMAKE

    output = shell_output("#{bin}/cmakelint test.cmake 2>&1")
    assert_match "Total Errors: 0", output

    assert_match version.to_s, shell_output("#{bin}/cmakelint --version 2>&1")
  end
end
