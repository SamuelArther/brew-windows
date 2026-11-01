class Unciv < Formula
  desc "Open-source Android/Desktop remake of Civ V"
  homepage "https://github.com/yairm210/Unciv"
  url "https://github.com/yairm210/Unciv/releases/download/4.22.16/Unciv.jar"
  sha256 "1b911a3059f09bea329bba17fa1ec9fe0d040e5827e94d3dbf8594cf7b4a5e75"
  license "MPL-2.0"

  livecheck do
    url :stable
    regex(/^v?(\d+(?:\.\d+)+(?:[._-]?patch\d*)?)$/i)
  end

  bottle do
    sha256 cellar: :any, x86_64_windows: "0047acf9bc4f1cbb47179c9a9cfc6cc1808624ab06435eefaaa97eb03f946142"
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
