class Unciv < Formula
  desc "Open-source Android/Desktop remake of Civ V"
  homepage "https://github.com/yairm210/Unciv"
  url "https://github.com/yairm210/Unciv/releases/download/4.22.14/Unciv.jar"
  sha256 "d7b8e4fd10833cab0579e1439aacb74464702ee6bae22ecc068fa8a2fc5b12e0"
  license "MPL-2.0"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+(?:[._-]?patch\d*)?)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "7f13240a2e6dc7e74ae3815f6386f07239b246563e787151f4e2439d698f356a"
  end

  no_autobump! because: :bumped_by_upstream

  depends_on "openjdk"

  def install
    libexec.install "Unciv.jar"
    bin.write_jar_script libexec/"Unciv.jar", "unciv"
  end

  test do
    # Unciv is a GUI application, so there is no cli functionality to test
    assert_match version.to_str, shell_output("#{bin}/unciv --version")
  end
end
