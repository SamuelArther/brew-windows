class Handbrake < Formula
  desc "Open-source video transcoder available for Linux, Mac, and Windows"
  homepage "https://handbrake.fr/"
  url "https://github.com/HandBrake/HandBrake/releases/download/1.11.2/HandBrakeCLI-1.11.2-win-x86_64.zip"
  sha256 "80bfe8d5f5d11cc3ef76b834add3ed4e82dee6523ffeb435c283f88b1a21f09d"
  license "GPL-2.0-only"
  revision 1

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "HandBrakeCLI.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/HandBrakeCLI --version")
  end
end
