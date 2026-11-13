class Unciv < Formula
  desc "Open-source Android/Desktop remake of Civ V"
  homepage "https://github.com/yairm210/Unciv"
  url "https://github.com/yairm210/Unciv/releases/download/5.0.0/Unciv.jar"
  sha256 "142f1517ea39cffbc4a26a8601ac5625806bfe78bd437337bbd13e7b7bc1b0e4"
  license "MPL-2.0"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+(?:[._-]?patch\d*)?)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "b2152b692adbe5bd343123a5ed994ad6fbf101eaeb9fbb8bc3b8895c48b5dfed"
  end

  no_autobump! because: :bumped_by_upstream

  depends_on "openjdk"

  deny_network_access!

  def install
    libexec.install "Unciv.jar"
    bin.write_jar_script libexec/"Unciv.jar", "unciv"
  end

  test do
    # Unciv is a GUI application, so there is no cli functionality to test
    assert_match version.to_str, shell_output("#{bin}/unciv --version")
  end
end
