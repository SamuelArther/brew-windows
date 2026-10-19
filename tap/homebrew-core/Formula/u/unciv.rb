class Unciv < Formula
  desc "Open-source Android/Desktop remake of Civ V"
  homepage "https://github.com/yairm210/Unciv"
  url "https://github.com/yairm210/Unciv/releases/download/4.22.12/Unciv.jar"
  sha256 "2355e6c65ffdfbcff1d06ebe77c332c196f2fd6766d1111b1319cf294d675e60"
  license "MPL-2.0"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+(?:[._-]?patch\d*)?)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "4af062ee6895c05ca15035c27655512a408acfa126dc155b6c11f235d5938b4a"
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
