class Setconf < Formula
  include Language::Python::Shebang

  desc "Utility for easily changing settings in configuration files"
  homepage "https://setconf.roboticoverlords.org/"
  url "https://setconf.roboticoverlords.org/setconf-0.7.8.tar.xz"
  sha256 "858bdf8fc1d351c2e5c95bc629a6fbc9ef5f89ac906f3f31e7117299e57917be"
  license "GPL-2.0-or-later"
  head "https://github.com/xyproto/setconf.git", branch: "main"

  bottle do
    sha256 cellar: :any, x86_64_windows: "5c092cd0f6b357df44b06bf3d6e0f882ef7672247f66bee17d7bb4af069341e8"
  end

  uses_from_macos "python"

  def install
    rewrite_shebang detected_python_shebang(use_python_from_path: true), "setconf.py"
    bin.install "setconf.py" => "setconf"
    man1.install "setconf.1.gz"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/setconf --version")
    system bin/"setconf", "--test"
  end
end
