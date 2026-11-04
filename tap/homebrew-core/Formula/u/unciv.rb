class Unciv < Formula
  desc "Open-source Android/Desktop remake of Civ V"
  homepage "https://github.com/yairm210/Unciv"
  url "https://github.com/yairm210/Unciv/releases/download/4.22.17/Unciv.jar"
  sha256 "65feedba1cb9f206f9067b985893bdf6c244c5d883e13ed6402226017df9ab64"
  license "MPL-2.0"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+(?:[._-]?patch\d*)?)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "db620339cf8893ad11794a1b2b34c6aa7dbd2548270f454c89049cd76b61b820"
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
