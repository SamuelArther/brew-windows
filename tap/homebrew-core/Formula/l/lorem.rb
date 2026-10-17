class Lorem < Formula
  include Language::Python::Shebang

  desc "Python generator for the console"
  homepage "https://github.com/per9000/lorem"
  url "https://github.com/per9000/lorem/archive/refs/tags/v0.8.0.tar.gz"
  sha256 "3eec439d616a044e61a6733730b1fc009972466f869dae358991f95abd57e8b3"
  license "GPL-3.0-or-later"
  head "https://github.com/per9000/lorem.git", branch: "master"

  bottle do
    sha256 cellar: :any, x86_64_windows: "aef2e7f089c3fb663a4ff4296cbdd4347e49371fc3137fd62b97fe4706b5ed00"
  end

  uses_from_macos "python"

  def install
    bin.install "lorem"
    rewrite_shebang detected_python_shebang(use_python_from_path: true), bin/"lorem"
  end

  test do
    assert_equal "lorem ipsum", shell_output("#{bin}/lorem -n 2").strip.downcase
  end
end
